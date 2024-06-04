using System;
using System.Collections.Generic;

namespace Com.Bit34games.PackageManager.FileVOs
{
    [Serializable]
    public class RepositoryFileVO
    {
        public List<RepositoryPackageFileVO> packages = null;
    }
}