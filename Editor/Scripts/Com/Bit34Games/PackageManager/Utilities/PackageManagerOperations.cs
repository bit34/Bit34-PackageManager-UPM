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
        public bool LoadRepositories()
        {
            string fileContent = StorageHelpers.LoadTextFile(PackageManagerConstants.REPOSITORIES_JSON_PATH);
            if (string.IsNullOrEmpty(fileContent))
            {
                UnityEngine.Debug.LogWarning(PackageManagerConstants.ERROR_TEXT_CAN_NOT_FOUND_REPOSITORIES);
                return false;
            }

            RepositoryFileVO file = JsonConvert.DeserializeObject<RepositoryFileVO>(fileContent);

            if (file == null || file.packages == null)
            {
                UnityEngine.Debug.LogWarning("Bit34 Package Manager : Can not parse repository file");
                return false;
            }
            
            for (int i=0; i<file.packages.Count; i++)
            {
                RepositoryPackageFileVO package = file.packages[i];
                
                _packageManagerModel.AddPackage(package.name, package.url, new SemanticVersionVO[]{});
            }

            return true;
        }

        public void GetClonedDependencies()
        {
            if (Directory.Exists(PackageManagerConstants.PACKAGE_FOLDER)==false)
            {
                return;
            }
            
            Dictionary<string, DependencyPackageVO> loadedDependencies = GetLoadedDependencyList();

            foreach(string folderName in loadedDependencies.Keys)
            {
                _packageManagerModel.AddDependency(loadedDependencies[folderName].name, loadedDependencies[folderName].version);
            }
        }

        public void CloneDependencies()
        {
            if (Directory.Exists(PackageManagerConstants.PACKAGE_FOLDER)==false)
            {
                Directory.CreateDirectory(PackageManagerConstants.PACKAGE_FOLDER);
            }

            //  Load dependencies file
            string filePath = PackageManagerConstants.DEPENDENCIES_JSON_FOLDER + PackageManagerConstants.DEPENDENCIES_JSON_FILENAME;

            if (File.Exists(filePath) == false)
            {
                UnityEngine.Debug.LogWarning("Bit34 Package Manager : No dependencies file");
                return;
            }
            
            Dictionary<string, DependencyPackageVO> loadedDependencies     = GetLoadedDependencyList();
            Dictionary<string, SemanticVersionVO>   unresolvedDependencies = GetDependencyList(filePath);

            //  Resolve dependencies
            while (unresolvedDependencies.Count>0)
            {
                IEnumerator<string> enumerator = unresolvedDependencies.Keys.GetEnumerator();
                enumerator.MoveNext();

                string              packageName    = enumerator.Current;
                SemanticVersionVO   packageVersion = unresolvedDependencies[packageName];
                RepositoryPackageVO package        = _packageManagerModel.GetPackageByName(packageName);
                string              packagePath    = PackageManagerHelpers.GetPackagePath(packageName, packageVersion);

                unresolvedDependencies.Remove(packageName);
                _packageManagerModel.AddDependency(packageName, packageVersion);

                if (loadedDependencies.ContainsKey(packagePath) == false)
                {
                    PackageManagerHelpers.ClonePackage(package, packageVersion);

                    List<string>        tags     = GitHelpers.GetTags(packagePath);
                    SemanticVersionVO[] versions = SemanticVersionHelpers.ParseVersionArray(tags.ToArray());
                    UnityEngine.Debug.Log(tags[0]);
                    _packageManagerModel.PackageVersionsReloadCompleted(packageName, versions);
                }
                else
                {
                    loadedDependencies.Remove(packagePath);
                }

                PackageFileVO packageFile = PackageManagerHelpers.LoadPackageJson(package, packageVersion);
                
                if (packageFile.dependencies != null && packageFile.dependencies.Count > 0)
                {
                    foreach (string dependencyName in packageFile.dependencies.Keys)
                    {
                        if (_packageManagerModel.HasDependency(dependencyName) == false &&
                            unresolvedDependencies.ContainsKey(dependencyName) == false)
                        {
                            SemanticVersionVO dependencyVersion = SemanticVersionHelpers.ParseVersionFromTag(packageFile.dependencies[dependencyName]);
                            unresolvedDependencies.Add(dependencyName, dependencyVersion);
                        }
                    }
                }
            }
            
            foreach (DependencyPackageVO loadedDependency in loadedDependencies.Values)
            {
                PackageManagerHelpers.DeletePackage(loadedDependency.name, loadedDependency.version);
            }
        }

        private Dictionary<string, SemanticVersionVO> GetDependencyList(string filePath)
        {
            Dictionary<string, SemanticVersionVO> dependencies = new Dictionary<string, SemanticVersionVO>();

            string             fileContent = StorageHelpers.LoadTextFile(filePath);
            DependenciesFileVO file        = JsonConvert.DeserializeObject<DependenciesFileVO>(fileContent);

            foreach (string dependencyName in file.dependencies.Keys)
            {
                dependencies.Add(dependencyName, SemanticVersionHelpers.ParseVersion(file.dependencies[dependencyName]));
            }
            
            return dependencies;
        }

        private Dictionary<string, DependencyPackageVO> GetLoadedDependencyList()
        {
            Dictionary<string, DependencyPackageVO> dependencies = new Dictionary<string, DependencyPackageVO>();

            string[] packageFolderPaths = Directory.GetDirectories(PackageManagerConstants.PACKAGE_FOLDER);

            for (int i = 0; i < packageFolderPaths.Length; i++)
            {
                string              packagePath    = packageFolderPaths[i];
                int                 startIndex     = Math.Max(0, packagePath.LastIndexOf(Path.DirectorySeparatorChar));
                int                 separatorIndex = packagePath.LastIndexOf('@');
                string              packageName    = packagePath.Substring(startIndex+1, separatorIndex-startIndex-1);
                string              packageVersion = packagePath.Substring(separatorIndex+1);
                DependencyPackageVO dependency     = new DependencyPackageVO(packageName, SemanticVersionHelpers.ParseVersion(packageVersion));
                dependencies.Add(packagePath, dependency);
            }

            return dependencies;
        }





        public bool CheckLoadedDependencies()
        {
            if (Directory.Exists(PackageManagerConstants.PACKAGE_FOLDER)==false)
            {
                return false;
            }

            //  Load dependencies file
            string                                  filePath           = PackageManagerConstants.DEPENDENCIES_JSON_FOLDER + PackageManagerConstants.DEPENDENCIES_JSON_FILENAME;
            Dictionary<string, SemanticVersionVO>   dependencies       = GetDependencyList(filePath);
            Dictionary<string, DependencyPackageVO> loadedDependencies = GetLoadedDependencyList();

            foreach (string dependencyName in dependencies.Keys)
            {
                SemanticVersionVO dependencyVersion = dependencies[dependencyName];
                bool              dependencyFound   = false;
                foreach (DependencyPackageVO loadedDependency in loadedDependencies.Values)
                {
                    if (loadedDependency.name    == dependencyName && 
                        loadedDependency.version == dependencyVersion)
                    {
                        dependencyFound = true;
                        break;
                    }
                }
                if (dependencyFound == false)
                {
                    return false;
                }
            }
            return true;
        }

    }
}
