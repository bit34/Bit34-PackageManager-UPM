using UnityEngine;
using UnityEditor;
using Com.Bit34games.PackageManager.Models;
using Com.Bit34games.PackageManager.Utilities;
using Com.Bit34games.PackageManager.VOs;
using Com.Bit34games.PackageManager.FileVOs;

namespace Com.Bit34games.Unity.Editor
{

    public class Bit34PackageManagerWindow : EditorWindow
    {
        //  CONSTANTS
        private const float  TOOLBAR_PANEL_HEIGHT   = 20;
        private const float  LIST_PANEL_WIDTH       = 220;
        private const int    PANEL_MARGIN           = 5;


        //  MEMBERS
        private RepositoriesModel    _repositoriesModel;
        private RepositoryOperations _repositoryOperations;
        //      Tool bar
        private Rect                _toolBarRect;
        //      Package list
        private Rect                _packageListRect;
        private GUIStyle            _packageListStyle;
        private Texture2D           _packageListBackgroundTexture;
        private int                 _packageListSelection;
        private Vector2             _packageListScrollPosition;
        //      Package list item
        private GUIStyle            _packageListItemStyle;
        private Texture2D           _notloadedIconTexture;
        private Texture2D           _loadedIconTexture;
        private Texture2D           _outdatedIconTexture;
        //      Package detail
        private Rect                _packageDetailRect;
        private GUIStyle            _packageDetailStyle;


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
            _repositoriesModel    = new RepositoriesModel();
            _repositoryOperations = new RepositoryOperations(_repositoriesModel);

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
        }

        private void OnGUI()
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
            if (GUILayout.Button("Reload"))
            {
                ReloadRepositories();
                _repositoryOperations.LoadDependencies();
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

            for (int i = 0; i < _repositoriesModel.PackageCount; i++)
            {
                RepositoryPackageVO package = _repositoriesModel.GetPackage(i);

                EditorGUILayout.BeginHorizontal();
                if(DrawPackageListItem(package))
                {
                    _packageListSelection = i;
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private bool DrawPackageListItem(RepositoryPackageVO package)
        {
            if (_repositoriesModel.HasDependency(package.name))
            {
                GUILayout.Box(_loadedIconTexture, GUILayout.Width(16), GUILayout.Height(16));
            }
            else
            {
                GUILayout.Box(_notloadedIconTexture, GUILayout.Width(16), GUILayout.Height(16));
            }
            
            return GUILayout.Button(new GUIContent(package.name));
        }

        private void DrawPackageDetail()
        {
            GUILayout.BeginArea(_packageDetailRect, _packageDetailStyle);

            if (_packageListSelection != -1)
            {
                RepositoryPackageVO package = _repositoriesModel.GetPackage(_packageListSelection);

                GUILayout.Label(package.name, EditorStyles.boldLabel);
                GUILayout.Space(16);

                if (_repositoriesModel.HasDependency(package.name))
                {
                    SemanticVersionVO packageVersion = _repositoriesModel.GetDependencyVersion(package.name);
                    PackageFileVO     packageFile    = _repositoryOperations.LoadPackageJson(package, packageVersion);

                    GUILayout.Label("[Installed Version:" + packageVersion.ToString() + "]", EditorStyles.boldLabel);
                    GUILayout.Space(16);
                    GUILayout.Label(packageFile.displayName, EditorStyles.boldLabel);
                    GUILayout.Space(16);
                    GUILayout.Label("Description :", EditorStyles.label);
                    GUILayout.Label(packageFile.description, EditorStyles.wordWrappedLabel);
                    GUILayout.Space(16);

                    if (packageFile.dependencies != null && packageFile.dependencies.Count > 0)
                    {
                        GUILayout.Label("Dependencies :", EditorStyles.label);
                        foreach (string dependencyName in packageFile.dependencies.Keys)
                        {
                            string dependencyVersion = _repositoryOperations.ParsePackageJsonDependencyVersion(packageFile.dependencies[dependencyName]);
                            GUILayout.Label(" - "+ dependencyName + "@" + dependencyVersion, EditorStyles.label);
                        }
                        GUILayout.Space(16);
                    }
                }

                GUILayout.Label("All versions :", EditorStyles.label);
                for (int i = 0; i < package.VersionCount; i++)
                {
                    SemanticVersionVO packageVersion = package.GetVersion(i);
                    GUILayout.Label(" - "+ packageVersion.ToString(), EditorStyles.label);
                }
            }
            GUILayout.EndArea();
        }

        private void ReloadRepositories()
        {
//            ReloadRepositoriesProgress(0);

            _repositoriesModel.Clear();
            _repositoryOperations.LoadRepositories(ReloadRepositoriesProgress);

//            EditorUtility.ClearProgressBar();
        }

        private void ReloadRepositoriesProgress(float progress)
        {
//            EditorUtility.DisplayProgressBar("Reloading repositories", "Just a sec...", progress);
        }

    }

}
