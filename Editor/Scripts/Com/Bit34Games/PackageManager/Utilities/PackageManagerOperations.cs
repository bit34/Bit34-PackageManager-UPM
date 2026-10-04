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

        /// <summary>
        /// Reports what is installed versus what is wanted, without touching
        /// the working tree.
        /// </summary>
        public bool DetectClonedDependencies()
        {
            return ResolveDependencies(applyChanges: false);
        }

        /// <summary>
        /// Brings the working tree in line with dependencies.json: clones what
        /// is missing, moves packages onto the wanted version, and deletes the
        /// ones nothing references any more.
        /// </summary>
        public bool CloneDependencies()
        {
            return ResolveDependencies(applyChanges: true);
        }

        /// <summary>
        /// Walks dependencies.json breadth-first, following each package's own
        /// declared dependencies, and records what it finds on the model.
        ///
        /// With <paramref name="applyChanges"/> set the walk also performs the
        /// work — cloning, version swaps, deletions. Both modes share this one
        /// body on purpose: the duplicate copies they replaced had drifted
        /// apart, which is how a package could end up deleted while still
        /// needed.
        /// </summary>
        private bool ResolveDependencies(bool applyChanges)
        {
            _packageManagerModel.Clear();

            if (LoadRepositories() == false)
            {
                return false;
            }

            UpdateInstalledVersions();

            List<PackageReferenceVO> dependencies;
            if (LoadDependencies(out dependencies) == false)
            {
                return false;
            }

            if (applyChanges &&
                Directory.Exists(PackageManagerConstants.PACKAGE_FOLDER) == false)
            {
                Directory.CreateDirectory(PackageManagerConstants.PACKAGE_FOLDER);
            }

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

                SemanticVersionVO existingVersion  = _packageManagerModel.GetDependencyVersion(dependency.name);
                SemanticVersionVO installedVersion = _packageManagerModel.GetInstalledVersion(dependency.name);

                //  Seen before: another package already pulled this one in. Only
                //  the version has to agree; its own dependencies were queued
                //  the first time around.
                if (existingVersion != null)
                {
                    if (existingVersion != dependency.version)
                    {
                        _packageManagerModel.SetError(new PackageManagerErrorForDependencyAddedWithDifferentVersionVO(dependency.name,
                                                                                                                      existingVersion,
                                                                                                                      _packageManagerModel.GetDependencyParents(dependency.name),
                                                                                                                      dependency.version,
                                                                                                                      dependency.parent));
                        return false;
                    }

                    _packageManagerModel.AddDependencyParent(dependency.name, dependency.parent);
                    continue;
                }

                //  First time: record it, and when applying, put it on disk.
                if (applyChanges)
                {
                    string packagePath = PackageManagerHelpers.GetPackagePath(dependency.name, dependency.version);

                    if (installedVersion == null)
                    {
                        string packageURL = _packageManagerModel.GetPackageURL(packageIndex);
                        if (PackageManagerHelpers.ClonePackage(dependency.name, packageURL, dependency.version) == false)
                        {
                            _packageManagerModel.SetError(new PackageManagerErrorForGitCommandFailedVO(
                                "Cloning " + dependency.name + " " + dependency.version,
                                GitHelpers.LastError));
                            return false;
                        }
                    }
                    else
                    if (installedVersion != dependency.version)
                    {
                        if (PackageManagerHelpers.ChangePackageVersion(dependency.name, installedVersion, dependency.version) == false)
                        {
                            _packageManagerModel.SetError(new PackageManagerErrorForGitCommandFailedVO(
                                "Switching " + dependency.name + " from " + installedVersion + " to " + dependency.version,
                                GitHelpers.LastError));
                            return false;
                        }
                    }

                    //  Record the dependency whichever way we got here. Missing
                    //  this on the already-installed paths left dependencyVersion
                    //  null, and RemoveNotNeededPackages then deleted a package
                    //  that was in fact needed.
                    _packageManagerModel.AddDependency(dependency.name, DependencyStates.Installed, dependency.version, dependency.parent);
                    _packageManagerModel.SetInstalledVersion(dependency.name, dependency.version);
                    installedVersion = dependency.version;

                    List<string>        tags     = GitHelpers.GetTags(packagePath);
                    SemanticVersionVO[] versions = SemanticVersionHelpers.ParseVersionArray(tags.ToArray());
                    _packageManagerModel.PackageVersionsReloadCompleted(dependency.name, versions);
                }
                else
                {
                    DependencyStates state;
                    if (installedVersion == null)                   { state = DependencyStates.NotInstalled; }
                    else if (installedVersion == dependency.version) { state = DependencyStates.Installed; }
                    else                                             { state = DependencyStates.WrongVersion; }

                    _packageManagerModel.AddDependency(dependency.name, state, dependency.version, dependency.parent);
                }

                //  A package's own dependencies are only readable once the right
                //  version is actually on disk.
                if (installedVersion != null &&
                    installedVersion == dependency.version)
                {
                    if (QueueSubDependencies(dependency, dependencies) == false)
                    {
                        return false;
                    }
                }
            }

            if (applyChanges) { RemoveNotNeededPackages(); }
            else              { UpdateNotNeededPackages(); }

            return true;
        }

        /// <summary>
        /// Reads the installed package's package.json and pushes anything it
        /// depends on onto the walk queue, failing on an unknown package or a
        /// version that contradicts what is already recorded.
        /// </summary>
        private bool QueueSubDependencies(PackageReferenceVO dependency, List<PackageReferenceVO> queue)
        {
            PackageFileVO packageFile = PackageManagerHelpers.LoadPackageJson(dependency.name, dependency.version);
            if (packageFile.dependencies == null || packageFile.dependencies.Count == 0)
            {
                return true;
            }

            foreach (string subDependencyName in packageFile.dependencies.Keys)
            {
                string            versionText        = packageFile.dependencies[subDependencyName];
                SemanticVersionVO subDependencyVersion = SemanticVersionHelpers.ParseVersionFromTag(versionText);

                if (_packageManagerModel.FindPackageIndex(subDependencyName) == -1)
                {
                    _packageManagerModel.SetError(new PackageManagerErrorForDependencyNotInRepositoryVO(subDependencyName));
                    return false;
                }
                else
                if (_packageManagerModel.GetDependencyState(subDependencyName) == DependencyStates.NotInUse)
                {
                    queue.Add(new PackageReferenceVO(subDependencyName, subDependencyVersion, dependency.name));
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

            return true;
        }

        /// <summary>
        /// Reads Assets/Bit34/dependencies.json into the initial walk queue.
        /// </summary>
        private bool LoadDependencies(out List<PackageReferenceVO> dependencies)
        {
            dependencies = null;

            string             fileContent = StorageHelpers.LoadTextFile(PackageManagerConstants.DEPENDENCIES_JSON_PATH);
            DependenciesFileVO file        = JsonConvert.DeserializeObject<DependenciesFileVO>(fileContent);
            if (file == null || file.dependencies == null)
            {
                _packageManagerModel.SetError(new PackageManagerErrorVO(PackageManagerErrors.DependenciesFileBadFormat));
                return false;
            }

            dependencies = PackageManagerHelpers.ReadDependenciesJson(file);
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
                    string packagePath    = packageFolderPaths[i];
                    string folderName     = Path.GetFileName(packagePath);
                    int    separatorIndex = folderName.LastIndexOf('@');

                    //  The package folder is gitignored and nothing stops
                    //  something else from leaving a folder there, so anything
                    //  that is not <name>@<version> for a package we know about
                    //  is skipped rather than trusted. Deleting it would be
                    //  presumptuous; crashing on it used to be the alternative.
                    if (separatorIndex == -1)
                    {
                        PackageManagerHelpers.Log("ignoring '" + folderName + "' in " +
                                                  PackageManagerConstants.PACKAGE_FOLDER +
                                                  " (expected <name>@<version>)");
                        continue;
                    }

                    string packageName = folderName.Substring(0, separatorIndex);

                    SemanticVersionVO packageVersion;
                    if (SemanticVersionHelpers.TryParseVersion(folderName.Substring(separatorIndex + 1), out packageVersion) == false)
                    {
                        PackageManagerHelpers.Log("ignoring '" + folderName + "' in " +
                                                  PackageManagerConstants.PACKAGE_FOLDER +
                                                  " (version is not a version number)");
                        continue;
                    }

                    if (_packageManagerModel.FindPackageIndex(packageName) == -1)
                    {
                        PackageManagerHelpers.Log("ignoring '" + folderName + "' in " +
                                                  PackageManagerConstants.PACKAGE_FOLDER +
                                                  " (not declared in " +
                                                  PackageManagerConstants.REPOSITORIES_JSON_FILENAME + ")");
                        continue;
                    }

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
