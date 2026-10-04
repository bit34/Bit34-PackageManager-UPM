namespace Com.Bit34games.PackageManager.Constants
{
    internal enum PackageManagerErrors
    {
        GitNotFound,

        RepositoriesFileNotFound,
        RepositoriesFileBadFormat,

        DependenciesFileNotFound,
        DependenciesFileBadFormat,

        DependencyNotInRepository,
        DependencyAddedWithDifferentVersion,

        //  Appended, not inserted: PackageManagerEditorWindow dispatches error
        //  drawing through an array indexed by this enum.
        GitCommandFailed,
    }
}
