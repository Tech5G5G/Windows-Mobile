using System;
using System.IO;

namespace Windows_Mobile.Indexing.Utils
{
    public static class PathUtils
    {
        public static string[] IndexFolder(string folderPath)
        {
            string[] files = Directory.GetFiles(folderPath);

            string[] subDirectories = Directory.GetDirectories(folderPath);
            if (subDirectories.Length != 0)
            {
                foreach (string directory in subDirectories)
                {
                    string[] subfiles = IndexFolder(directory);
                    foreach (string subfile in subfiles)
                        files = [.. files, subfile];
                }
            }

            return files;
        }
    }
}
