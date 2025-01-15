using Com.Bit34games.PackageManager.Constants;


namespace Com.Bit34games.PackageManager.VOs
{
    internal class PackageManagerErrorForDependencyNotInRepositoryVO : PackageManagerErrorVO
    {
        //  MEMBERS
        public readonly string packageName;


        //  CONSTRUCTOR
        public PackageManagerErrorForDependencyNotInRepositoryVO(string packageName)
         : base(PackageManagerErrors.DependencyNotInRepository)
        {
            this.packageName = packageName;
        }
    }
}
