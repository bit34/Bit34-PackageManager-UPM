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

        /// <summary>
        /// Clones a package and checks out its version tag. Returns false if
        /// either git call failed, leaving the reason in GitHelpers.LastError.
        /// </summary>
        public static bool ClonePackage(string packageName, string packageURL, SemanticVersionVO packageVersion)
        {
            string packagePath = GetPackagePath(packageName, packageVersion);
            if (GitHelpers.Clone(packagePath, packageURL) == false)
            {
                return false;
            }
            if (GitHelpers.CheckoutBranch(packagePath, PackageManagerConstants.VERSION_BRANCH_PREFIX + packageVersion) == false)
            {
                return false;
            }
            AssetDatabase.Refresh();
            return true;
        }

        /// <summary>
        /// Moves an already cloned package onto another version tag and renames
        /// its folder to match. Returns false if either git call failed; the
        /// folder is left on the old version in that case.
        /// </summary>
        public static bool ChangePackageVersion(string packageName, SemanticVersionVO packageVersion, SemanticVersionVO newPackageVersion)
        {
            string packagePath = GetPackagePath(packageName, packageVersion);
            if (GitHelpers.Fetch(packagePath) == false)
            {
                return false;
            }
            if (GitHelpers.CheckoutBranch(packagePath, PackageManagerConstants.VERSION_BRANCH_PREFIX + newPackageVersion) == false)
            {
                return false;
            }
            string newPackagePath = GetPackagePath(packageName, newPackageVersion);
            StorageHelpers.RenameDirectory(packagePath, newPackagePath);
            AssetDatabase.Refresh();
            return true;
        }

        public static void DeletePackage(string packagePath)
        {
            //  Through the asset database, not File/Directory. DeleteAsset
            //  removes a folder together with its sibling .meta and keeps
            //  Unity's own record in step. Doing it behind Unity's back left
            //  the .meta on disk after a version bump, and Unity recreated an
            //  empty folder from it and warned about every one: "A meta data
            //  file (.meta) exists but its folder can't be found, and has been
            //  created." Seven of those for seven replaced packages.
            string assetPath = packagePath.Replace(Path.DirectorySeparatorChar, '/');
            if (AssetDatabase.DeleteAsset(assetPath))
            {
                return;
            }

            //  Fallback for a path the asset database does not have, which is
            //  what a half-removed folder from an older run looks like.
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