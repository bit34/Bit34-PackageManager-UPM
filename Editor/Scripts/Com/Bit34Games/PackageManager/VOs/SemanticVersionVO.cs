namespace Com.Bit34games.PackageManager.VOs
{
    public class SemanticVersionVO
    {
        //  MEMBERS
        public readonly int major;
        public readonly int minor;
        public readonly int patch;

        //  CONSTRUCTORS
        public SemanticVersionVO(int major,
                                 int minor=0,
                                 int patch=0)
        {
            this.major = major;
            this.minor = minor;
            this.patch = patch;
        }

        //  METHODS
        public override string ToString()
        {
            return major + "." + minor + "." + patch;
        }

        public override bool Equals(object obj)
        {
            SemanticVersionVO castedObj = (SemanticVersionVO)obj;
            return major == castedObj.major &&
                   minor == castedObj.minor &&
                   patch == castedObj.patch;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public bool IsLowerThan(SemanticVersionVO version)
        {
            if (major < version.major || 
                minor < version.minor ||
                patch < version.patch)
            {
                return true;
            }
            return false;
        }
    }
}
