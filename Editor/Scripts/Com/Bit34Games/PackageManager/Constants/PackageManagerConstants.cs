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
        public static readonly string PACKAGE_FOLDER             = "Assets" + Path.DirectorySeparatorChar + "Bit34" + Path.DirectorySeparatorChar + "Packages" + Path.DirectorySeparatorChar;
        public static readonly string PACKAGE_JSON_FILENAME      = "package.json";
        public static readonly string VERSION_BRANCH_PREFIX      = "v";
        
        public static readonly string ERROR_TEXT_CAN_NOT_FOUND_GIT             = "Can not found git";
        public static readonly string ERROR_TEXT_CAN_NOT_FOUND_REPOSITORIES    = "Can not found Assets/Bit34/repositories.json For more details checkout Bit34Games.com";
        public static readonly string ERROR_TEXT_CAN_NOT_FOUND_DEPENDENCIES    = "Can not found Assets/Bit34/dependencies.json For more details checkout Bit34Games.com";
    }
}
