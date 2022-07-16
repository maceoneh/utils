using System;
using System.IO;

namespace es.dmoreno.utils.path
{
    public class PathHelper
    {
        static public string GetAppDataFolder()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + Path.DirectorySeparatorChar;
            return path;
        }

        static public string GetAppFolder()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar;
            return path;
        }
    }
}
