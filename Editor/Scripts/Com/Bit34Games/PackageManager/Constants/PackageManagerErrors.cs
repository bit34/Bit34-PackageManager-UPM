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
    }
}
