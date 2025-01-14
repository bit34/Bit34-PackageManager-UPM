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

        public static void DeletePackage(string packagePath)
        {
            StorageHelpers.DeleteDirectory(packagePath);
            StorageHelpers.DeleteFile(packagePath + ".meta");
        }
        
        public static void DeletePackage(string packageName, SemanticVersionVO packageVersion)
        {
            string packagePath = GetPackagePath(packageName, packageVersion);
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

/*
        public static List<PackageReferenceVO> GetLoadedDependencies()
        {
            List<PackageReferenceVO> dependencies = new List<PackageReferenceVO>();

            string[] packageFolderPaths = Directory.GetDirectories(PackageManagerConstants.PACKAGE_FOLDER);

            for (int i = 0; i < packageFolderPaths.Length; i++)
            {
                string              packagePath    = packageFolderPaths[i];
                int                 startIndex     = Math.Max(0, packagePath.LastIndexOf(Path.DirectorySeparatorChar));
                int                 separatorIndex = packagePath.LastIndexOf('@');
                string              packageName    = packagePath.Substring(startIndex+1, separatorIndex-startIndex-1);
                SemanticVersionVO   packageVersion = SemanticVersionHelpers.ParseVersion(packagePath.Substring(separatorIndex+1));
                dependencies.Add(new PackageReferenceVO(packageName, packageVersion, ""));
            }

            return dependencies;
        }
*/
        public static void Log(string message)
        {
            UnityEngine.Debug.Log("PackageManager:" + message);
        }
    }

}