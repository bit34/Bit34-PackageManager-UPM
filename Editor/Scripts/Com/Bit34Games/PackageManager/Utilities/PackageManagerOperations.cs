using System;
using System.Collections.Generic;
using System.IO;
using Com.Bit34games.PackageManager.Constants;
using Com.Bit34games.PackageManager.FileVOs;
using Com.Bit34games.PackageManager.Models;
using Com.Bit34games.PackageManager.VOs;
using Newtonsoft.Json;  //   "com.unity.nuget.newtonsoft-json": "2.0.0",


namespace Com.Bit34games.PackageManager.Utilities
{

    internal class PackageManagerOperations
    {
        //  MEMBERS
        //      Private
        private PackageManagerModel _packageManagerModel;


        //  CONSTRUCTORS
        public PackageManagerOperations(PackageManagerModel packageManagerModel)
        {
            _packageManagerModel = packageManagerModel;
        }


        //  METHODS
        public bool CheckPrerequirements()
        {
            string version;
            if (GitHelpers.GetVersion(out version)==false)
            {
                _packageManagerModel.SetError(new PackageManagerErrorVO(PackageManagerErrors.GitNotFound));
                return false;
            }

            string repositoriesFilePath = PackageManagerConstants.REPOSITORIES_JSON_FOLDER + PackageManagerConstants.REPOSITORIES_JSON_FILENAME;
            if (File.Exists(repositoriesFilePath) == false)
            {
                _packageManagerModel.SetError(new PackageManagerErrorVO(PackageManagerErrors.RepositoriesFileNotFound));
                return false;
            }
            
            string dependenciesFilePath = PackageManagerConstants.DEPENDENCIES_JSON_FOLDER + PackageManagerConstants.DEPENDENCIES_JSON_FILENAME;
            if (File.Exists(dependenciesFilePath) == false)
            {
                _packageManagerModel.SetError(new PackageManagerErrorVO(PackageManagerErrors.DependenciesFileNotFound));
                return false;
            }

            return true;
        }

        public bool DetectClonedDependencies()
        {
            _packageManagerModel.Clear();

            if (LoadRepositories() == false)
            {
                return false;
            }

            UpdateInstalledVersions();

            //  Load dependencies file
            string             fileContent = StorageHelpers.LoadTextFile(PackageManagerConstants.DEPENDENCIES_JSON_PATH);
            DependenciesFileVO file        = JsonConvert.DeserializeObject<DependenciesFileVO>(fileContent);
            if (file == null || file.dependencies == null)
            {
                _packageManagerModel.SetError(new PackageManagerErrorVO(PackageManagerErrors.DependenciesFileBadFormat));
                return false;
            }

            //  Read dependencies file content
            List<PackageReferenceVO> dependencies = PackageManagerHelpers.ReadDependenciesJson(file);

            //  Iterate dependencies from file
            while (dependencies.Count > 0)
            {
                PackageReferenceVO dependency = dependencies[0];
                dependencies.RemoveAt(0);
                
                //  Dependency does not have a repository data
                int packageIndex = _packageManagerModel.FindPackageIndex(dependency.name);
                if (packageIndex == -1)
                {
                    _packageManagerModel.SetError(new PackageManagerErrorForDependencyNotInRepositoryVO(dependency.name));
                    return false;
                }

                //  Dependency already added
                SemanticVersionVO existingVersion  = _packageManagerModel.GetDependencyVersion(dependency.name);
                SemanticVersionVO installedVersion = _packageManagerModel.GetInstalledVersion(dependency.name);
                if (existingVersion != null)
                {
                    //  Added dependency has a different version
                    if (existingVersion != dependency.version)
                    {
                        _packageManagerModel.SetError(new PackageManagerErrorForDependencyAddedWithDifferentVersionVO(dependency.name, 
                                                                                                                      _packageManagerModel.GetDependencyVersion(dependency.name),
                                                                                                                      _packageManagerModel.GetDependencyParents(dependency.name),
                                                                                                                      dependency.version,
                                                                                                                      dependency.parent));
                        return false;
                    }

                    _packageManagerModel.AddDependencyParent(dependency.name, dependency.parent);
                }
                //  Add dependecy
                else
                {
                    if(installedVersion == null)
                    {
                        _packageManagerModel.AddDependency(dependency.name, DependencyStates.NotInstalled, dependency.version, dependency.parent);
                    }
                    else
                    if(installedVersion == dependency.version)
                    {
                        _packageManagerModel.AddDependency(dependency.name, DependencyStates.Installed, dependency.version, dependency.parent);
                    }
                    else
                    {
                        _packageManagerModel.AddDependency(dependency.name, DependencyStates.WrongVersion, dependency.version, dependency.parent);
                    }
                }

                //  If dependency is loaded, get its dependencies
                if(installedVersion != null && installedVersion == dependency.version)
                {
                    PackageFileVO dependencyPackageFile = PackageManagerHelpers.LoadPackageJson(dependency.name, dependency.version);
                    
                    if (dependencyPackageFile.dependencies != null && dependencyPackageFile.dependencies.Count > 0)
                    {
                        foreach (string subDependencyName in dependencyPackageFile.dependencies.Keys)
                        {
                            string            subDependencyVersionText = dependencyPackageFile.dependencies[subDependencyName];
                            SemanticVersionVO subDependencyVersion     = SemanticVersionHelpers.ParseVersionFromTag(subDependencyVersionText);
                            if (_packageManagerModel.FindPackageIndex(subDependencyName) == -1)
                            {
                                _packageManagerModel.SetError(new PackageManagerErrorForDependencyNotInRepositoryVO(subDependencyName));
                                return false;
                            }
                            else
                            if (_packageManagerModel.GetDependencyState(subDependencyName) == DependencyStates.NotInUse)
                            {
                                dependencies.Add(new PackageReferenceVO(subDependencyName, subDependencyVersion, dependency.name));
                            }
                            else
                            if (subDependencyVersion != _packageManagerModel.GetDependencyVersion(subDependencyName))
                            {
                                _packageManagerModel.SetError(new PackageManagerErrorForDependencyAddedWithDifferentVersionVO(subDependencyName, 
                                                                                                                              _packageManagerModel.GetDependencyVersion(subDependencyName),
                                                                                                                              _packageManagerModel.GetDependencyParents(subDependencyName),
                                                                                                                              subDependencyVersion,
                                                                                                                              dependency.name));
                                return false;
                            }
                        }
                    }
                }
            }

            UpdateNotNeededPackages();
            
            return true;
        }

