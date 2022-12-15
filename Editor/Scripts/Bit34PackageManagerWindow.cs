using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;  //   "com.unity.nuget.newtonsoft-json": "2.0.0",
using UnityEngine;
using UnityEditor;

namespace Com.Bit34games.Unity.Editor
{
    [Serializable]
    class PackageJson
    {
        public string                     name         = "";
        public string                     displayName  = "";
        public string                     version      = "";
        public string                     description  = "";
        public Dictionary<string, string> dependencies = null;
    }

    class PackageItem
    {
        public readonly string name;
        public readonly string displayName;
        public readonly string version;
        public readonly string description;

        public PackageItem(string name,
                           string displayName,
                           string version,
                           string description)
        {
            this.name        = name;
            this.displayName = displayName;
            this.version     = version;
            this.description = description;
        }
    }

    public class Bit34PackageManagerWindow : EditorWindow
    {
        //  CONSTANTS
        private const float  LOAD_PANEL_HEIGHT          = 100;
        private const float  LIST_PANEL_WIDTH           = 230;
        private const float  DETAIL_PANEL_MIN_WIDTH     = 100;
        private const string PACKAGE_CACHE_FOLDER       = "Assets/Bit34/Packages/";
        private const string PACKAGE_REVERSE_URL_PREFIX = "com.bit34games.";
        private const string PACKAGE_NAME_PREFIX        = "bit34-";
        private const string PACKAGE_NAME_POSTFIX       = "-upm";
        private const string GITHUB_PROFILE_URL         = "https://github.com/bit34/";
        private const string GIT_EXTENSION              = ".git";
        private const string VERSION_BRANCH_PREFIX      = "v";

        //  MEMBERS
        //      Load panel
        private Rect              _loadPanelRect;
        private string            _packageName    = "";
        private string            _packageVersion = "";
        //      List panel
        private List<PackageItem> _list;
        private int               _listSelection;
        private Rect              _listPanelRect;
        private Vector2           _listPanelScroll;
        private GUIStyle          _listItemStyle;
        //      Detail panel
        private Rect              _detailPanelRect;

        //  METHODS
        [MenuItem("Bit34/Package Manager")]
        static void Init()
        {
            Bit34PackageManagerWindow window = (Bit34PackageManagerWindow)EditorWindow.GetWindow(typeof(Bit34PackageManagerWindow));
            window.titleContent = new GUIContent("Bit34 - Package Manager");
            window.Show();
        }

        private void OnEnable()
        {
            _list = new List<PackageItem>();
            _listItemStyle = new GUIStyle();
            _listItemStyle.normal.textColor = Color.black;
            _listSelection = -1;

            ReadLoadedPackages();
        }

