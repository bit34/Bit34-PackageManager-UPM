namespace Com.Bit34games.PackageManager.VOs
{
    internal class DependencyPackageVO
    {
        //  MEMBERS
        public readonly string            name;
        public readonly SemanticVersionVO version;

        //  CONSTRUCTORS
        public DependencyPackageVO(string            name, 
                                   SemanticVersionVO version)
        {
            this.name    = name;
            this.version = version;
        }
    }
}