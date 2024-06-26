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
        
        public static void DeleteDirectory(string target_dir)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(target_dir, false);
        }

        public static string LoadTextFile(string filePath)
        {
            return File.ReadAllText(filePath);
        }
    }
}
