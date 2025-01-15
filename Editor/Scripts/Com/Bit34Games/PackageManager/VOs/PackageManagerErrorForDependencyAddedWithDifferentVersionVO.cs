using Com.Bit34games.PackageManager.Constants;

namespace Com.Bit34games.PackageManager.VOs
{
    internal class PackageManagerErrorForDependencyAddedWithDifferentVersionVO : PackageManagerErrorVO
    {
        //  MEMBERS
        public readonly string            packageName;
        public readonly SemanticVersionVO currentVersion;
        public readonly string[]          currentRequesters;
        public readonly SemanticVersionVO newVersion;
        public readonly string            newRequester;


        //  CONSTRUCTOR
        public PackageManagerErrorForDependencyAddedWithDifferentVersionVO(string            packageName,
                                                                           SemanticVersionVO currentVersion,
                                                                           string[]          currentRequesters,
                                                                           SemanticVersionVO newVersion,
                                                                           string            newRequester) 
        : base(PackageManagerErrors.DependencyAddedWithDifferentVersion)
        {
            this.packageName       = packageName;
            this.currentVersion    = currentVersion;
            this.currentRequesters = currentRequesters;
            this.newVersion        = newVersion;
            this.newRequester      = newRequester;
        }
    }
}