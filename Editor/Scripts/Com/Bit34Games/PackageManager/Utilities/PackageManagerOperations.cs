using System;
using System.Collections.Generic;
using System.IO;
using Com.Bit34games.PackageManager.FileVOs;
using Com.Bit34games.PackageManager.Models;
using Com.Bit34games.PackageManager.VOs;
using Newtonsoft.Json;  //   "com.unity.nuget.newtonsoft-json": "2.0.0",
using UnityEditor;


namespace Com.Bit34games.PackageManager.Utilities
{
    internal class PackageManagerOperations
    {
        //  CONSTANTS
        private readonly string REPOSITORIES_JSON_FOLDER   = "Assets" + Path.DirectorySeparatorChar + "Bit34" + Path.DirectorySeparatorChar;
        private readonly string REPOSITORIES_JSON_FILENAME = "repositories.json";
        private readonly string DEPENDENCIES_JSON_FOLDER   = "Assets" + Path.DirectorySeparatorChar + "Bit34" + Path.DirectorySeparatorChar;
        private readonly string DEPENDENCIES_JSON_FILENAME = "dependencies.json";
        private readonly string PACKAGE_FOLDER             = "Assets" + Path.DirectorySeparatorChar + "Bit34" + Path.DirectorySeparatorChar + "Packages" + Path.DirectorySeparatorChar;
        private readonly string PACKAGE_JSON_FILENAME      = "package.json";
        private readonly string VERSION_BRANCH_PREFIX      = "v";


        //  MEMBERS
        //      Private
        private PackageManagerModel _packageManagerModel;


        //  CONSTRUCTORS
        public PackageManagerOperations(PackageManagerModel packageManagerModel)
        {
            _packageManagerModel = packageManagerModel;
        }

        //  METHODS
        public void LoadRepositories(Action<float> progressHandler)
        {
            string filePath = REPOSITORIES_JSON_FOLDER + REPOSITORIES_JSON_FILENAME;
            if (File.Exists(filePath) == false)
            {
                UnityEngine.Debug.LogWarning("Bit34 Package Manager : No repository file");
                return;
            }

            string fileContent = StorageHelpers.LoadTextFile(filePath);
            if (string.IsNullOrEmpty(fileContent))
            {
                UnityEngine.Debug.LogWarning("Bit34 Package Manager : Can not read repository file");
                return;
            }

            RepositoryFileVO file = JsonConvert.DeserializeObject<RepositoryFileVO>(fileContent);

            if (file == null || file.packages == null)
            {
                UnityEngine.Debug.LogWarning("Bit34 Package Manager : Can not parse repository file");
                return;
            }

            progressHandler(0.1f);
            
            for (int i=0; i<file.packages.Count; i++)
            {
                RepositoryPackageFileVO package = file.packages[i];
                AddPackage(package.name, package.url);

                progressHandler(0.9f * (1.0f/file.packages.Count)+i);
            }
        }

        private void AddPackage(string name, string url)
        {
            List<string> tags = GitHelpers.GetRemoteTags(url);
            _packageManagerModel.AddPackage(name, url, SemanticVersionHelpers.ParseVersionArray(tags.ToArray()));
        }

        public bool CheckLoadedDependencies()
        {
            if (Directory.Exists(PACKAGE_FOLDER)==false)
            {
                return false;
            }

            //  Load dependencies file
            string filePath = DEPENDENCIES_JSON_FOLDER + DEPENDENCIES_JSON_FILENAME;

            if (File.Exists(filePath) == false)
            {
                UnityEngine.Debug.LogWarning("Bit34 Package Manager : No dependencies file");
                return false;
            }

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

        public void LoadDependencies()
        {
            if (Directory.Exists(PACKAGE_FOLDER)==false)
            {
                Directory.CreateDirectory(PACKAGE_FOLDER);
            }

            //  Load dependencies file
            string filePath = DEPENDENCIES_JSON_FOLDER + DEPENDENCIES_JSON_FILENAME;

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
                string              packagePath    = GetPackagePath(packageName, packageVersion);

                unresolvedDependencies.Remove(packageName);
                _packageManagerModel.AddDependency(packageName, packageVersion);

                if (loadedDependencies.ContainsKey(packagePath) == false)
                {
                    ClonePackage(package, packageVersion);
                }
                else
                {
                    loadedDependencies.Remove(packagePath);
                }

                PackageFileVO packageFile = LoadPackageJson(package, packageVersion);
                
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
                DeletePackage(loadedDependency.name, loadedDependency.version);
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

            string[] packageFolderPaths = Directory.GetDirectories(PACKAGE_FOLDER);

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

        private string GetPackagePath(string packageName, SemanticVersionVO packageVersion)
        {
            return PACKAGE_FOLDER + packageName + "@" + packageVersion;
        }

        private void ClonePackage(RepositoryPackageVO package, SemanticVersionVO packageVersion)
        {
            string packagePath = GetPackagePath(package.name, packageVersion);
            GitHelpers.Clone(packagePath, package.url);
            GitHelpers.CheckoutBranch(packagePath, VERSION_BRANCH_PREFIX + packageVersion);
            AssetDatabase.Refresh();
        }

        private void DeletePackage(string packageName, SemanticVersionVO packageVersion)
        {
            string packagePath = GetPackagePath(packageName, packageVersion);
            StorageHelpers.DeleteDirectory(packagePath);
            StorageHelpers.DeleteFile(packagePath + ".meta");
        }

        public PackageFileVO LoadPackageJson(RepositoryPackageVO package, SemanticVersionVO packageVersion)
        {
            string        packagePath = GetPackagePath(package.name, packageVersion);
            string        fileContent = StorageHelpers.LoadTextFile(packagePath + Path.DirectorySeparatorChar + PACKAGE_JSON_FILENAME);
            PackageFileVO file        = JsonConvert.DeserializeObject<PackageFileVO>(fileContent);
            return file;
        }

    }
}
