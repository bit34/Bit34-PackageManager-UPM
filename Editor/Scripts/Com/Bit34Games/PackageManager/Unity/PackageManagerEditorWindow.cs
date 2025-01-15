using UnityEngine;
using UnityEditor;
using Com.Bit34games.PackageManager.Models;
using Com.Bit34games.PackageManager.Utilities;
using Com.Bit34games.PackageManager.VOs;
using Com.Bit34games.PackageManager.FileVOs;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Com.Bit34games.PackageManager.Constants;
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


        //  MEMBERS
        private PackageManagerModel             _packageManagerModel;
        private PackageManagerOperations        _packageManagerOperations;
        //      Messages
        private Rect                            _fullRect;
        private GUIStyle                        _loadingStyle;
        private string[]                        _loadingTextFrames;
        //      Tool bar
        private Rect                            _toolBarRect;
        //      Package list
        private Rect                            _packageListRect;
        private GUIStyle                        _packageListStyle;
        private Texture2D                       _packageListBackgroundTexture;
        private int                             _packageListSelection;
        private Vector2                         _packageListScrollPosition;
        //      Package list item
        private GUIStyle                        _packageListItemStyle;
        private Texture2D[]                     _packageStateIconTextures;
        //      Package detail
        private Rect                            _packageDetailRect;
        private GUIStyle                        _packageDetailStyle;
        private GUIStyle                        _packageDetailHeaderStyle;
        private GUIStyle                        _packageDetailTextStyle;
        //      Errors
        private Action<PackageManagerErrorVO>[] _errorDrawMethods;


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
            InitializeDrawing();

            if (_packageManagerModel == null)
            {
                _packageManagerModel      = new PackageManagerModel();
                _packageManagerOperations = new PackageManagerOperations(_packageManagerModel);

                DetectLoadedDependencies();
            }
            
            EditorApplication.update += WindowUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= WindowUpdate;
        }

        private void WindowUpdate()
        {
            if (_packageManagerModel.State == PackageManagerStates.Loading)
            {
                Repaint();
            }
        }

        private void OnGUI()
        {
            _fullRect = new Rect(0, 0, position.width, position.height);

            _packageManagerOperations.CheckPrerequirements();

            if (_packageManagerModel.State == PackageManagerStates.Error)
            {
                DrawForError();
            }
            else
            if (_packageManagerModel.State == PackageManagerStates.Ready)
            {
                DrawForReady();
            }
            else
            {
                DrawForLoading();
            }
        }

        private void InitializeDrawing()
        {
            if(_loadingStyle == null)
            {
                _loadingStyle = new GUIStyle();
                _loadingStyle.fontSize = 50;
                _loadingStyle.fontStyle = FontStyle.Bold;
                _loadingStyle.alignment = TextAnchor.MiddleCenter;
                _loadingStyle.wordWrap = false;
                _loadingStyle.normal.textColor = Color.white;

                _packageListStyle = new GUIStyle();
                _packageListStyle.margin = new RectOffset(PANEL_MARGIN, PANEL_MARGIN, PANEL_MARGIN, PANEL_MARGIN);
                _packageListBackgroundTexture = EditorGUIUtility.FindTexture("d_tranp");
                _packageListSelection = -1;

                _packageListItemStyle = new GUIStyle();
                _packageListItemStyle.normal.textColor = Color.black;
                _packageListItemStyle.alignment = TextAnchor.MiddleLeft;

                _packageStateIconTextures = new Texture2D[5];
                _packageStateIconTextures[(int)DependencyStates.NotInUse]     = EditorGUIUtility.FindTexture("d_tranp");
                _packageStateIconTextures[(int)DependencyStates.Installed]    = EditorGUIUtility.FindTexture("console.infoicon");//("d_toggle_on_focus");
                _packageStateIconTextures[(int)DependencyStates.NotInstalled] = EditorGUIUtility.FindTexture("console.erroricon");//("d_toggle_bg");
                _packageStateIconTextures[(int)DependencyStates.WrongVersion] = EditorGUIUtility.FindTexture("console.warnicon");//("d_toggle_on");
                _packageStateIconTextures[(int)DependencyStates.NotNeeded]    = EditorGUIUtility.FindTexture("d_console.infoicin.inactive.sml");//("d_toggle_mixed_bg");

                _packageDetailStyle = new GUIStyle();
                _packageDetailStyle.padding = new RectOffset(PANEL_MARGIN, PANEL_MARGIN, PANEL_MARGIN, PANEL_MARGIN);

                _packageDetailHeaderStyle = new GUIStyle(EditorStyles.boldLabel);

                _packageDetailTextStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
                
                _loadingTextFrames = new string[4]
                {
                    "Loading",
                    "Loading.",
                    "Loading..",
                    "Loading...",
                };

                _errorDrawMethods = new Action<PackageManagerErrorVO>[]
                {
                    DrawForErrorGitNotFound,                         //  GitNotFound,

                    DrawForErrorRepositoriesFileNotFound,            //  RepositoriesFileNotFound,
                    DrawForErrorRepositoriesFileBadFormat,           //  RepositoriesFileBadFormat,

                    DrawForErrorDependenciesFileNotFound,            //  DependenciesFileNotFound,
                    DrawForErrorDependenciesFileBadFormat,           //  DependenciesFileBadFormat,

                    DrawForErrorDependencyNotInRepository,           //  DependencyNotInRepository,
                    DrawForErrorDependencyAddedWithDifferentVersion, //  DependencyAddedWithDifferentVersion,
                };
            }
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

        private void DrawForError()
        {
            int                           methodIndex = (int)_packageManagerModel.Error.error;
            Action<PackageManagerErrorVO> method      = _errorDrawMethods[methodIndex];
            method(_packageManagerModel.Error);
        }

        private void DrawForErrorGitNotFound(PackageManagerErrorVO error)
        {
            EditorGUILayout.BeginVertical();
                GUILayout.BeginArea(_fullRect);
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.HelpBox(PackageManagerConstants.ERROR_TEXT_GIT_NOT_FOUND, MessageType.Warning, true);
                    EditorGUILayout.EndHorizontal();
                GUILayout.EndArea();
            EditorGUILayout.EndVertical();
        }

        private void DrawForErrorRepositoriesFileNotFound(PackageManagerErrorVO error)
        {
            EditorGUILayout.BeginVertical();
                GUILayout.BeginArea(_fullRect);
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.HelpBox(PackageManagerConstants.ERROR_TEXT_REPOSITORIES_FILE_NOT_FOUND, MessageType.Warning, true);
                    EditorGUILayout.EndHorizontal();
                GUILayout.EndArea();
            EditorGUILayout.EndVertical();
        }
        
        private void DrawForErrorRepositoriesFileBadFormat(PackageManagerErrorVO error)
        {
            EditorGUILayout.BeginVertical();
                GUILayout.BeginArea(_fullRect);
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.HelpBox(PackageManagerConstants.ERROR_TEXT_REPOSITORIES_FILE_BAD_FORMAT, MessageType.Warning, true);
                    EditorGUILayout.EndHorizontal();
                GUILayout.EndArea();
            EditorGUILayout.EndVertical();
        }

        private void DrawForErrorDependenciesFileNotFound(PackageManagerErrorVO error)
        {
            EditorGUILayout.BeginVertical();
                GUILayout.BeginArea(_fullRect);
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.HelpBox(PackageManagerConstants.ERROR_TEXT_DEPENDENCIES_FILE_NOT_FOUND, MessageType.Warning, true);
                    EditorGUILayout.EndHorizontal();
                GUILayout.EndArea();
            EditorGUILayout.EndVertical();
        }
        
        private void DrawForErrorDependenciesFileBadFormat(PackageManagerErrorVO error)
        {
            EditorGUILayout.BeginVertical();
                GUILayout.BeginArea(_fullRect);
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.HelpBox(PackageManagerConstants.ERROR_TEXT_DEPENDENCIES_FILE_BAD_FORMAT, MessageType.Warning, true);
                    EditorGUILayout.EndHorizontal();
                GUILayout.EndArea();
            EditorGUILayout.EndVertical();
        }

        private void DrawForErrorDependencyNotInRepository(PackageManagerErrorVO error)
        {
            PackageManagerErrorForDependencyNotInRepositoryVO castedError = (PackageManagerErrorForDependencyNotInRepositoryVO) error;

            string text = PackageManagerConstants.ERROR_TEXT_DEPENDENCY_NOT_IN_REPOSITORY;
            text += "\n";
            text += "\nPackage : " + castedError.packageName;

            EditorGUILayout.BeginVertical();
                GUILayout.BeginArea(_fullRect);
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.HelpBox(text, MessageType.Warning, true);
                    EditorGUILayout.EndHorizontal();
                GUILayout.EndArea();
            EditorGUILayout.EndVertical();
        }

        private void DrawForErrorDependencyAddedWithDifferentVersion(PackageManagerErrorVO error)
        {
            PackageManagerErrorForDependencyAddedWithDifferentVersionVO castedError = (PackageManagerErrorForDependencyAddedWithDifferentVersionVO)error;
            
            string text = PackageManagerConstants.ERROR_TEXT_DEPENDENCY_ADDED_WITH_DIFFERENT_VERSION;
            text += "\n";
            text += "\nPackage : " + castedError.packageName;
            text += "\n";
            text += "\nCurrent Version    : " + castedError.currentVersion;
            text += "\nCurrent Requesters :";
            for (int i = 0; i < castedError.currentRequesters.Length; i++)
            {
                string requester = castedError.currentRequesters[i];
                if (requester=="")
                {
                    text += "\n - " + PackageManagerConstants.DEPENDENCIES_JSON_FILENAME;
                }
                else
                {
                    text += "\n - " + requester;
                }
            }
            text += "\n";
            text += "\nNew Version   : " + castedError.newVersion;
            text += "\nNew Requester : " + castedError.newRequester;

            EditorGUILayout.BeginVertical();
                GUILayout.BeginArea(_fullRect);
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.HelpBox(text, MessageType.Warning, true);
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
            
            EditorGUILayout.HelpBox(PackageManagerConstants.HELP_TEXT, MessageType.Warning, true);

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
            string  label = packageName;
            Texture icon  = null;

            DependencyStates dependencyState = _packageManagerModel.GetDependencyState(packageName);
            icon = _packageStateIconTextures[(int)dependencyState];

            GUILayout.Box(icon, GUILayout.Width(16), GUILayout.Height(16));
            return GUILayout.Button(new GUIContent(label), EditorStyles.miniButtonLeft);
        }

        private void DrawPackageDetail()
        {
            GUILayout.BeginArea(_packageDetailRect, _packageDetailStyle);

            if (_packageListSelection != -1)
            {
                string packageName         = _packageManagerModel.GetPackageName(_packageListSelection);
                int    packageVersionCount = _packageManagerModel.GetPackageVersionCount(_packageListSelection);

                GUILayout.Label("State: " + _packageManagerModel.GetDependencyState(packageName), _packageDetailHeaderStyle, GUILayout.Width(200));

                EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Package: ", _packageDetailHeaderStyle, GUILayout.Width(60));
                    GUILayout.Label(packageName, _packageDetailTextStyle);
                    GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(16);

                SemanticVersionVO packageVersion = null;
                if (_packageManagerModel.GetDependencyState(packageName) != DependencyStates.NotInUse)
                {
                    packageVersion = _packageManagerModel.GetDependencyVersion(packageName);
                    string packageVersionText = packageVersion.ToString();

                    SemanticVersionVO installedVersion = _packageManagerModel.GetInstalledVersion(packageName);
                    if (installedVersion != null && installedVersion != packageVersion)
                    {
                        packageVersionText += " ( installed: "+installedVersion.ToString()+" )";
                    }
                    
                    EditorGUILayout.BeginHorizontal();
                        GUILayout.Label("Version:", _packageDetailHeaderStyle, GUILayout.Width(60));
                        GUILayout.Label(packageVersionText, _packageDetailTextStyle);
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

                if (_packageManagerModel.GetDependencyState(packageName) == DependencyStates.Installed)
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

        private void DetectLoadedDependencies()
        {
            _packageManagerModel.SetAsLoading();
            Repaint();

            _packageManagerOperations.DetectClonedDependencies();
            if (_packageManagerModel.Error != null)
            {
                Repaint();
                return;
            }

            _packageManagerModel.SetAsReady();
            Repaint();
        }

        private void ReloadDependencies()
        {
            Scene  activeScene     = EditorSceneManager.GetActiveScene();
            string activeScenePath = activeScene.path;
            Scene  tempScene       = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

            _packageManagerModel.SetAsLoading();
            Repaint();

            _packageManagerOperations.CloneDependencies();
//                EditorUtility.RequestScriptReload();

            if (string.IsNullOrEmpty(activeScenePath) == false)
            {
                EditorSceneManager.OpenScene(activeScenePath);
            }

            _packageManagerModel.SetAsReady();
            Repaint();
        }

        private void ReloadPackageVersions(string packageName)
        {
            int    packageIndex = _packageManagerModel.FindPackageIndex(packageName);
            string packageURL   = _packageManagerModel.GetPackageURL(packageIndex);
            
//            Task task = Task.Run(() => 
//            {
                _packageManagerModel.PackageVersionsReloadStarted(packageName);
                List<string>        tags     = GitHelpers.GetRemoteTags(packageURL);
                SemanticVersionVO[] versions = SemanticVersionHelpers.ParseVersionArray(tags.ToArray());
                _packageManagerModel.PackageVersionsReloadCompleted(packageName, versions);

                if (_packageListSelection != -1 &&
                    _packageManagerModel.GetPackageName(_packageListSelection) == packageName)
                {
                    Repaint();
                }
//            });
        }

    }
}
