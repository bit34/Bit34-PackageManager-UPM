
using System.Collections.Generic;
using Com.Bit34games.PackageManager.Constants;
using Com.Bit34games.PackageManager.VOs;

namespace Com.Bit34games.PackageManager.Models
{
    internal class PackageManagerModel
    {
        //  MEMBERS
        public PackageManagerStates State { get; private set; }
        public int                  PackageCount { get { return _packages.Count; } }
        //      Private
        private List<RepositoryPackageVO>               _packages;
        private Dictionary<string, RepositoryPackageVO> _packagesByName;
        private Dictionary<string, DependencyPackageVO> _dependencies;
        private HashSet<string>                         _packagesReloadingVersion;


        //  CONSTRUCTORS
        public PackageManagerModel()
        {
            _packages                 = new List<RepositoryPackageVO>();
            _packagesByName           = new Dictionary<string, RepositoryPackageVO>();
            _dependencies             = new Dictionary<string, DependencyPackageVO>();
            _packagesReloadingVersion = new HashSet<string>();
        }

        //  METHODS
        public void SetAsReady()
        {
            State = PackageManagerStates.Ready;
        }
        
        public void SetAsReloading()
        {
            State = PackageManagerStates.Reloading;
        }

        public void Clear()
        {
            _packages.Clear();
            _packagesByName.Clear();
            _dependencies.Clear();
        }

        public void AddPackage(string name, string url, SemanticVersionVO[] versions)
        {
            RepositoryPackageVO package = new RepositoryPackageVO(name, url, versions);
            _packages.Add(package);
            _packagesByName.Add(name, package);
        }

        public RepositoryPackageVO GetPackage(int index)
        {
            return _packages[index];
        }

        public RepositoryPackageVO GetPackageByName(string name)
        {
            RepositoryPackageVO package = null;
            _packagesByName.TryGetValue(name, out package);
            return package;
        }

        public void AddDependency(string name, SemanticVersionVO version) 
        {
            _dependencies.Add(name, new DependencyPackageVO(name, version));
        }

        public bool HasDependency(string name)
        {
            return _dependencies.ContainsKey(name);
        }

        public SemanticVersionVO GetDependencyVersion(string name)
        {
            return _dependencies[name].version;
        }

        public void PackageVersionsReloadStarted(string packageName)
        {
            _packagesReloadingVersion.Add(packageName);
            _packagesByName[packageName].UpdateVersions(new SemanticVersionVO[]{});
        }

        public void PackageVersionsReloadCompleted(string packageName, SemanticVersionVO[] versions)
        {
            _packagesReloadingVersion.Remove(packageName);
            _packagesByName[packageName].UpdateVersions(versions);
        }

        public bool IsPackageVersionsUpdating(string packageName)
        {
            return _packagesReloadingVersion.Contains(packageName);
        }
/*
        public IEnumerator<string> GetDependencyEnumerator()
        {
            return _dependencies.Keys.GetEnumerator();
        }
*/

    }
}
