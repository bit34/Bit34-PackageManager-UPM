using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Com.Bit34games.PackageManager.Utilities
{
    public static class GitHelpers
    {
        //  METHODS
        public static bool GetVersion(out string version)
        {
            string output = "";

            Process process = new Process();
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName  = "git";
            process.StartInfo.Arguments = " --version";
            process.OutputDataReceived += (object sender, DataReceivedEventArgs e)=>{ output += e.Data; };
            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();
            }
            catch(Exception exception)
            {
                version = exception.Message;
                return false;
            }

            version = output;
            return true;
        }

        public static void Clone(string directory, string gitURL, bool waitForExit = true)
        {
            string output = "";

            Process process = new Process();
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = "git";
            process.StartInfo.Arguments = " clone -q " + gitURL + " " + directory;
            process.OutputDataReceived += (object sender, DataReceivedEventArgs e)=>{ output += e.Data; };
            process.Start();
            if(waitForExit)
            {
                process.WaitForExit();
            }
        }

        public static void Fetch(string directory, bool waitForExit = true)
        {
            Process process = new Process();
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = "git";
            process.StartInfo.Arguments = " -C " + directory + " fetch -q";
            process.Start();
            if(waitForExit)
            {
                process.WaitForExit();
            }
        }

        public static void CheckoutBranch(string directory, string branchName, bool waitForExit = true)
        {
            Process process = new Process();
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = "git";
            process.StartInfo.Arguments = " -C " + directory + " checkout " + branchName + " -q";
            process.Start();
            if(waitForExit)
            {
                process.WaitForExit();
            }
        }

        public static void InitSubmodules(string directory, bool waitForExit = true)
        {
            Process process = new Process();
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = "git";
            process.StartInfo.Arguments = " -C " + directory + " submodule update --init -q";
            process.Start();
            if(waitForExit)
            {
                process.WaitForExit();
            }
        }

        public static List<string> GetTags(string directory)
        {
            List<string> tags = new List<string>();
            Process process = new Process();
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName  = "git";
            process.StartInfo.Arguments = " -C "  + directory + " tag";
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
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName  = "git";
            process.StartInfo.Arguments = "-c 'versionsort.suffix=-' ls-remote --tags " + url;
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
