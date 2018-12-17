using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PackageBuilder
{
    public const string BaseDirectory = "Assets/Plugins/Ubernet";
    public const string BuildDirectory = "Assets/Plugins/Ubernet/InstallerPackages";
    public const string InstallerDirectory = "Assets/Plugins/Ubernet/Installer";
    public const string InstallerBuildDirectory = "Build";
    public const string InstallerPackagesDirectoryName = "InstallerPackages";
    public const string ScriptsDirectoryName = "Scripts";
    public const string ExtrasDirectoryName = "Extras";
    public const string ProvidersDirectoryName = "Providers";

    public const string CoreDirectoryName = "Core";
    public const string PhotonProviderDirectoryName = "Photon";
    public const string ExperimentalLiteNetLibProviderDirectoryName = "LiteNetLibExperimental";
    
    [MenuItem("Tools/Build unitypackages")]
    public static void BuildUnityPackages()
    {
        if (!Directory.Exists(BuildDirectory))
        {
            Directory.CreateDirectory(BuildDirectory);
        }
        
        var jobs = new List<Action>();
        
        AddJobs(jobs);

        for (var i = 0; i < jobs.Count; i++)
        {
            var job = jobs[i];
            bool canceled = EditorUtility.DisplayCancelableProgressBar("Building unitypackages", $"{i}/{jobs.Count}", i/(float) jobs.Count);

            if (canceled)
            {
                Debug.LogWarning("Cancelled");
                EditorUtility.ClearProgressBar();
                return;
            }

            try
            {
                job();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.ClearProgressBar();
            }
        }
        
        EditorUtility.ClearProgressBar();

        Debug.Log("Export finished.");
    }

    [MenuItem("Tools/Build Installer")]
    public static void BuildInstallerPackage()
    {
        if (!Directory.Exists(InstallerBuildDirectory))
        {
            Directory.CreateDirectory(InstallerBuildDirectory);
        }
        
        EditorUtility.DisplayProgressBar("Building installer...", "Building installer", 0f);

        try
        {
            AssetDatabase.ExportPackage(new[] { BuildDirectory, InstallerDirectory }, $"{InstallerBuildDirectory}/Ubernet.Installer.unitypackage",
                ExportPackageOptions.Recurse);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        
        Debug.Log("Installer build finished.");
    }
    
    [MenuItem("Tools/Build Packages and Installer")]
    public static void BuildPackagesAndInstaller()
    {
        BuildUnityPackages();
        BuildInstallerPackage();
    }

    public static void AddJobs(List<Action> jobs)
    {
        string scriptsDirectory = $"{BaseDirectory}/{ScriptsDirectoryName}";
        
        // Build core package
        jobs.Add(() => AssetDatabase.ExportPackage($"{scriptsDirectory}/{CoreDirectoryName}", 
            $"{BuildDirectory}/Ubernet.Core.unitypackage",
            ExportPackageOptions.Recurse));

        string providersDirectory = $"{scriptsDirectory}/{ProvidersDirectoryName}";
        
        jobs.Add(() => AssetDatabase.ExportPackage($"{providersDirectory}/{PhotonProviderDirectoryName}",
            $"{BuildDirectory}/Ubernet.Providers.Photon.unitypackage", ExportPackageOptions.Recurse));
        
        jobs.Add(() => AssetDatabase.ExportPackage($"{providersDirectory}/{ExperimentalLiteNetLibProviderDirectoryName}",
            $"{BuildDirectory}/Ubernet.Providers.LiteNetLibExperimental.unitypackage", ExportPackageOptions.Recurse));
        
        jobs.Add(() => AssetDatabase.ExportPackage($"{BaseDirectory}/{ScriptsDirectoryName}/{ExtrasDirectoryName}",
            $"{BuildDirectory}/Ubernet.Extras.unitypackage", ExportPackageOptions.Recurse));
    }
}