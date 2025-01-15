using System.IO;


namespace Com.Bit34games.PackageManager.Constants
{
    internal static class PackageManagerConstants
    {
        //  CONSTANTS
        public static readonly string REPOSITORIES_JSON_FOLDER   = "Assets" + Path.DirectorySeparatorChar + "Bit34" + Path.DirectorySeparatorChar;
        public static readonly string REPOSITORIES_JSON_FILENAME = "repositories.json";
        public static readonly string REPOSITORIES_JSON_PATH     = REPOSITORIES_JSON_FOLDER + REPOSITORIES_JSON_FILENAME;

        public static readonly string DEPENDENCIES_JSON_FOLDER   = "Assets" + Path.DirectorySeparatorChar + "Bit34" + Path.DirectorySeparatorChar;
        public static readonly string DEPENDENCIES_JSON_FILENAME = "dependencies.json";
        public static readonly string DEPENDENCIES_JSON_PATH     = DEPENDENCIES_JSON_FOLDER + DEPENDENCIES_JSON_FILENAME;
        
        public static readonly string PACKAGE_FOLDER             = "Assets" + Path.DirectorySeparatorChar + "Bit34" + Path.DirectorySeparatorChar + "Packages" + Path.DirectorySeparatorChar;
        public static readonly string PACKAGE_JSON_FILENAME      = "package.json";
        public static readonly string VERSION_BRANCH_PREFIX      = "v";
        
        public static readonly string ERROR_TEXT_GIT_NOT_FOUND                  = "Error : Can not found git";

        public static readonly string ERROR_TEXT_REPOSITORIES_FILE_NOT_FOUND    = "Error : Can not found Assets/Bit34/repositories.json For more details checkout Bit34Games.com";
        public static readonly string ERROR_TEXT_REPOSITORIES_FILE_BAD_FORMAT   = "Error : Assets/Bit34/repositories.json file has error(s)";

        public static readonly string ERROR_TEXT_DEPENDENCIES_FILE_NOT_FOUND    = "Error : Can not found Assets/Bit34/dependencies.json For more details checkout Bit34Games.com";
        public static readonly string ERROR_TEXT_DEPENDENCIES_FILE_BAD_FORMAT   = "Error : Assets/Bit34/dependencies.json file has error(s)";

        public static readonly string ERROR_TEXT_DEPENDENCY_DOES_NOT_HAVE_REPOSITORY = "Error : Dependency is not defined in repository file.";
        public static readonly string ERROR_TEXT_DEPENDENCY_ADDED_WITH_DIFFERENT_VERSION = "ERROR : Dependency already added with a difference version.";
    }
}