        private void OnGUI()
        {
            DrawLoadPanel();

            EditorGUILayout.BeginHorizontal();

            DrawListPanel();
            DrawDetailPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLoadPanel()
        {
            _loadPanelRect = new Rect(0, 0, position.width, LOAD_PANEL_HEIGHT);
            GUILayout.BeginArea(_loadPanelRect);

            GUILayout.Label("Load a package", EditorStyles.boldLabel);

            _packageName    = EditorGUILayout.TextField("Name", _packageName).ToLower();
            _packageVersion = EditorGUILayout.TextField("Version", _packageVersion);

            if (GUILayout.Button("Install"))
            {
                if (Directory.Exists(PACKAGE_CACHE_FOLDER) == false)
                {
                    Process.Start("mkdir", " " + PACKAGE_CACHE_FOLDER).WaitForExit();
                    AssetDatabase.Refresh();
                }

                LoadPackage(_packageName, _packageVersion);
            }
            GUILayout.EndArea();
        }

        private void DrawListPanel()
        {
            _listPanelRect = new Rect(0, LOAD_PANEL_HEIGHT, LIST_PANEL_WIDTH, position.height);
            GUILayout.BeginArea(_listPanelRect);

            _listPanelScroll = GUILayout.BeginScrollView(_listPanelScroll);
            for (int i = 0; i < _list.Count; i++)
            {
                PackageItem item = _list[i];
                if(DrawListItem(item))
                {
                    _listSelection = i;
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private bool DrawListItem(PackageItem listItem)
        {
            return GUILayout.Button(new GUIContent(listItem.name + " @ " + listItem.version));
        }

        private void DrawDetailPanel()
        {
            _detailPanelRect = new Rect(LIST_PANEL_WIDTH, LOAD_PANEL_HEIGHT, Mathf.Max(DETAIL_PANEL_MIN_WIDTH, position.width - LIST_PANEL_WIDTH), position.height);
            GUILayout.BeginArea(_detailPanelRect);

            if (_listSelection != -1)
            {
                PackageItem item = _list[_listSelection];

                GUILayout.Label(item.displayName, EditorStyles.boldLabel);
                GUILayout.Label(item.description, EditorStyles.label);
            }

            GUILayout.EndArea();
        }

        private void ReadLoadedPackages()
        {
            if (Directory.Exists(PACKAGE_CACHE_FOLDER))
            {
                string[] packageFolderPaths = Directory.GetDirectories(PACKAGE_CACHE_FOLDER);

                for (int i = 0; i < packageFolderPaths.Length; i++)
                {
                    string      packageJsonPath    = packageFolderPaths[i] + Path.DirectorySeparatorChar + "package.json";
                    string      packageJsonContent = File.ReadAllText(packageJsonPath);
                    PackageJson packageJson        = JsonConvert.DeserializeObject<PackageJson>(packageJsonContent);

                    PackageItem listItem = new PackageItem(packageJson.name,
                                                        packageJson.displayName,
                                                        packageJson.version,
                                                        packageJson.description);
                    _list.Add(listItem);
                }
            }
        }

        private void LoadPackage(string packageName, string packageVersion, int depth = 0)
        {
            string tabs = new string('-', depth) + ">";
//            UnityEngine.Debug.Log(tabs + "Loading package : " + packageName +"@"+packageVersion);

            string packageGitUrl     = GITHUB_PROFILE_URL + PACKAGE_NAME_PREFIX + packageName + PACKAGE_NAME_POSTFIX + GIT_EXTENSION;
            string packageFolderName = PACKAGE_REVERSE_URL_PREFIX + packageName + "@" + packageVersion;

            if (Directory.Exists(PACKAGE_CACHE_FOLDER + packageFolderName) == false)
            {
                //  Clone repository
                Process.Start("git", " clone -q " + packageGitUrl + " " + PACKAGE_CACHE_FOLDER + packageFolderName).WaitForExit();
            }
            else
            {
                //  Fetch latest files
                Process.Start("git", " -C " + PACKAGE_CACHE_FOLDER + packageFolderName + " fetch -q").WaitForExit();
            }
            AssetDatabase.Refresh();

            //  Checkout version branch
            Process.Start("git", " -C " + PACKAGE_CACHE_FOLDER + packageFolderName + " checkout -q -b " + VERSION_BRANCH_PREFIX + _packageVersion).WaitForExit();
            AssetDatabase.Refresh();

            //  Parse package.json file
            string      packageJsonPath    = PACKAGE_CACHE_FOLDER + packageFolderName + Path.DirectorySeparatorChar + "package.json";
            string      packageJsonContent = File.ReadAllText(packageJsonPath);
            PackageJson packageJson        = JsonConvert.DeserializeObject<PackageJson>(packageJsonContent);

            PackageItem listItem = new PackageItem(packageJson.name,
                                                   packageJson.displayName,
                                                   packageJson.version,
                                                   packageJson.description);
            _list.Add(listItem);

            //  Handle dependencies
            if( packageJson.dependencies !=  null)
            {
                foreach (var key in packageJson.dependencies.Keys)
                {
                    string value = packageJson.dependencies[key];

                    string dependencyReverseUrl = key;
                    string dependencyName       = dependencyReverseUrl.Substring(dependencyReverseUrl.LastIndexOf('.')+1);
                    string dependencyVersion    = value.Substring(value.IndexOf('#')+2);
                    LoadPackage(dependencyName, dependencyVersion, depth+1);
                }
            }
        }
    }
}
/*
//Console.WriteLine(CommandOutput("git status"));

    public static string CommandOutput(string command,
                                       string workingDirectory = null)
    {
        try
        {
            ProcessStartInfo procStartInfo = new ProcessStartInfo("cmd", "/c " + command);

            procStartInfo.RedirectStandardError = procStartInfo.RedirectStandardInput = procStartInfo.RedirectStandardOutput = true;
            procStartInfo.UseShellExecute = false;
            procStartInfo.CreateNoWindow = true;
            if (null != workingDirectory)
            {
                procStartInfo.WorkingDirectory = workingDirectory;
            }

            Process proc = new Process();
            proc.StartInfo = procStartInfo;
            proc.Start();

            StringBuilder sb = new StringBuilder();
            proc.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e)
            {
                sb.AppendLine(e.Data);
            };
            proc.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e)
            {
                sb.AppendLine(e.Data);
            };

            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();
            return sb.ToString();
        }
        catch (Exception objException)
        {
            return $"Error in command: {command}, {objException.Message}";
        }
    }
Share

*/