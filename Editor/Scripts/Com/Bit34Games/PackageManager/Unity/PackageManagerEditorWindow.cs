using UnityEngine;
using UnityEditor;
using Com.Bit34games.PackageManager.Models;
using Com.Bit34games.PackageManager.Utilities;
using Com.Bit34games.PackageManager.VOs;
using Com.Bit34games.PackageManager.FileVOs;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Com.Bit34games.PackageManager.Constants;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;


namespace Com.Bit34games.PackageManager.Unity
{
//X    [InitializeOnLoad]
    public class PackageManagerEditorWindow : EditorWindow
    {
        //  CONSTANTS
        private const float  TOOLBAR_PANEL_HEIGHT   = 54;
        private const float  LIST_PANEL_WIDTH       = 220;
        private const int    PANEL_MARGIN           = 5;
        private const string BETA_HELP_TEXT         = "Usage: (For more details checkout Bit34Games.com)\n"+
                                                      "- Add your packages to Assets/Bit34/repositories.json\n"+
                                                      "- Add your dependencies to Assets/Bit34/dependencies.json\n"+
                                                      "- Everytime you modify dependencies.json press Reload button (and wait a little).";


        //  MEMBERS
        private PackageManagerModel      _packageManagerModel;
        private PackageManagerOperations _packageManagerOperations;
        //      Messages
        private Rect                     _fullRect;
        private GUIStyle                 _loadingStyle;
        private string[]                 _loadingTextFrames;
        //      Tool bar
        private Rect                     _toolBarRect;
        //      Package list
        private Rect                     _packageListRect;
        private GUIStyle                 _packageListStyle;
        private Texture2D                _packageListBackgroundTexture;
        private int                      _packageListSelection;
        private Vector2                  _packageListScrollPosition;
        //      Package list item
        private GUIStyle                 _packageListItemStyle;
        private Texture2D                _notloadedIconTexture;
        private Texture2D                _loadedIconTexture;
        private Texture2D                _outdatedIconTexture;
        //      Package detail
        private Rect                     _packageDetailRect;
        private GUIStyle                 _packageDetailStyle;
        private GUIStyle                 _packageDetailHeaderStyle;
        private GUIStyle                 _packageDetailTextStyle;
        

        //  METHODS
        [MenuItem("Bit34/Package Manager")]
        static void Init()
        {
            PackageManagerEditorWindow window = (PackageManagerEditorWindow)EditorWindow.GetWindow(typeof(PackageManagerEditorWindow));
            window.titleContent = new GUIContent("Bit34 - Package Manager");
            window.Show();
        }

        private void OnEnable()
        {
            _loadingStyle = new GUIStyle();
            _loadingStyle.fontSize = 50;
            _loadingStyle.fontStyle = FontStyle.Bold;
            _loadingStyle.alignment = TextAnchor.MiddleCenter;
            _loadingStyle.wordWrap = false;
            _loadingStyle.normal.textColor = Color.white;

            _loadingTextFrames = new string[4]
            {
                "Loading",
                "Loading.",
                "Loading..",
                "Loading...",
            };

            _packageListStyle = new GUIStyle();
            _packageListStyle.margin = new RectOffset(PANEL_MARGIN, PANEL_MARGIN, PANEL_MARGIN, PANEL_MARGIN);
            _packageListBackgroundTexture = EditorGUIUtility.FindTexture("d_tranp");
            _packageListSelection = -1;

            _packageListItemStyle = new GUIStyle();
            _packageListItemStyle.normal.textColor = Color.black;
            _notloadedIconTexture = EditorGUIUtility.FindTexture("d_tranp");
            _loadedIconTexture    = EditorGUIUtility.FindTexture("console.infoicon");
            _outdatedIconTexture  = EditorGUIUtility.FindTexture("console.warnicon");

            _packageDetailStyle = new GUIStyle();
            _packageDetailStyle.padding = new RectOffset(PANEL_MARGIN, PANEL_MARGIN, PANEL_MARGIN, PANEL_MARGIN);

            _packageDetailHeaderStyle = new GUIStyle(EditorStyles.boldLabel);

            _packageDetailTextStyle = new GUIStyle(EditorStyles.wordWrappedLabel);

            if (_packageManagerModel == null)
            {
                _packageManagerModel      = new PackageManagerModel();
                _packageManagerOperations = new PackageManagerOperations(_packageManagerModel);

                ReadAlreadyLoadedDependencies();
            }
            
            EditorApplication.update += WindowUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= WindowUpdate;
        }

        private void WindowUpdate()
        {
            if( _packageManagerModel.State == PackageManagerStates.NotInitialized)
            {
                Repaint();
            }
        }