        public bool CloneDependencies()
        {
            _packageManagerModel.Clear();
            
            if (LoadRepositories() == false)
            {
                return false;
            }

            UpdateInstalledVersions();

            //  Load dependencies file
            string             fileContent = StorageHelpers.LoadTextFile(PackageManagerConstants.DEPENDENCIES_JSON_PATH);
            DependenciesFileVO file        = JsonConvert.DeserializeObject<DependenciesFileVO>(fileContent);
            if (file == null || file.dependencies == null)
            {
                _packageManagerModel.SetError(new PackageManagerErrorVO(PackageManagerErrors.DependenciesFileBadFormat));
                return false;
            }

            //  Read dependencies file content
            List<PackageReferenceVO> dependencies = PackageManagerHelpers.ReadDependenciesJson(file);

            if (Directory.Exists(PackageManagerConstants.PACKAGE_FOLDER)==false)
            {
                Directory.CreateDirectory(PackageManagerConstants.PACKAGE_FOLDER);
            }

            List<string> oldInstallsToRemove = new List<string>();

            //  Iterate dependencies from file
            while (dependencies.Count>0)
            {
                PackageReferenceVO dependency = dependencies[0];
                dependencies.RemoveAt(0);
                
                //  Dependency does not have a repository data
                int packageIndex = _packageManagerModel.FindPackageIndex(dependency.name);
                if (packageIndex == -1)
                {
                    _packageManagerModel.SetError(new PackageManagerErrorForDependencyNotInRepositoryVO(dependency.name));
                    return false;
                }

                //  Dependency already added
                SemanticVersionVO existingVersion  = _packageManagerModel.GetDependencyVersion(dependency.name);
                SemanticVersionVO installedVersion = _packageManagerModel.GetInstalledVersion(dependency.name);
                if (existingVersion != null)
                {
                    //  Added dependency has a different version
                    if (existingVersion != dependency.version)
                    {
                        _packageManagerModel.SetError(new PackageManagerErrorForDependencyAddedWithDifferentVersionVO(dependency.name, 
                                                                                                                      _packageManagerModel.GetDependencyVersion(dependency.name),
                                                                                                                      _packageManagerModel.GetDependencyParents(dependency.name),
                                                                                                                      dependency.version,
                                                                                                                      dependency.parent));
                        return false;
                    }

                    _packageManagerModel.AddDependencyParent(dependency.name, dependency.parent);
                }
                //  Add dependecy
                else
                {
                    if (installedVersion != null && installedVersion != dependency.version)
                    {
                        oldInstallsToRemove.Add(PackageManagerHelpers.GetPackagePath(dependency.name, installedVersion));
                        _packageManagerModel.SetInstalledVersion(dependency.name, null);
                    }

                    if (installedVersion != dependency.version)
                    {
                        _packageManagerModel.AddDependency(dependency.name, DependencyStates.Installed, dependency.version, dependency.parent);
                        _packageManagerModel.SetInstalledVersion(dependency.name, dependency.version);

                        string packagePath = PackageManagerHelpers.GetPackagePath(dependency.name, dependency.version);
                        string packageURL  = _packageManagerModel.GetPackageURL(packageIndex);
                        PackageManagerHelpers.ClonePackage(dependency.name, packageURL, dependency.version);

                        List<string>        tags     = GitHelpers.GetTags(packagePath);
                        SemanticVersionVO[] versions = SemanticVersionHelpers.ParseVersionArray(tags.ToArray());
                        _packageManagerModel.PackageVersionsReloadCompleted(dependency.name, versions);

                        PackageFileVO dependencyPackageFile = PackageManagerHelpers.LoadPackageJson(dependency.name, dependency.version);
                        if (dependencyPackageFile.dependencies != null && dependencyPackageFile.dependencies.Count > 0)
                        {
                            foreach (string subDependencyName in dependencyPackageFile.dependencies.Keys)
                            {
                                string            subDependencyVersionText = dependencyPackageFile.dependencies[subDependencyName];
                                SemanticVersionVO subDependencyVersion     = SemanticVersionHelpers.ParseVersionFromTag(subDependencyVersionText);
                                if (_packageManagerModel.FindPackageIndex(subDependencyName) == -1)
                                {
                                    _packageManagerModel.SetError(new PackageManagerErrorForDependencyNotInRepositoryVO(subDependencyName));
                                    return false;
                                }
                                else
                                if (_packageManagerModel.GetDependencyState(subDependencyName) == DependencyStates.NotInUse)
                                {
                                    dependencies.Add(new PackageReferenceVO(subDependencyName, subDependencyVersion, dependency.name));
                                }
                                else
                                if (subDependencyVersion != _packageManagerModel.GetDependencyVersion(subDependencyName))
                                {
                                    _packageManagerModel.SetError(new PackageManagerErrorForDependencyAddedWithDifferentVersionVO(subDependencyName, 
                                                                                                                                  _packageManagerModel.GetDependencyVersion(subDependencyName),
                                                                                                                                  _packageManagerModel.GetDependencyParents(subDependencyName),
                                                                                                                                  subDependencyVersion,
                                                                                                                                  dependency.name));
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < oldInstallsToRemove.Count; i++)
            {
                PackageManagerHelpers.DeletePackage(oldInstallsToRemove[i]);
            }
            
            RemoveNotNeededPackages();
            return true;
        }

        private bool LoadRepositories()
        {
            string           fileContent = StorageHelpers.LoadTextFile(PackageManagerConstants.REPOSITORIES_JSON_PATH);
            RepositoryFileVO file        = JsonConvert.DeserializeObject<RepositoryFileVO>(fileContent);
            if (file == null || file.packages == null)
            {
                _packageManagerModel.SetError(new PackageManagerErrorVO(PackageManagerErrors.RepositoriesFileBadFormat));
                return false;
            }
            
            //  Read file content
            for (int i=0; i<file.packages.Count; i++)
            {
                RepositoryPackageFileVO package = file.packages[i];
                
                _packageManagerModel.AddPackage(package.name, package.url, new SemanticVersionVO[]{});
            }

            return true;
        }

        private void UpdateInstalledVersions()
        {
            IEnumerator<string> packageNames = _packageManagerModel.GetPackageNameEnumerator();
            while (packageNames.MoveNext())
            {
                string packageName = packageNames.Current;
                _packageManagerModel.SetInstalledVersion(packageName, null);
            }

            if (Directory.Exists(PackageManagerConstants.PACKAGE_FOLDER))
            {
                string[] packageFolderPaths = Directory.GetDirectories(PackageManagerConstants.PACKAGE_FOLDER);

                for (int i = 0; i < packageFolderPaths.Length; i++)
                {
                    string              packagePath    = packageFolderPaths[i];
                    int                 startIndex     = Math.Max(0, packagePath.LastIndexOf(Path.DirectorySeparatorChar));
                    int                 separatorIndex = packagePath.LastIndexOf('@');
                    string              packageName    = packagePath.Substring(startIndex+1, separatorIndex-startIndex-1);
                    SemanticVersionVO   packageVersion = SemanticVersionHelpers.ParseVersion(packagePath.Substring(separatorIndex+1));
                    _packageManagerModel.SetInstalledVersion(packageName, packageVersion);
                }
            }
        }
        
        private void UpdateNotNeededPackages()
        {
            IEnumerator<string> packageNames = _packageManagerModel.GetPackageNameEnumerator();
            while (packageNames.MoveNext())
            {
                string packageName = packageNames.Current;
                if (_packageManagerModel.GetInstalledVersion(packageName) != null &&
                    _packageManagerModel.GetDependencyVersion(packageName) == null)
                {
                    _packageManagerModel.SetDependencyState(packageName, DependencyStates.NotNeeded);
                }
            }
        }

        private void RemoveNotNeededPackages()
        {
            List<string> packagesToRemove = new List<string>();

            IEnumerator<string> packageNames = _packageManagerModel.GetPackageNameEnumerator();
            while (packageNames.MoveNext())
            {
                string            packageName             = packageNames.Current;
                SemanticVersionVO packageInstalledVersion = _packageManagerModel.GetInstalledVersion(packageName);
                if (packageInstalledVersion != null &&
                    _packageManagerModel.GetDependencyVersion(packageName) == null)
                {
                    string packagePath = PackageManagerHelpers.GetPackagePath(packageName, packageInstalledVersion);
                    UnityEngine.Debug.Log(packagePath);
                    packagesToRemove.Add(packagePath);
                }
            }

            for (int i = 0; i < packagesToRemove.Count; i++)
            {
                string packagePath = packagesToRemove[i];
                PackageManagerHelpers.DeletePackage(packagePath);
            }
        }

    }
}
