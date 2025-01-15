namespace Com.Bit34games.PackageManager.VOs
{

    internal class PackageReferenceVO
    {
        //  MEMBERS
        public readonly string            name;
        public readonly SemanticVersionVO version;
        public readonly string            parent;


        //  CONSTRUCTORS
        public PackageReferenceVO(string            name,
                                  SemanticVersionVO version,
                                  string            parent)
        {
            this.name    = name;
            this.version = version;
            this.parent  = parent;
        }
    }
}
