namespace Com.Bit34games.PackageManager.VOs
{
    internal class RepositoryPackageVO
    {
        //  MEMBERS
        public int VersionCount { get { return _versions.Length; } }
        public readonly string name;
        public readonly string url;
        //      Private
        private SemanticVersionVO[] _versions;

        //  CONSTRUCTORS
        public RepositoryPackageVO(string   name,
                                   string   url,
                                   SemanticVersionVO[] versions)
        {
            this.name = name;
            this.url  = url;
            _versions = versions;
        }

        //  METHODS
        public SemanticVersionVO GetVersion(int index)
        {
            return _versions[index];
        }
    }
}