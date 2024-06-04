
using System.Collections.Generic;
using Com.Bit34games.PackageManager.VOs;

namespace Com.Bit34games.PackageManager.Models
{
    internal class PackageManagerModel
    {
        //  MEMBERS
        public int PackageCount { get { return _packages.Count; } }
        //      Private
        private List<RepositoryPackageVO>               _packages;
        private Dictionary<string, RepositoryPackageVO> _packagesByName;
        private Dictionary<string, DependencyPackageVO> _dependencies;

        //  CONSTRUCTORS
        public PackageManagerModel()
        {
            _packages             = new List<RepositoryPackageVO>();
            _packagesByName       = new Dictionary<string, RepositoryPackageVO>();
            _dependencies         = new Dictionary<string, DependencyPackageVO>();
        }

        //  METHODS
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
/*
        public IEnumerator<string> GetDependencyEnumerator()
        {
            return _dependencies.Keys.GetEnumerator();
        }
*/

    }
}
