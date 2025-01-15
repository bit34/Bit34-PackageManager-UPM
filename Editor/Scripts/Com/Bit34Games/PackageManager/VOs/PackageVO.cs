using System.Collections.Generic;
using Com.Bit34games.PackageManager.Constants;

namespace Com.Bit34games.PackageManager.VOs
{
    internal class PackageVO
    {
        //  MEMBERS
        public readonly string       name;
        public readonly string       url;
        public SemanticVersionVO[]   versions;
        public SemanticVersionVO     installedVersion;
        public DependencyStates      dependencyState;
        public SemanticVersionVO     dependencyVersion;
        public readonly List<string> dependencyParents;


        //  CONSTRUCTORS
        public PackageVO(string              name,
                         string              url,
                         SemanticVersionVO[] versions)
        {
            this.name         = name;
            this.url          = url;
            this.versions     = versions;
            dependencyState   = DependencyStates.NotInUse;
            dependencyParents = new List<string>();
        }

    }
}