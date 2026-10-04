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

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <summary>
        /// Precedence order: major first, then minor, then patch. The previous
        /// `||` chain compared the three independently, which made 2.0.0 look
        /// lower than 1.5.0.
        /// </summary>
        public bool IsLowerThan(SemanticVersionVO version)
        {
            if (major != version.major) { return major < version.major; }
            if (minor != version.minor) { return minor < version.minor; }
            return patch < version.patch;
        }


        public override bool Equals(object obj)
        {
            //  `as` rather than a cast: Equals(object) must answer false for an
            //  unrelated type, not throw.
            SemanticVersionVO castedObj = obj as SemanticVersionVO;
            if (ReferenceEquals(castedObj, null))
            {
                return false;
            }
            if (ReferenceEquals(this, castedObj))
            {
                return true;
            }
            return major == castedObj.major &&
                   minor == castedObj.minor &&
                   patch == castedObj.patch;
        }

        public static bool operator == (SemanticVersionVO obj1, SemanticVersionVO obj2)
        {

        if (ReferenceEquals(obj1, obj2)) 
            return true;
        if (ReferenceEquals(obj1, null)) 
            return false;
        if (ReferenceEquals(obj2, null))
            return false;

            return obj1.major == obj2.major &&
                   obj1.minor == obj2.minor &&
                   obj1.patch == obj2.patch;
        }

        public static bool operator != (SemanticVersionVO obj1, SemanticVersionVO obj2)
        {
            return !(obj1 == obj2);
        }
    }
}
