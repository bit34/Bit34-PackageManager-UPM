namespace Com.Bit34games.PackageManager.Constants
{
    internal enum DependencyStates
    {
        NotInUse,       //  No reference to package
        Installed,      //  Right version installed
        NotInstalled,   //  Needs to be installed
        WrongVersion,   //  Installed wrong version
        NotNeeded,      //  Installed but not in use anymore, must be deleted
    }
}
