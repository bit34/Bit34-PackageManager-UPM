using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Com.Bit34games.PackageManager.Utilities
{
    /// <summary>
    /// Thin wrapper over the git command line.
    ///
    /// Every call funnels through <see cref="RunGit"/>, which exists for three
    /// reasons the per-method copies it replaced got wrong:
    ///   - arguments go through ArgumentList, so nothing has to be quoted or
    ///     escaped by hand (a hand-quoted `-c` flag used to make git refuse to
    ///     start, which silently emptied every remote version list);
    ///   - stdout and stderr are actually read, so a chatty command cannot fill
    ///     its pipe and deadlock against WaitForExit;
    ///   - the exit code is checked, so a failure surfaces instead of looking
    ///     like an empty result.
    /// </summary>
    public static class GitHelpers
    {
        //  MEMBERS
        /// <summary>
        /// stderr of the last failed call, for whoever reports the failure.
        /// </summary>
        public static string LastError { get; private set; } = "";


        //  METHODS
        public static bool GetVersion(out string version)
        {
            return RunGit(out version, "--version");
        }

        public static bool Clone(string directory, string gitURL)
        {
            string output;
            return RunGit(out output, "clone", "-q", gitURL, directory);
        }

        public static bool Fetch(string directory)
        {
            string output;
            return RunGit(out output, "-C", directory, "fetch", "-q");
        }

        public static bool CheckoutBranch(string directory, string branchName)
        {
            string output;
            return RunGit(out output, "-C", directory, "checkout", "-q", branchName);
        }

        public static bool InitSubmodules(string directory)
        {
            string output;
            return RunGit(out output, "-C", directory, "submodule", "update", "--init", "-q");
        }

        /// <summary>
        /// Tag names in a local clone. Empty list when git fails — check
        /// <see cref="LastError"/> to tell that apart from a repo with no tags.
        /// </summary>
        public static List<string> GetTags(string directory)
        {
            string output;
            if (RunGit(out output, "-C", directory, "tag") == false)
            {
                return new List<string>();
            }
            return ParseTagNames(output);
        }

        /// <summary>
        /// Tag names on a remote, without cloning it.
        /// </summary>
        public static List<string> GetRemoteTags(string url)
        {
            string output;
            if (RunGit(out output, "ls-remote", "--tags", url) == false)
            {
                return new List<string>();
            }
            return ParseTagNames(output);
        }

        /// <summary>
        /// Pulls tag names out of either `git tag` output (one name per line) or
        /// `git ls-remote --tags` output ("&lt;sha&gt;\trefs/tags/&lt;name&gt;").
        ///
        /// Annotated tags make ls-remote print a second, dereferenced line
        /// ending in "^{}"; those are skipped, since the name they carry is not
        /// a version anybody can check out.
        /// </summary>
        private static List<string> ParseTagNames(string output)
        {
            List<string> tags = new List<string>();

            foreach (string rawLine in output.Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.Length == 0)
                {
                    continue;
                }
                if (line.EndsWith("^{}", StringComparison.Ordinal))
                {
                    continue;
                }

                int separatorIndex = line.LastIndexOf('/');
                tags.Add(line.Substring(separatorIndex + 1));
            }

            return tags;
        }

        /// <summary>
        /// Runs git with the given arguments and collects stdout. Returns false
        /// when git could not be started or exited non-zero, leaving the reason
        /// in <see cref="LastError"/>.
        /// </summary>
        private static bool RunGit(out string stdout, params string[] arguments)
        {
            stdout = "";

            Process process = new Process();
            process.StartInfo.FileName               = "git";
            process.StartInfo.UseShellExecute        = false;
            process.StartInfo.CreateNoWindow         = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError  = true;
            //  ArgumentList quotes each argument for us, so paths with spaces
            //  and values with punctuation need no escaping at the call site.
            foreach (string argument in arguments)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            string stderr;
            try
            {
                process.Start();
                //  Read both pipes before waiting; a full pipe would otherwise
                //  block git while we block on it.
                stdout = process.StandardOutput.ReadToEnd();
                stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();
            }
            catch (Exception exception)
            {
                LastError = exception.Message;
                return false;
            }

            if (process.ExitCode != 0)
            {
                LastError = Describe(arguments) + "\n" + stderr.Trim();
                return false;
            }

            LastError = "";
            return true;
        }

        private static string Describe(string[] arguments)
        {
            return "git " + string.Join(" ", arguments);
        }
    }
}
