using Com.Bit34games.PackageManager.Constants;


namespace Com.Bit34games.PackageManager.VOs
{
    internal class PackageManagerErrorVO
    {
        //  MEMBERS
        public readonly PackageManagerErrors error;


        //  CONSTRUCTOR
        public PackageManagerErrorVO(PackageManagerErrors error)
        {
            this.error = error;
        }
    }
}