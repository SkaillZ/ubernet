using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Ubernet.Installer
{
    public class UbernetInstallerWindow : EditorWindow
    {
        private const string CorePackageName = "Ubernet.Core.unitypackage";
        private const string PhotonProviderPackageName = "Ubernet.Providers.Photon.unitypackage";

        private const string LiteNetLibExperimentalProviderPackageName =
            "Ubernet.Providers.LiteNetLibExperimental.unitypackage";

        private const string ExtrasPackageName = "Ubernet.Extras.unitypackage";
        private const string IncrementalCompilerPackageName = "com.unity.incrementalcompiler";

        private UbernetPackageInfo[] _packages;

        private bool _isIncrementalCompilerInstalled;
        private AddRequest _incrementalCompilerAddRequest;
        
        private bool _interactiveMode;
        private bool _installing;

        [MenuItem("Window/Ubernet/Installer")]
        public static void ShowWindow()
        {
            GetWindow<UbernetInstallerWindow>(true, "Ubernet Installer", true);
        }

        private void OnEnable()
        {
            minSize = new Vector2(350, 400);
            _isIncrementalCompilerInstalled = CheckIncrementalCompiler();
            _packages = GetPackages();
        }

        private void OnGUI()
        {
            if (!CheckInstallationDependencies())
            {
                return;
            }

            DrawPackageList();

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Separator();

            DrawPackageInfo();

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(_installing))
            {
                if (GUILayout.Button(_installing ? "Installing..." : "Install", GUILayout.Height(25f)))
                {
                    Install();
                }
            }

            // _interactiveMode = EditorGUILayout.ToggleLeft("Interactive Installation", _interactiveMode);
        }
        
        private void DrawPackageList()
        {
            EditorGUILayout.HelpBox("Click a package name for more information.", MessageType.Info);
            EditorGUILayout.Separator();

            foreach (var package in _packages)
            {
                // Force installing the core package
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(package.required))
                    {
                        package.enabled = EditorGUILayout.Toggle(package.enabled, GUILayout.Width(20f), GUILayout.Height(16f));
                    }

                    GUI.SetNextControlName(package.name);
                    EditorGUILayout.SelectableLabel(package.displayName, GUILayout.Height(16f));
                    if (package.state == UbernetPackageInfo.InstallationState.Installing)
                    {
                        EditorGUILayout.LabelField("Installing", EditorStyles.boldLabel);
                    } 
                    else if (package.state == UbernetPackageInfo.InstallationState.Installed)
                    {
                        EditorGUILayout.LabelField("Installed", EditorStyles.boldLabel);

                    }
                }
            }
        }

        private void DrawPackageInfo()
        {
            var focusedPackage = GetPackageByName(GUI.GetNameOfFocusedControl());
            if (focusedPackage != null)
            {
                EditorGUILayout.LabelField("Package", focusedPackage.displayName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Name", focusedPackage.name);
                EditorGUILayout.LabelField("Version", focusedPackage.version);
                EditorGUILayout.LabelField("Description");
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField(focusedPackage.description, EditorStyles.wordWrappedLabel);
                }

                if (focusedPackage.dependencies != null && focusedPackage.dependencies.Length > 0)
                {
                    EditorGUILayout.LabelField("Dependencies");
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.LabelField(string.Join("\n", focusedPackage.dependencies),
                            EditorStyles.wordWrappedLabel);
                    }
                }
            }
        }

        private UbernetPackageInfo GetPackageByName(string packageName)
        {
            if (string.IsNullOrEmpty(packageName))
            {
                return null;
            }

            return _packages.SingleOrDefault(p => p.name == packageName);
        }

        private void Install()
        {
            // Check if all package dependencies are enabled
            foreach (var package in _packages.Where(p => p.dependencies != null))
            {
                foreach (string dependency in package.dependencies)
                {
                    // ReSharper disable once SimplifyLinqExpression
                    if (!_packages.Any(p => p.name == dependency && p.enabled))
                    {
                        EditorUtility.DisplayDialog("Installation failed", "'" + package.name + "' depends on '" + dependency + "'. " +
                            "Please enable the dependency and try again.", "OK");
                        return;
                    }
                }
            }

            _installing = true;
            
            // TODO: use proper asset import callbacks
            foreach (var package in _packages)
            {
                if (!package.enabled)
                {
                    // Skip package if not enabled
                    continue;
                }

                try
                {
                    package.state = UbernetPackageInfo.InstallationState.Installing;
                    Repaint();
                    AssetDatabase.ImportPackage(package.assetFullPath, _interactiveMode);
                }
                finally
                {
                    package.state = UbernetPackageInfo.InstallationState.Installed;
                    Repaint();
                }
            }
            
            _installing = false;
            Close();
        }

        private bool CheckInstallationDependencies()
        {
            // Check if we're on .NET 4.x
            if (PlayerSettings.scriptingRuntimeVersion == ScriptingRuntimeVersion.Legacy)
            {
                EditorGUILayout.HelpBox("The scripting runtime must be set to .NET 4.x or later.", MessageType.Error);
                if (GUILayout.Button("Fix"))
                {
                    PlayerSettings.scriptingRuntimeVersion = ScriptingRuntimeVersion.Latest;
                    EditorUtility.DisplayDialog("Runtime Version updated",
                        "Please restart the Unity editor before installing Ubernet.", "OK");
                    Close();
                }

                return false;
            }

            #if !UNITY_2018_3_OR_NEWER
            if (!_isIncrementalCompilerInstalled)
            {
                EditorGUILayout.HelpBox("The incremental compiler must be installed.", MessageType.Error);

                if (_incrementalCompilerAddRequest == null)
                {
                    if (GUILayout.Button("Fix"))
                    {
                        _incrementalCompilerAddRequest = Client.Add(IncrementalCompilerPackageName);
                    }

                    if (GUILayout.Button("Ignore (not recommended)"))
                    {
                        _isIncrementalCompilerInstalled = true;
                    }
                }
                else
                {
                    if (_incrementalCompilerAddRequest.IsCompleted)
                    {
                        if (_incrementalCompilerAddRequest.Error == null)
                        {
                            _isIncrementalCompilerInstalled = true;
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Error",
                                "An error occured while installing the incremental compiler: "
                                + _incrementalCompilerAddRequest.Error.message, "OK");
                        }

                        _incrementalCompilerAddRequest = null;
                    }
                    else
                    {
                        GUILayout.Label("Adding the incremental compiler to your project...");
                    }
                }

                return false;
            }
            #endif

            if (_packages.Length == 0)
            {
                GUILayout.Label("No installable packages found.");
                return false;
            }

            return true;
        }

        private static bool CheckIncrementalCompiler()
        {
            var packagesClass = Assembly.GetAssembly(typeof(Editor)).GetType("UnityEditor.PackageManager.Packages");
            var getAllMethod = packagesClass.GetMethod("GetAll", BindingFlags.Static | BindingFlags.Public);
            if (getAllMethod != null)
            {
                var packages = (PackageInfo[]) getAllMethod.Invoke(null, new object[0]);
                return packages.Any(p => p.name == IncrementalCompilerPackageName);
            }

            return true;
        }

        public static UbernetPackageInfo[] GetPackages()
        {
            return AssetDatabase.FindAssets(".packageinfo t:TextAsset")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(a => a.EndsWith(".json"))
                .Select(File.ReadAllText)
                .Select(JsonUtility.FromJson<UbernetPackageInfo>)
                .Select(p =>
                {
                    if (p.required)
                    {
                        p.enabled = true;
                    }

                    p.assetFullPath = AssetDatabase
                        .FindAssets(p.name)
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .FirstOrDefault(a => a.EndsWith(".unitypackage"));

                    if (string.IsNullOrEmpty(p.assetFullPath))
                    {
                        Debug.LogWarning("Could not locate package '" + p.name + "'.");
                    }

                    return p;
                })
                .Where(p => !string.IsNullOrEmpty(p.assetFullPath))
                .ToArray();
        }
    }
}