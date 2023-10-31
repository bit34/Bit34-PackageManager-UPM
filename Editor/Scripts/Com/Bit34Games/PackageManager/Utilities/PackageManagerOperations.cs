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
            _packageManagerModel.AddPackage(name, url, SemanticVersionHelpers.ParseVersionArray(tags.ToArray()));
        }

        public void LoadDependencies()
        {
            if (Directory.Exists(PACKAGE_FOLDER)==false)
            {
                Directory.CreateDirectory(PACKAGE_FOLDER);
            }

//  TODO only delete not needed package folders
//            Dictionary<string, string> loadedDependencies     = GetLoadedDependencyList();

            //  Delete all folders in package install folder
            string[] packageFolderPaths = Directory.GetDirectories(PACKAGE_FOLDER);

            for (int i = 0; i < packageFolderPaths.Length; i++)
            {
                Directory.Delete(packageFolderPaths[i], true);
                File.Delete(packageFolderPaths[i] + ".meta");
            }

            //  Load dependencies file
            string filePath = DEPENDENCIES_JSON_FOLDER + DEPENDENCIES_JSON_FILENAME;

            if (File.Exists(filePath) == false)
            {
                UnityEngine.Debug.LogWarning("Bit34 Package Manager : No dependencies file");
                return;
            }
            
            Dictionary<string, string> unresolvedDependencies = GetDependencyList(filePath);

            //  Resolve dependencies
            while (unresolvedDependencies.Count>0)
            {
                IEnumerator<string> enumerator = unresolvedDependencies.Keys.GetEnumerator();
                enumerator.MoveNext();

                string              packageName    = enumerator.Current;
                SemanticVersionVO   packageVersion = SemanticVersionHelpers.ParseVersion(unresolvedDependencies[packageName]);
                RepositoryPackageVO package        = _packageManagerModel.GetPackageByName(packageName);

                unresolvedDependencies.Remove(packageName);
                _packageManagerModel.AddDependency(packageName, packageVersion);

                ClonePackage(package, packageVersion);
                PackageFileVO packageFile = LoadPackageJson(package, packageVersion);
                
                if (packageFile.dependencies != null && packageFile.dependencies.Count > 0)
                {
                    foreach (string dependencyName in packageFile.dependencies.Keys)
                    {
                        if (_packageManagerModel.HasDependency(dependencyName) == false &&
                            unresolvedDependencies.ContainsKey(dependencyName) == false)
                        {
                            string dependencyVersion = ParsePackageJsonDependencyVersion(packageFile.dependencies[dependencyName]);
                            unresolvedDependencies.Add(dependencyName, dependencyVersion);
                        }
                    }
                }
            }
        }

        private Dictionary<string, string> GetDependencyList(string filePath)
        {
            Dictionary<string, string> dependencies = new Dictionary<string, string>();

            string             fileContent = File.ReadAllText(filePath);
            DependenciesFileVO file        = JsonConvert.DeserializeObject<DependenciesFileVO>(fileContent);

            foreach (string dependencyName in file.dependencies.Keys)
            {
                dependencies.Add(dependencyName, file.dependencies[dependencyName]);
            }
            
            return dependencies;
        }

        private Dictionary<string, string> GetLoadedDependencyList()
        {
            Dictionary<string, string> dependencies = new Dictionary<string, string>();

            string[] packageFolderPaths = Directory.GetDirectories(PACKAGE_FOLDER);

            for (int i = 0; i < packageFolderPaths.Length; i++)
            {
                string packageFolderPath = packageFolderPaths[i];
                int    startIndex        = Math.Max(0, packageFolderPath.LastIndexOf(Path.DirectorySeparatorChar));
                int    separatorIndex    = packageFolderPath.LastIndexOf('@');
                string packageName       = packageFolderPath.Substring(startIndex+1, separatorIndex-startIndex-1);
                string packageVersion    = packageFolderPath.Substring(separatorIndex+1);
                dependencies.Add(packageName, packageVersion);
            }

            return dependencies;
        }

        private void ClonePackage(RepositoryPackageVO package, SemanticVersionVO packageVersion)
        {
            string packagePath = PACKAGE_FOLDER + package.name + "@" + packageVersion;
            GitHelpers.Clone(packagePath, package.url);
            GitHelpers.CheckoutBranch(packagePath, VERSION_BRANCH_PREFIX + packageVersion);
            AssetDatabase.Refresh();
        }

        public PackageFileVO LoadPackageJson(RepositoryPackageVO package, SemanticVersionVO packageVersion)
        {
            string        packagePath = PACKAGE_FOLDER + package.name + "@" + packageVersion;
            string        filePath    = packagePath + Path.DirectorySeparatorChar + PACKAGE_JSON_FILENAME;
            string        fileContent = File.ReadAllText(filePath);
            PackageFileVO file        = JsonConvert.DeserializeObject<PackageFileVO>(fileContent);
            return file;
        }

        public string ParsePackageJsonDependencyVersion(string version)
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
