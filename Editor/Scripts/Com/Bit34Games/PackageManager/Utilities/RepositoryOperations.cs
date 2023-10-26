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
    internal class RepositoryOperations
    {
        //  CONSTANTS
        private const string REPOSITORIES_JSON_FOLDER   = "Assets/Bit34/";
        private const string REPOSITORIES_JSON_FILENAME = "repositories.json";
        private const string DEPENDENCIES_JSON_FOLDER   = "Assets/Bit34/";
        private const string DEPENDENCIES_JSON_FILENAME = "dependencies.json";
        private const string PACKAGE_FOLDER             = "Assets/Bit34/Packages/";
        private const string PACKAGE_JSON_FILENAME      = "package.json";
        private const string VERSION_BRANCH_PREFIX      = "v";

        //  MEMBERS
        //      Private
        private RepositoriesModel _repositoriesModel;

        //  CONSTRUCTORS
        public RepositoryOperations(RepositoriesModel repositoriesModel)
        {
            _repositoriesModel = repositoriesModel;
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

            string fileContent = File.ReadAllText(filePath);
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
            _repositoriesModel.AddPackage(name, url, SemanticVersionHelpers.ParseVersionArray(tags.ToArray()));
        }

/*
        private void CheckCacheFolders()
        {
            if (Directory.Exists(PACKAGE_CACHE_FOLDER))
            {
                string[] packageFolderPaths = Directory.GetDirectories(PACKAGE_CACHE_FOLDER);

                for (int i = 0; i < packageFolderPaths.Length; i++)
                {
                    string              packageFolderPath = packageFolderPaths[i];
                    string              packageFolderName = packageFolderPath.Substring(packageFolderPath.LastIndexOf(Path.DirectorySeparatorChar)+1);
                    RepositoryPackageVO package           = _repositoriesModel.GetPackageByName(packageFolderName);

                    if (package != null)
                    {
                        //List<string> tags = _gitOperations.GetTags(packageFolderPath);
                        _repositoriesModel.AddPackageCache(package.name, tags.ToArray());
                    }
                }
            }
        }
*/

        public void LoadDependencies()
        {
            string filePath = DEPENDENCIES_JSON_FOLDER + DEPENDENCIES_JSON_FILENAME;

            if (File.Exists(filePath) == false)
            {
                UnityEngine.Debug.LogWarning("Bit34 Package Manager : No dependencies file");
                return;
            }
            
            string             fileContent = File.ReadAllText(filePath);
            DependenciesFileVO file        = JsonConvert.DeserializeObject<DependenciesFileVO>(fileContent);

            Dictionary<string, string> unresolvedDependencies = new Dictionary<string, string>();
            foreach (string dependencyName in file.dependencies.Keys)
            {
                unresolvedDependencies.Add(dependencyName, file.dependencies[dependencyName]);
            }
            
            while (unresolvedDependencies.Count>0)
            {
                IEnumerator<string> enumerator = unresolvedDependencies.Keys.GetEnumerator();
                enumerator.MoveNext();

                string              packageName    = enumerator.Current;
                SemanticVersionVO   packageVersion = SemanticVersionHelpers.ParseVersion(unresolvedDependencies[packageName]);
                RepositoryPackageVO package        = _repositoriesModel.GetPackageByName(packageName);

                unresolvedDependencies.Remove(packageName);
                _repositoriesModel.AddDependency(packageName, packageVersion);

                ClonePackage(package, packageVersion);
                PackageFileVO packageFile = LoadPackageJson(package, packageVersion);
                
                if (packageFile.dependencies != null && packageFile.dependencies.Count > 0)
                {
                    foreach (string dependencyName in packageFile.dependencies.Keys)
                    {
                        if (_repositoriesModel.HasDependency(dependencyName) == false &&
                            unresolvedDependencies.ContainsKey(dependencyName) == false)
                        {
                            string dependencyVersion = ParseDependencyVersion(packageFile.dependencies[dependencyName]);
                            unresolvedDependencies.Add(dependencyName, dependencyVersion);
                        }
                    }
                }
            }
        }

        private void ClonePackage(RepositoryPackageVO package, SemanticVersionVO packageVersion)
        {
            string packagePath = PACKAGE_FOLDER + package.name + "@" + packageVersion;
            GitHelpers.Clone(packagePath, package.url);
            GitHelpers.CheckoutBranch(packagePath, VERSION_BRANCH_PREFIX + packageVersion);
            AssetDatabase.Refresh();
        }

        private PackageFileVO LoadPackageJson(RepositoryPackageVO package, SemanticVersionVO packageVersion)
        {
            string        packagePath = PACKAGE_FOLDER + package.name + "@" + packageVersion;
            string        filePath    = packagePath + Path.DirectorySeparatorChar + PACKAGE_JSON_FILENAME;
            string        fileContent = File.ReadAllText(filePath);
            PackageFileVO file        = JsonConvert.DeserializeObject<PackageFileVO>(fileContent);
            return file;
        }

        private string ParseDependencyVersion(string version)
        {
            if (char.IsDigit(version[0]))
            {
                return version;
            }

            int startIndex = version.LastIndexOf('v') + 1;
            return version.Substring(startIndex);
        }
    }
}
