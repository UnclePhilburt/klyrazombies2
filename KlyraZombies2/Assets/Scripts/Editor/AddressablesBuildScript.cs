using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build;

/// <summary>
/// Automated build script for Addressables + WebGL.
/// Menu: Project Klyra > Build > Build Addressables + WebGL
/// </summary>
public class AddressablesBuildScript
{
    [MenuItem("Project Klyra/Build/Build Addressables Only")]
    public static void BuildAddressablesOnly()
    {
        Debug.Log("[Build] Starting Addressables build...");

        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[Build] Addressables settings not found! Run Setup Addressables first.");
            EditorUtility.DisplayDialog("Error", "Addressables not set up! Run Project Klyra > World > Setup Addressables first.", "OK");
            return;
        }

        // Build Addressables
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);

        if (!string.IsNullOrEmpty(result.Error))
        {
            Debug.LogError($"[Build] Addressables build failed: {result.Error}");
            EditorUtility.DisplayDialog("Build Failed", $"Addressables build failed:\n\n{result.Error}", "OK");
            return;
        }

        Debug.Log($"[Build] Addressables build complete! Duration: {result.Duration:F2}s");
        EditorUtility.DisplayDialog("Build Complete",
            $"Addressables build successful!\n\nDuration: {result.Duration:F2}s\n\nAssets are ready in ServerData folder.",
            "OK");
    }

    [MenuItem("Project Klyra/Build/Build WebGL (with Addressables)")]
    public static void BuildWebGLWithAddressables()
    {
        Debug.Log("[Build] Starting full build: Addressables + WebGL...");

        // Step 1: Build Addressables
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[Build] Addressables settings not found!");
            EditorUtility.DisplayDialog("Error", "Addressables not set up! Run Project Klyra > World > Setup Addressables first.", "OK");
            return;
        }

        EditorUtility.DisplayProgressBar("Building", "Building Addressables...", 0.3f);

        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult addressableResult);

        if (!string.IsNullOrEmpty(addressableResult.Error))
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[Build] Addressables build failed: {addressableResult.Error}");
            EditorUtility.DisplayDialog("Build Failed", $"Addressables build failed:\n\n{addressableResult.Error}", "OK");
            return;
        }

        Debug.Log($"[Build] Addressables built successfully! Duration: {addressableResult.Duration:F2}s");

        // Step 2: Build WebGL
        EditorUtility.DisplayProgressBar("Building", "Building WebGL...", 0.6f);

        string buildPath = EditorUtility.SaveFolderPanel("Choose WebGL Build Location", "", "Build");
        if (string.IsNullOrEmpty(buildPath))
        {
            EditorUtility.ClearProgressBar();
            Debug.Log("[Build] Build cancelled by user.");
            return;
        }

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = buildPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(buildOptions);

        EditorUtility.ClearProgressBar();

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[Build] WebGL build succeeded! Size: {FormatBytes(report.summary.totalSize)}");

            EditorUtility.DisplayDialog("Build Complete",
                $"✓ Addressables built ({addressableResult.Duration:F2}s)\n" +
                $"✓ WebGL built ({report.summary.totalTime.TotalSeconds:F1}s)\n" +
                $"\nSize: {FormatBytes(report.summary.totalSize)}\n\n" +
                "Upload BOTH:\n" +
                "1. WebGL build folder\n" +
                "2. ServerData folder (Addressables assets)\n\n" +
                "to your hosting (Netlify/itch.io)",
                "OK");

            // Open build folder
            EditorUtility.RevealInFinder(buildPath);
        }
        else
        {
            Debug.LogError($"[Build] WebGL build failed: {report.summary.result}");
            EditorUtility.DisplayDialog("Build Failed",
                $"WebGL build failed with {report.summary.totalErrors} errors.\n\nCheck console for details.",
                "OK");
        }
    }

    [MenuItem("Project Klyra/Build/Build WebGL (No Addressables)")]
    public static void BuildWebGLOnly()
    {
        Debug.Log("[Build] Starting WebGL build (without Addressables)...");

        string buildPath = EditorUtility.SaveFolderPanel("Choose WebGL Build Location", "", "Build");
        if (string.IsNullOrEmpty(buildPath))
        {
            Debug.Log("[Build] Build cancelled by user.");
            return;
        }

        EditorUtility.DisplayProgressBar("Building", "Building WebGL...", 0.5f);

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = buildPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(buildOptions);

        EditorUtility.ClearProgressBar();

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[Build] Build succeeded! Size: {FormatBytes(report.summary.totalSize)}, Time: {report.summary.totalTime}");

            EditorUtility.DisplayDialog("Build Complete",
                $"WebGL build successful!\n\n" +
                $"Size: {FormatBytes(report.summary.totalSize)}\n" +
                $"Time: {report.summary.totalTime.TotalSeconds:F1}s\n\n" +
                "Build folder opened.",
                "OK");

            // Open build folder
            EditorUtility.RevealInFinder(buildPath);
        }
        else
        {
            Debug.LogError($"[Build] Build failed: {report.summary.result}");
            EditorUtility.DisplayDialog("Build Failed",
                $"Build failed with {report.summary.totalErrors} errors.\n\nCheck console for details.",
                "OK");
        }
    }

    private static string[] GetEnabledScenes()
    {
        var scenes = new System.Collections.Generic.List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
                scenes.Add(scene.path);
        }
        return scenes.ToArray();
    }

    private static string FormatBytes(ulong bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:F2} {sizes[order]}";
    }
}
