using System.Collections.Generic;
using Com.Bit34games.PackageManager.VOs;

namespace Com.Bit34games.PackageManager.Utilities
{
    public static class SemanticVersionHelpers
    {
        /// <summary>
        /// Parses "1", "1.2", "1.2.3" or any of those with a leading "v".
        /// Throws on anything else — callers reading user-authored files want
        /// to hear about a typo rather than have it swallowed.
        /// </summary>
        public static SemanticVersionVO ParseVersion(string version)
        {
            SemanticVersionVO parsed;
            if (TryParseVersion(version, out parsed) == false)
            {
                throw new System.FormatException("'" + version + "' is not a version number");
            }
            return parsed;
        }

        /// <summary>
        /// Same shapes as <see cref="ParseVersion"/>, reporting failure instead
        /// of throwing. Used where the input is whatever a git repository
        /// happens to be tagged with.
        /// </summary>
        public static bool TryParseVersion(string version, out SemanticVersionVO parsedVersion)
        {
            parsedVersion = null;

            if (string.IsNullOrEmpty(version))
            {
                return false;
            }

            int current = 0;

            //  ignore leading v letter, if any
            if (version[current] == 'v')
            {
                current++;
            }

            int major;
            int minor = 0;
            int patch = 0;

            int index = version.IndexOf('.', current);
            if (index == -1)
            {
                if (TryParseNumber(version.Substring(current), out major) == false) { return false; }
                parsedVersion = new SemanticVersionVO(major);
                return true;
            }

            if (TryParseNumber(version.Substring(current, index - current), out major) == false) { return false; }
            current = index + 1;

            index = version.IndexOf('.', current);
            if (index == -1)
            {
                if (TryParseNumber(version.Substring(current), out minor) == false) { return false; }
                parsedVersion = new SemanticVersionVO(major, minor);
                return true;
            }

            if (TryParseNumber(version.Substring(current, index - current), out minor) == false) { return false; }
            current = index + 1;

            if (TryParseNumber(version.Substring(current), out patch) == false) { return false; }

            parsedVersion = new SemanticVersionVO(major, minor, patch);
            return true;
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

        /// <summary>
        /// Parses a list of tag names, dropping the ones that are not version
        /// numbers. A repository is free to carry tags like "latest" or
        /// "release-1" and listing its versions should still work.
        /// </summary>
        public static SemanticVersionVO[] ParseVersionArray(string[] versions)
        {
            List<SemanticVersionVO> parsedVersions = new List<SemanticVersionVO>(versions.Length);
            for (int i = 0; i < versions.Length; i++)
            {
                SemanticVersionVO parsed;
                if (TryParseVersion(versions[i], out parsed))
                {
                    parsedVersions.Add(parsed);
                }
            }
            return parsedVersions.ToArray();
        }

        private static bool TryParseNumber(string text, out int value)
        {
            //  int.TryParse accepts leading/trailing whitespace and a sign,
            //  neither of which belongs in a version component.
            value = 0;
            if (text.Length == 0)
            {
                return false;
            }
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsDigit(text[i]) == false)
                {
                    return false;
                }
            }
            return int.TryParse(text, out value);
        }
    }
}
