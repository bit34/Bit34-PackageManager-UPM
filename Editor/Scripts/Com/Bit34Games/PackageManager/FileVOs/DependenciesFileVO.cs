using System;
using System.Collections.Generic;

namespace Com.Bit34games.PackageManager.FileVOs
{
    [Serializable]
    public class DependenciesFileVO
    {
        public Dictionary<string, string> dependencies = null;
    }
}