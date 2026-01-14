using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

#if UNITY_ADDRESSABLES
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
#endif

/// <summary>
/// One-click setup tool for Addressables chunk streaming.
/// Automatically marks chunk scenes as addressable and configures settings.
/// Menu: Project Klyra > World > Setup Addressables
/// </summary>
public class AddressablesSetup : EditorWindow
{
    private ChunkConfig m_Config;
    private string m_ChunksFolder = "Assets/Scenes/Chunks";
    private bool m_HasAddressables = false;
    private int m_ChunkScenesFound = 0;
    private int m_ChunkScenesMarked = 0;

    [MenuItem("Project Klyra/World/Setup Addressables")]
    public static void ShowWindow()
    {
        GetWindow<AddressablesSetup>("Addressables Setup");
    }

    private void OnEnable()
    {
        CheckAddressablesInstalled();
    }

    private void OnGUI()
    {
        GUILayout.Label("Addressables Setup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Sets up Addressables for chunk streaming. This reduces initial download size for WebGL.", MessageType.Info);
        EditorGUILayout.Space();

        // Check if Addressables is installed
        if (!m_HasAddressables)
        {
            EditorGUILayout.HelpBox("Addressables package is not installed!", MessageType.Warning);

            if (GUILayout.Button("Install Addressables Package", GUILayout.Height(30)))
            {
                InstallAddressablesPackage();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "After installing, Unity will recompile scripts. Then reopen this window to continue setup.",
                MessageType.Info);

            return;
        }

        // Config
        m_Config = (ChunkConfig)EditorGUILayout.ObjectField("Chunk Config", m_Config, typeof(ChunkConfig), false);

        if (m_Config == null)
        {
            EditorGUILayout.HelpBox("Please assign your ChunkConfig asset.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();

        // Chunks folder
        EditorGUILayout.BeginHorizontal();
        m_ChunksFolder = EditorGUILayout.TextField("Chunks Folder", m_ChunksFolder);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Chunks Folder", "Assets/Scenes", "");
            if (!string.IsNullOrEmpty(path))
            {
                m_ChunksFolder = "Assets" + path.Substring(Application.dataPath.Length);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Scan button
        if (GUILayout.Button("Scan for Chunk Scenes"))
        {
            ScanChunkScenes();
        }

        if (m_ChunkScenesFound > 0)
        {
            EditorGUILayout.HelpBox($"Found {m_ChunkScenesFound} chunk scenes in {m_ChunksFolder}", MessageType.Info);
        }

        EditorGUILayout.Space();

        // Setup button
        GUI.enabled = m_Config != null && m_ChunkScenesFound > 0;
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("SETUP ADDRESSABLES FOR CHUNKS", GUILayout.Height(40)))
        {
            SetupAddressables();
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        if (m_ChunkScenesMarked > 0)
        {
            EditorGUILayout.Space();
            GUI.color = Color.green;
            EditorGUILayout.LabelField($"✓ {m_ChunkScenesMarked} chunk scenes marked as Addressable!", EditorStyles.boldLabel);
            GUI.color = Color.white;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Next steps:\n" +
                "1. Build Addressables: Window > Asset Management > Addressables > Groups > Build > New Build > Default Build Script\n" +
                "2. Build your WebGL game normally\n" +
                "3. Upload both the WebGL build AND the ServerData folder to your hosting",
                MessageType.Info);
        }
    }

    private void CheckAddressablesInstalled()
    {
        #if UNITY_ADDRESSABLES
        m_HasAddressables = true;
        #else
        m_HasAddressables = false;
        #endif
    }

    private void InstallAddressablesPackage()
    {
        // Open Package Manager to Addressables
        UnityEditor.PackageManager.UI.Window.Open("com.unity.addressables");

        EditorUtility.DisplayDialog("Install Addressables",
            "The Package Manager will open.\n\n" +
            "1. Find 'Addressables' in the Unity Registry\n" +
            "2. Click 'Install'\n" +
            "3. Wait for scripts to recompile\n" +
            "4. Reopen this window to continue",
            "OK");
    }

    private void ScanChunkScenes()
    {
        if (!Directory.Exists(m_ChunksFolder))
        {
            EditorUtility.DisplayDialog("Error", $"Chunks folder not found: {m_ChunksFolder}", "OK");
            m_ChunkScenesFound = 0;
            return;
        }

        string[] chunkFiles = Directory.GetFiles(m_ChunksFolder, "Chunk_*.unity");
        m_ChunkScenesFound = chunkFiles.Length;

        Debug.Log($"[AddressablesSetup] Found {m_ChunkScenesFound} chunk scenes");
    }

    private void SetupAddressables()
    {
        #if UNITY_ADDRESSABLES
        Debug.Log("[AddressablesSetup] Setting up Addressables...");

        // Get or create Addressables settings
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
        }

        if (settings == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not create Addressables settings!", "OK");
            return;
        }

        // Get or create "Chunks" group
        AddressableAssetGroup chunksGroup = settings.FindGroup("Chunks");
        if (chunksGroup == null)
        {
            chunksGroup = settings.CreateGroup("Chunks", false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            Debug.Log("[AddressablesSetup] Created 'Chunks' group");
        }

        // Configure the group for optimal streaming
        var schema = chunksGroup.GetSchema<BundledAssetGroupSchema>();
        if (schema != null)
        {
            schema.BuildPath.SetVariableByName(settings, "LocalBuildPath");
            schema.LoadPath.SetVariableByName(settings, "LocalLoadPath");
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            schema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4; // Fast decompression for WebGL
        }

        // Find all chunk scenes
        string[] chunkFiles = Directory.GetFiles(m_ChunksFolder, "Chunk_*.unity");
        m_ChunkScenesMarked = 0;

        foreach (string chunkFile in chunkFiles)
        {
            string assetPath = chunkFile.Replace("\\", "/");
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning($"[AddressablesSetup] Could not find GUID for: {assetPath}");
                continue;
            }

            // Check if already addressable
            AddressableAssetEntry entry = settings.FindAssetEntry(guid);

            if (entry == null)
            {
                // Create new entry
                string sceneName = Path.GetFileNameWithoutExtension(chunkFile);
                entry = settings.CreateOrMoveEntry(guid, chunksGroup, false, false);
                entry.address = sceneName; // Use scene name as address (e.g., "Chunk_5_3")
                m_ChunkScenesMarked++;
                Debug.Log($"[AddressablesSetup] Marked as addressable: {sceneName}");
            }
            else
            {
                Debug.Log($"[AddressablesSetup] Already addressable: {entry.address}");
                m_ChunkScenesMarked++;
            }
        }

        // Mark settings as dirty
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        Debug.Log($"[AddressablesSetup] Setup complete! {m_ChunkScenesMarked} chunk scenes marked as addressable.");

        EditorUtility.DisplayDialog("Addressables Setup Complete",
            $"✓ {m_ChunkScenesMarked} chunk scenes marked as Addressable\n\n" +
            "Next: Build Addressables via:\n" +
            "Window > Asset Management > Addressables > Groups\n" +
            "Then click: Build > New Build > Default Build Script",
            "OK");

        // Open Addressables Groups window
        EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");
        #else
        EditorUtility.DisplayDialog("Error", "Addressables package not installed!", "OK");
        #endif
    }
}
