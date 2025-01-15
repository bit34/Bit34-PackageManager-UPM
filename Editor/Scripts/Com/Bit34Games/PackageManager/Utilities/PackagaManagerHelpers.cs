using System;
using System.Collections.Generic;
using System.IO;
using Com.Bit34games.PackageManager.Constants;
using Com.Bit34games.PackageManager.FileVOs;
using Com.Bit34games.PackageManager.VOs;
using Newtonsoft.Json;
using UnityEditor;


namespace Com.Bit34games.PackageManager.Utilities
{
    internal static class PackageManagerHelpers
    {

        public static string GetPackagePath(string packageName, SemanticVersionVO packageVersion)
        {
            return PackageManagerConstants.PACKAGE_FOLDER + packageName + "@" + packageVersion;
        }

        public static void ClonePackage(string packageName, string packageURL, SemanticVersionVO packageVersion)
        {
            string packagePath = GetPackagePath(packageName, packageVersion);
            GitHelpers.Clone(packagePath, packageURL);
            GitHelpers.CheckoutBranch(packagePath, PackageManagerConstants.VERSION_BRANCH_PREFIX + packageVersion);
            AssetDatabase.Refresh();
        }

        public static void ChangePackageVersion(string packageName, SemanticVersionVO packageVersion, SemanticVersionVO newPackageVersion)
        {
            string packagePath = GetPackagePath(packageName, packageVersion);
            GitHelpers.Fetch(packagePath);
            GitHelpers.CheckoutBranch(packagePath, PackageManagerConstants.VERSION_BRANCH_PREFIX + newPackageVersion);
            string newPackagePath = GetPackagePath(packageName, newPackageVersion);
            StorageHelpers.RenameDirectory(packagePath, newPackagePath);
            AssetDatabase.Refresh();
        }

        public static void DeletePackage(string packagePath)
        {
            StorageHelpers.DeleteDirectory(packagePath);
            StorageHelpers.DeleteFile(packagePath + ".meta");
        }
        
        public static PackageFileVO LoadPackageJson(string packageName, SemanticVersionVO packageVersion)
        {
            string        packagePath = GetPackagePath(packageName, packageVersion);
            string        fileContent = StorageHelpers.LoadTextFile(packagePath + Path.DirectorySeparatorChar + PackageManagerConstants.PACKAGE_JSON_FILENAME);
            PackageFileVO file        = JsonConvert.DeserializeObject<PackageFileVO>(fileContent);
            return file;
        }

        public static List<PackageReferenceVO> ReadDependenciesJson(DependenciesFileVO file)
        {
            List<PackageReferenceVO> dependencies = new List<PackageReferenceVO>();
            foreach (string dependencyName in file.dependencies.Keys)
            {
                string            dependencyVersionString = file.dependencies[dependencyName];
                SemanticVersionVO dependencyVersion       = SemanticVersionHelpers.ParseVersion(dependencyVersionString);
                dependencies.Add(new PackageReferenceVO(dependencyName, dependencyVersion, ""));
            }
            
            return dependencies;
        }

        public static void Log(string message)
        {
            UnityEngine.Debug.Log("PackageManager:" + message);
        }
    }

}