        private void OnGUI()
        {
            _fullRect = new Rect(0, 0, position.width, position.height);

            string version;
            if (GitHelpers.GetVersion(out version)==false)
            {
                DrawForCanNotFoundGit();
                return;
            }

            string repositoriesFilePath = PackageManagerConstants.REPOSITORIES_JSON_FOLDER + PackageManagerConstants.REPOSITORIES_JSON_FILENAME;
            if (File.Exists(repositoriesFilePath) == false)
            {
                DrawForCanNotFoundRepositories();
                return;
            }

            string dependenciesFilePath = PackageManagerConstants.DEPENDENCIES_JSON_FOLDER + PackageManagerConstants.DEPENDENCIES_JSON_FILENAME;
            if (File.Exists(dependenciesFilePath) == false)
            {
                DrawForCanNotFoundDependencies();
                return;
            }

            if (_packageManagerModel.State == PackageManagerStates.Ready)
            {
                DrawForReady();
            }
            else
            {
                DrawForLoading();
            }
        }

        private void DrawForCanNotFoundGit()
        {
            EditorGUILayout.BeginVertical();
                GUILayout.BeginArea(_fullRect);
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.HelpBox(PackageManagerConstants.ERROR_TEXT_CAN_NOT_FOUND_GIT, MessageType.Warning, true);
                    EditorGUILayout.EndHorizontal();
                GUILayout.EndArea();
            EditorGUILayout.EndVertical();
        }

        private void DrawForCanNotFoundRepositories()
        {
            EditorGUILayout.BeginVertical();
                GUILayout.BeginArea(_fullRect);
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.HelpBox(PackageManagerConstants.ERROR_TEXT_CAN_NOT_FOUND_REPOSITORIES, MessageType.Warning, true);
                    EditorGUILayout.EndHorizontal();
                GUILayout.EndArea();
            EditorGUILayout.EndVertical();
        }

        private void DrawForCanNotFoundDependencies()
        {
            EditorGUILayout.BeginVertical();
                GUILayout.BeginArea(_fullRect);
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.HelpBox(PackageManagerConstants.ERROR_TEXT_CAN_NOT_FOUND_DEPENDENCIES, MessageType.Warning, true);
                    EditorGUILayout.EndHorizontal();
                GUILayout.EndArea();
            EditorGUILayout.EndVertical();
        }

        private void DrawForLoading()
        {
            EditorGUILayout.BeginVertical();
                GUILayout.BeginArea(_fullRect);
                    EditorGUILayout.BeginHorizontal();
                        int frame = DateTime.Now.Second % 4;
                        GUI.Label(_fullRect, _loadingTextFrames[frame], _loadingStyle);
                    EditorGUILayout.EndHorizontal();
                GUILayout.EndArea();
            EditorGUILayout.EndVertical();
        }

