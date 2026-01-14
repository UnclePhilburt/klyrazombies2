using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class FixBuildSettings
{
    [MenuItem("Project Klyra/World/Fix Build Settings Scenes")]
    public static void Fix()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();

        // Add MainMenu first if it exists
        if (File.Exists("Assets/Scenes/MainMenu.unity"))
        {
            scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true));
        }

        // Add Persistent scene
        if (File.Exists("Assets/Scenes/Chunks/Persistent.unity"))
        {
            scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Chunks/Persistent.unity", true));
        }

        // Add all chunk scenes that actually exist
        string chunksFolder = "Assets/Scenes/Chunks";
        if (Directory.Exists(chunksFolder))
        {
            string[] chunkFiles = Directory.GetFiles(chunksFolder, "Chunk_*.unity");
            System.Array.Sort(chunkFiles); // Sort alphabetically

            foreach (var file in chunkFiles)
            {
                string path = file.Replace("\\", "/");
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }
        }

        // Apply to build settings
        EditorBuildSettings.scenes = scenes.ToArray();

        Debug.Log($"[FixBuildSettings] Set {scenes.Count} scenes in Build Settings");
        EditorUtility.DisplayDialog("Done", $"Build Settings updated with {scenes.Count} scenes:\n- MainMenu (if exists)\n- Persistent\n- {scenes.Count - 2} Chunk scenes", "OK");
    }
}
