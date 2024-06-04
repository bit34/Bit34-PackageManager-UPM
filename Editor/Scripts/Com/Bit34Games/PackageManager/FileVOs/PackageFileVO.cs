using System;
using System.Collections.Generic;

namespace Com.Bit34games.PackageManager.FileVOs
{
    [Serializable]
    class PackageFileVO
    {
        public string                     name         = "";
        public string                     displayName  = "";
        public string                     version      = "";
        public string                     description  = "";
        public Dictionary<string, string> dependencies = null;
    }
}
