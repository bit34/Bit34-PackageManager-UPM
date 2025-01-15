using System.Collections.Generic;
using Com.Bit34games.PackageManager.Constants;
using Com.Bit34games.PackageManager.VOs;


namespace Com.Bit34games.PackageManager.Models
{
    internal class PackageManagerModel
    {
        //  MEMBERS
        public PackageManagerStates  State { get; private set; }
        public PackageManagerErrorVO Error { get; private set; }
        public int                   PackageCount { get { return _packages.Count; } }
        //      Private
        private List<PackageVO>               _packages;
        private Dictionary<string, PackageVO> _packagesByName;
        private HashSet<string>               _packagesReloadingVersion;


        //  CONSTRUCTORS
        public PackageManagerModel()
        {
            _packages                 = new List<PackageVO>();
            _packagesByName           = new Dictionary<string, PackageVO>();
            _packagesReloadingVersion = new HashSet<string>();
        }

        //  METHODS
        public void ResetState()
        {
            State = PackageManagerStates.NotInitialized;
        }

        public void SetAsReady()
        {
            State = PackageManagerStates.Ready;
        }
        
        public void SetAsLoading()
        {
            State = PackageManagerStates.Loading;
        }

        public void SetError(PackageManagerErrorVO error)
        {
            Error = error;
            if(Error != null)
            {
                State = PackageManagerStates.Error;
            }
        }

        public void Clear()
        {
            _packages.Clear();
            _packagesByName.Clear();
        }

        public void AddPackage(string name, string url, SemanticVersionVO[] versions)
        {
            PackageVO package = new PackageVO(name, url, versions);
            _packages.Add(package);
            _packagesByName.Add(name, package);
        }

        public int FindPackageIndex(string packageName)
        {
            return _packages.FindIndex((PackageVO package)=>{ return package.name == packageName; });
        }

        public string GetPackageName(int packageIndex)
        {
            return _packages[packageIndex].name;
        }

        public string GetPackageURL(int packageIndex)
        {
            return _packages[packageIndex].url;
        }

        public int GetPackageVersionCount(int packageIndex)
        {
            return _packages[packageIndex].versions.Length;
        }

        public SemanticVersionVO GetPackageVersion(int packageIndex, int versionIndex)
        {
            return _packages[packageIndex].versions[versionIndex];
        }

        public void AddDependency(string packageName, DependencyStates state, SemanticVersionVO version, string parent) 
        {
            PackageVO package = _packagesByName[packageName];
            package.dependencyState   = state;
            package.dependencyVersion = version;
            package.dependencyParents.Add(parent);
        }

        public void SetDependencyState(string packageName, DependencyStates state) 
        {
            PackageVO package = _packagesByName[packageName];
            package.dependencyState = state;
        }

        public void SetInstalledVersion(string packageName, SemanticVersionVO version) 
        {
            PackageVO package = _packagesByName[packageName];
            package.installedVersion = version;
        }

        public void AddDependencyParent(string packageName, string parent) 
        {
            PackageVO package = _packagesByName[packageName];
            package.dependencyParents.Add(parent);
        }

        public DependencyStates GetDependencyState(string packageName)
        {
            return _packagesByName[packageName].dependencyState;
        }

        public SemanticVersionVO GetDependencyVersion(string packageName)
        {
            return _packagesByName[packageName].dependencyVersion;
        }

        public string[] GetDependencyParents(string packageName)
        {
            return _packagesByName[packageName].dependencyParents.ToArray();
        }

        public SemanticVersionVO GetInstalledVersion(string packageName)
        {
            return _packagesByName[packageName].installedVersion;
        }

        public void PackageVersionsReloadStarted(string packageName)
        {
            _packagesReloadingVersion.Add(packageName);
            _packagesByName[packageName].versions = new SemanticVersionVO[]{};
        }

        public void PackageVersionsReloadCompleted(string packageName, SemanticVersionVO[] versions)
        {
            _packagesReloadingVersion.Remove(packageName);
            _packagesByName[packageName].versions = versions;
        }

        public bool IsPackageVersionsUpdating(string packageName)
        {
            return _packagesReloadingVersion.Contains(packageName);
        }

        public IEnumerator<string> GetPackageNameEnumerator()
        {
            return _packagesByName.Keys.GetEnumerator();
        }

    }
}