        private void DrawForReady()
        {
            _toolBarRect       = new Rect(0,                0,                    position.width,                  TOOLBAR_PANEL_HEIGHT);
            _packageListRect   = new Rect(0,                TOOLBAR_PANEL_HEIGHT, LIST_PANEL_WIDTH,                position.height-TOOLBAR_PANEL_HEIGHT);
            _packageDetailRect = new Rect(LIST_PANEL_WIDTH, TOOLBAR_PANEL_HEIGHT, position.width-LIST_PANEL_WIDTH, position.height-TOOLBAR_PANEL_HEIGHT);

            GUI.Box(_packageListRect, _packageListBackgroundTexture);
            EditorGUILayout.BeginVertical();
                DrawToolBar();
                EditorGUILayout.BeginHorizontal();
                    DrawPackageList();
                    DrawPackageDetail();
                EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawToolBar()
        {
            GUILayout.BeginArea(_toolBarRect);
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.HelpBox(BETA_HELP_TEXT, MessageType.Warning, true);

            if (GUILayout.Button("Reload", GUILayout.Height(TOOLBAR_PANEL_HEIGHT)))
            {
                Scene activeScene = EditorSceneManager.GetActiveScene();
                if (activeScene.isDirty)
                {
                    EditorUtility.DisplayDialog("Warning", "Please save your scene before updating packages.", "Ok");
                }
                else
                {
                    ReloadDependencies();
                    
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawPackageList()
        {
            GUILayout.BeginArea(_packageListRect, _packageListStyle);

            _packageListScrollPosition = GUILayout.BeginScrollView(_packageListScrollPosition, 
                                                                   GUILayout.Width(_packageListRect.width), 
                                                                   GUILayout.Height(_packageListRect.height));

            for (int i = 0; i < _packageManagerModel.PackageCount; i++)
            {
                EditorGUILayout.BeginHorizontal();
                if(DrawPackageListItem(_packageManagerModel.GetPackageName(i)))
                {
                    _packageListSelection = i;
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private bool DrawPackageListItem(string packageName)
        {
            if (_packageManagerModel.HasDependency(packageName))
            {
                GUILayout.Box(_loadedIconTexture, GUILayout.Width(16), GUILayout.Height(16));
            }
            else
            {
                GUILayout.Box(_notloadedIconTexture, GUILayout.Width(16), GUILayout.Height(16));
            }
            
            return GUILayout.Button(new GUIContent(packageName));
        }

        private void DrawPackageDetail()
        {
            GUILayout.BeginArea(_packageDetailRect, _packageDetailStyle);

            if (_packageListSelection != -1)
            {
                string packageName         = _packageManagerModel.GetPackageName(_packageListSelection);
                int    packageVersionCount = _packageManagerModel.GetPackageVersionCount(_packageListSelection);

                EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Package: ", _packageDetailHeaderStyle, GUILayout.Width(60));
                    GUILayout.Label(packageName, _packageDetailTextStyle);
                    GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(16);

                SemanticVersionVO packageVersion = null;
                if (_packageManagerModel.HasDependency(packageName))
                {
                    packageVersion = _packageManagerModel.GetDependencyVersion(packageName);
                    
                    EditorGUILayout.BeginHorizontal();
                        GUILayout.Label("Version:", _packageDetailHeaderStyle, GUILayout.Width(60));
                        GUILayout.Label(packageVersion.ToString(), _packageDetailTextStyle);
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(16);
                }

                if (_packageManagerModel.IsPackageVersionsUpdating(packageName))
                {
                    GUILayout.Label("Available versions :", _packageDetailHeaderStyle);
                    GUILayout.Label("Updating, please wait...", _packageDetailTextStyle);
                }
                else
                {
                    EditorGUILayout.BeginHorizontal(GUILayout.Width(200));
                        GUILayout.Label("Available versions :", _packageDetailHeaderStyle);
                        if (GUILayout.Button("Update", new GUILayoutOption[]{GUILayout.Height(16), GUILayout.Width(100)}))
                        {
                            ReloadPackageVersions(packageName);
                            Repaint();
                        }
                    EditorGUILayout.EndHorizontal();
                    if (packageVersionCount == 0)
                    {
                        GUILayout.Label(" Not loaded", _packageDetailTextStyle);
                    }
                    else
                    {
                        for (int i = 0; i < packageVersionCount; i++)
                        {
                            GUILayout.Label(" - " + _packageManagerModel.GetPackageVersion( _packageListSelection, i), _packageDetailTextStyle);
                        }
                    }
                }

                GUILayout.Space(16);

                if (_packageManagerModel.HasDependency(packageName))
                {
                    PackageFileVO packageFile = PackageManagerHelpers.LoadPackageJson(packageName, packageVersion);

                    GUILayout.Label(packageFile.displayName, _packageDetailHeaderStyle);

                    GUILayout.Space(16);

                    GUILayout.Label(packageFile.description, _packageDetailTextStyle);

                    GUILayout.Space(16);

                    if (packageFile.dependencies != null && packageFile.dependencies.Count > 0)
                    {
                        GUILayout.Label("Dependencies :", _packageDetailHeaderStyle);
                        foreach (string dependencyName in packageFile.dependencies.Keys)
                        {
                            SemanticVersionVO dependencyVersion = SemanticVersionHelpers.ParseVersionFromTag(packageFile.dependencies[dependencyName]);
                            GUILayout.Label(" - "+ dependencyName + "@" + dependencyVersion, _packageDetailTextStyle);
                        }
                        GUILayout.Space(16);
                    }
                }
            }
            GUILayout.EndArea();
        }

        private void ReadAlreadyLoadedDependencies()
        {
            Task task = Task.Run(() => 
            {
                _packageManagerModel.Clear();
                _packageManagerOperations.LoadRepositories();
                _packageManagerOperations.GetClonedDependencies();
                _packageManagerModel.SetAsReady();
                Repaint();
            });
        }

        private void ReloadDependencies()
        {
            Scene  activeScene     = EditorSceneManager.GetActiveScene();
            string activeScenePath = activeScene.path;
            Scene  tempScene       = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

            Task task = Task.Run(() => 
            {
                _packageManagerModel.SetAsReloading();
                Repaint();

                _packageManagerModel.Clear();
                _packageManagerOperations.LoadRepositories();
                _packageManagerOperations.CloneDependencies();
//                EditorUtility.RequestScriptReload();

                if (string.IsNullOrEmpty(activeScenePath) == false)
                {
                    EditorSceneManager.OpenScene(activeScenePath);
                }

                _packageManagerModel.SetAsReady();
                Repaint();
            });
        }

        private void ReloadPackageVersions(string packageName)
        {
            int    packageIndex = _packageManagerModel.FindPackageIndex(packageName);
            string packageURL   = _packageManagerModel.GetPackageURL(packageIndex);
            
            Task task = Task.Run(() => 
            {
                _packageManagerModel.PackageVersionsReloadStarted(packageName);
                List<string>        tags     = GitHelpers.GetRemoteTags(packageURL);
                SemanticVersionVO[] versions = SemanticVersionHelpers.ParseVersionArray(tags.ToArray());
                _packageManagerModel.PackageVersionsReloadCompleted(packageName, versions);

                if (_packageListSelection != -1 &&
                    _packageManagerModel.GetPackageName(_packageListSelection) == packageName)
                {
                    Repaint();
                }
            });
        }
    }
}
