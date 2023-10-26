using System.Collections.Generic;
using System.Diagnostics;

namespace Com.Bit34games.PackageManager.Utilities
{
    public static class GitHelpers
    {
        //  METHODS
        public static void Clone(string directory, string gitURL, bool waitForExit = true)
        {
            Process process = Process.Start("git", " clone -q " + gitURL + " " + directory);
            if(waitForExit)
            {
                process.WaitForExit();
            }
        }

        public static void Fetch(string directory, bool waitForExit = true)
        {
            Process process = Process.Start("git", " -C " + directory + " fetch -q");
            if(waitForExit)
            {
                process.WaitForExit();
            }
        }

        public static void CheckoutBranch(string directory, string branchName, bool waitForExit = true)
        {
            Process process = Process.Start("git", " -C " + directory + " checkout " + branchName + " -q");
            if(waitForExit)
            {
                process.WaitForExit();
            }
        }

        public static void InitSubmodules(string directory, bool waitForExit = true)
        {
            Process process = Process.Start("git", " -C " + directory + " submodule update --init -q");
            if(waitForExit)
            {
                process.WaitForExit();
            }
        }

        public static List<string> GetTags(string directory)
        {
            List<string> tags = new List<string>();
            Process process = new Process();
            process.StartInfo.FileName  = "git";
            process.StartInfo.Arguments = " -C "  + directory + " tag";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += (object sender, DataReceivedEventArgs e)=>{ tags.Add(e.Data); };
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();

            return tags;
        }

        public static List<string> GetRemoteTags(string url)
        {
//  git -c 'versionsort.suffix=-' ls-remote --tags --sort='v:refname' git@github.com:bit34/Bit34-Injector-UPM.git

            List<string> tags = new List<string>();
            Process process = new Process();
            process.StartInfo.FileName  = "git";
            process.StartInfo.Arguments = "-c 'versionsort.suffix=-' ls-remote --tags " + url;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += (object sender, DataReceivedEventArgs e)=>{ if (string.IsNullOrEmpty(e.Data)==false){tags.Add(e.Data);} };
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();

//  TODO    Use this instead  ==>>         | cut --delimiter='/' --fields=3 
            for (int j = 0; j < tags.Count; j++)
            {
                int separatorIndex = tags[j].LastIndexOf('/');
                tags[j] = tags[j].Substring(separatorIndex+1);
            }
    
            return tags;
        }
        
    }
}
