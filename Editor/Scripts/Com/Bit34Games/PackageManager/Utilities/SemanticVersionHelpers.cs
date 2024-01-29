using Com.Bit34games.PackageManager.VOs;

namespace Com.Bit34games.PackageManager.Utilities
{
    public static class SemanticVersionHelpers
    {
        public static SemanticVersionVO ParseVersion(string version)
        {
            int major = 0;
            int minor = 0;
            int patch = 0;

            int    current = 0;
            int    index   = 0;
            string token   = "";

            //  ignore leading v letter, if any
            if(version[current] == 'v')
            {
                current++;
            }

            index = version.IndexOf('.', current);
            if (index == -1)
            {
                token = version.Substring(current);
                major = int.Parse(token);
                return new SemanticVersionVO(major);
            }

            token = version.Substring(current, index-current);
            major = int.Parse(token);

            current = index+1;

            index = version.IndexOf('.', current);
            if (index == -1)
            {
                token = version.Substring(current);
                minor = int.Parse(token);
                return new SemanticVersionVO(major, minor);
            }

            token = version.Substring(current, index-current);
            minor = int.Parse(token);

            current = index+1;

            token = version.Substring(current);
            patch = int.Parse(token);
            return new SemanticVersionVO(major, minor, patch);
        }

        public static SemanticVersionVO ParseVersionFromTag(string version)
        {
            if (char.IsDigit(version[0]))
            {
                return ParseVersion(version);
            }

            int startIndex = version.LastIndexOf('v') + 1;
            return ParseVersion(version.Substring(startIndex));
        }

        public static SemanticVersionVO[] ParseVersionArray(string[] versions)
        {
            SemanticVersionVO[] parsedVersions = new SemanticVersionVO[versions.Length];
            for (int i = 0; i < versions.Length; i++)
            {
                parsedVersions[i] = ParseVersion(versions[i]);
            }
            return parsedVersions;
        }
    }
}