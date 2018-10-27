using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

public class UbernetInstaller : EditorWindow
{
    private const string CorePackageName = "Ubernet.Core.unitypackage";
    private const string IncrementalCompilerPackageName = "com.unity.incrementalcompiler";

    private string[] _packageNames;
    private bool[] _enabledPackages;

    private bool _isIncrementalCompilerInstalled;
    private AddRequest _incrementalCompilerAddRequest;
    
    [MenuItem("Window/Ubernet/Installer")]
    public static void ShowWindow()
    {
        GetWindow<UbernetInstaller>(true, "Ubernet Installer", true);
    }
    
    private void OnEnable()
    {
        _packageNames = GetPackages();
        _enabledPackages = new bool[_packageNames.Length];
        
        EnableCorePackage();

        _isIncrementalCompilerInstalled = CheckIncrementalCompiler();
    }

    private void OnGUI()
    {
        if (!CheckDependencies())
        {
            return;
        }
        
        for (var i = 0; i < _packageNames.Length; i++)
        {
            string packageName = _packageNames[i];
            string displayName = GetDisplayName(packageName);
            
            // Force installing the core package
            using (new EditorGUI.DisabledScope(packageName.EndsWith(CorePackageName)))
            {
                _enabledPackages[i] = EditorGUILayout.ToggleLeft(displayName, _enabledPackages[i]);
            }
        }
        
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Install", GUILayout.Height(25f)))
        {
            Install();
        }
    }
    
    private void Install()
    {
        Close();
        for (int i = 0; i < _packageNames.Length; i++)
        {
            if (!_enabledPackages[i])
            {
                // Skip package if not enabled
                continue;
            }
            
            string packageName = _packageNames[i];
            Debug.Log(string.Format("Installing {0}", packageName));
            AssetDatabase.ImportPackage(packageName, false);
            Debug.Log(string.Format("{0} installed.", packageName));
        }
    }

    private bool CheckDependencies()
    {
        // Check if we're on .NET 4.x
        if (PlayerSettings.scriptingRuntimeVersion == ScriptingRuntimeVersion.Legacy)
        {
            EditorGUILayout.HelpBox("The scripting runtime must be set to .NET 4.x or later.", MessageType.Error);
            if (GUILayout.Button("Fix"))
            {
                PlayerSettings.scriptingRuntimeVersion = ScriptingRuntimeVersion.Latest;
                EditorUtility.DisplayDialog("Runtime Version updated", "Please restart the Unity editor before installing Ubernet.", "OK");
                Close();
            }
            
            return false;
        }
        
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
                        EditorUtility.DisplayDialog("Error", "An error occured while installing the incremental compiler: "
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
        
        if (_packageNames.Length == 0)
        {
            GUILayout.Label("No installable packages found.");
            return false;
        }

        return true;
    }

    private void EnableCorePackage()
    {
        for (var i = 0; i < _packageNames.Length; i++)
        {
            if (_packageNames[i].EndsWith(CorePackageName))
            {
                _enabledPackages[i] = true;
                return;
            }
        }
    }

    private string GetDisplayName(string packageName)
    {
        // Only show the file name and remove the ending
        var split = packageName.Split('/');
        if (split.Length == 0)
        {
            return packageName;
        }
        
        string fileName = split[split.Length - 1];
        
        var splitFileName = fileName.Split('.');
        if (splitFileName.Length == 0)
        {
            return fileName;
        }
        
        // Remove the file ending
        splitFileName = splitFileName.Take(splitFileName.Length - 1).ToArray();
        return string.Join(".", splitFileName);
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

    public static string[] GetPackages()
    {
        return AssetDatabase.FindAssets("Ubernet t:DefaultAsset")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(a => a.EndsWith(".unitypackage"))
            .ToArray();
    }
}