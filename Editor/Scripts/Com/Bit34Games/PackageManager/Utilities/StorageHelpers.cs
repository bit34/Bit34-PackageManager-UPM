using System.IO;


namespace Com.Bit34games.PackageManager.Utilities
{
    public static class StorageHelpers
    {
        //  METHODS
        public static void DeleteFile(string filepath)
        {
            File.Delete(filepath);
        }
        
        public static void DeleteDirectory(string path)
        {
            string[] files = Directory.GetFiles(path);
            string[] dirs = Directory.GetDirectories(path);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(path, false);
        }

        public static void RenameDirectory(string path, string newPath)
        {
            Directory.Move(path, newPath);
        }

        public static string LoadTextFile(string filePath)
        {
            return File.ReadAllText(filePath);
        }
    }
}
