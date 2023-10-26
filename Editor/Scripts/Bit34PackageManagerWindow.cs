using UnityEngine;
using UnityEditor;
using Com.Bit34games.PackageManager.Models;
using Com.Bit34games.PackageManager.Utilities;
using Com.Bit34games.PackageManager.VOs;

namespace Com.Bit34games.Unity.Editor
{

    public class Bit34PackageManagerWindow : EditorWindow
    {
        //  CONSTANTS
        private const float  TOOLBAR_PANEL_HEIGHT       = 20;
        private const float  LIST_PANEL_WIDTH           = 220;


        //  MEMBERS
        private RepositoriesModel    _repositoriesModel;
        private RepositoryOperations _repositoryOperations;
        //      Tool bar
        private Rect                _toolBarRect;
        //      Package list
        private Rect                _packageListRect;
        private int                 _packageListSelection;
        private GUIStyle            _packageListItemStyle;
        private Texture2D           _notloadedIcon;
        private Texture2D           _loadedIcon;
        private Texture2D           _outdatedIcon;
        private Vector2             _packageListScrollPosition;
        //      Package detail
        private Rect                _packageDetailRect;


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

            _packageListItemStyle = new GUIStyle();
            _packageListItemStyle.normal.textColor = Color.black;
            _packageListSelection = -1;
            _notloadedIcon = EditorGUIUtility.FindTexture("d_tranp") ;
            _loadedIcon    = EditorGUIUtility.FindTexture("console.infoicon") ;
            _outdatedIcon  = EditorGUIUtility.FindTexture("console.warnicon") ;
        }

        private void OnGUI()
        {
            _toolBarRect       = new Rect(0,                0,                    position.width,                  TOOLBAR_PANEL_HEIGHT);
            _packageListRect   = new Rect(0,                TOOLBAR_PANEL_HEIGHT, LIST_PANEL_WIDTH,                position.height-TOOLBAR_PANEL_HEIGHT);
            _packageDetailRect = new Rect(LIST_PANEL_WIDTH, TOOLBAR_PANEL_HEIGHT, position.width-LIST_PANEL_WIDTH, position.height-TOOLBAR_PANEL_HEIGHT);

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
            if (GUILayout.Button("Reload", GUILayout.Width(LIST_PANEL_WIDTH)))
            {
                ReloadRepositories();
                _repositoryOperations.LoadDependencies();
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawPackageList()
        {
            GUILayout.BeginArea(_packageListRect, GUI.skin.textArea);

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
                GUILayout.Box(_loadedIcon, GUILayout.Width(16), GUILayout.Height(16));
            }
            else
            {
                GUILayout.Box(_notloadedIcon, GUILayout.Width(16), GUILayout.Height(16));
            }
            
            return GUILayout.Button(new GUIContent(package.name));
        }

        private void DrawPackageDetail()
        {
            GUILayout.BeginArea(_packageDetailRect);

            if (_packageListSelection != -1)
            {
                RepositoryPackageVO package = _repositoriesModel.GetPackage(_packageListSelection);

                if (_repositoriesModel.HasDependency(package.name))
                {
                    GUILayout.Label(package.name + "[Installed]", EditorStyles.boldLabel);
                    GUILayout.Label("Version : " + _repositoriesModel.GetDependencyVersion(package.name), EditorStyles.label);
                }
                else
                {
                    GUILayout.Label(package.name, EditorStyles.boldLabel);
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
