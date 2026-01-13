using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Editor tool to split a scene into chunks for streaming.
/// Menu: Project Klyra > World > Chunk Splitter
/// </summary>
public class ChunkSplitter : EditorWindow
{
    private ChunkConfig m_Config;
    private string m_SourceScenePath = "";
    private string m_OutputFolder = "Assets/Scenes/Chunks";
    private bool m_CreatePersistentScene = true;
    private Vector3 m_WorldMin = Vector3.zero;
    private Vector3 m_WorldMax = new Vector3(900, 100, 900);
    private bool m_AnalyzedScene = false;
    private int m_TotalRootObjects = 0;
    private int m_TotalAllObjects = 0;
    private Dictionary<Vector2Int, int> m_ChunkObjectCounts = new Dictionary<Vector2Int, int>();

    [MenuItem("Project Klyra/World/Chunk Splitter")]
    public static void ShowWindow()
    {
        GetWindow<ChunkSplitter>("Chunk Splitter");
    }

    [MenuItem("Project Klyra/World/Open All Chunks")]
    public static void OpenAllChunks()
    {
        string chunksFolder = "Assets/Scenes/Chunks";
        if (!System.IO.Directory.Exists(chunksFolder))
        {
            EditorUtility.DisplayDialog("Error", "Chunks folder not found!", "OK");
            return;
        }

        // Open Persistent first
        string persistentPath = chunksFolder + "/Persistent.unity";
        if (System.IO.File.Exists(persistentPath))
        {
            EditorSceneManager.OpenScene(persistentPath, OpenSceneMode.Single);
        }

        // Open all chunk scenes additively
        string[] files = System.IO.Directory.GetFiles(chunksFolder, "Chunk_*.unity");
        foreach (var file in files)
        {
            EditorSceneManager.OpenScene(file, OpenSceneMode.Additive);
        }

        Debug.Log($"[ChunkSplitter] Opened {files.Length} chunk scenes");
    }

    [MenuItem("Project Klyra/World/Close All Chunks")]
    public static void CloseAllChunks()
    {
        // Close all scenes except the first one
        for (int i = UnityEngine.SceneManagement.SceneManager.sceneCount - 1; i > 0; i--)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("World Chunk Splitter", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Config
        m_Config = (ChunkConfig)EditorGUILayout.ObjectField("Chunk Config", m_Config, typeof(ChunkConfig), false);

        if (m_Config == null)
        {
            EditorGUILayout.HelpBox("Create a ChunkConfig asset first:\nRight-click in Project > Create > Game > Chunk Config", MessageType.Warning);

            if (GUILayout.Button("Create Default ChunkConfig"))
            {
                CreateDefaultConfig();
            }
            return;
        }

        EditorGUILayout.Space();

        // Source scene
        EditorGUILayout.LabelField("Source Scene", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        m_SourceScenePath = EditorGUILayout.TextField("Scene Path", m_SourceScenePath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFilePanel("Select Source Scene", "Assets/Scenes", "unity");
            if (!string.IsNullOrEmpty(path))
            {
                m_SourceScenePath = "Assets" + path.Substring(Application.dataPath.Length);
            }
        }
        if (GUILayout.Button("Current", GUILayout.Width(60)))
        {
            if (SceneManager.GetActiveScene().path != "")
            {
                m_SourceScenePath = SceneManager.GetActiveScene().path;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Output folder
        m_OutputFolder = EditorGUILayout.TextField("Output Folder", m_OutputFolder);
        m_CreatePersistentScene = EditorGUILayout.Toggle("Create Persistent Scene", m_CreatePersistentScene);

        EditorGUILayout.Space();

        // Split button - AT TOP for accessibility
        GUI.enabled = !string.IsNullOrEmpty(m_SourceScenePath) && m_Config != null;
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("SPLIT SCENE INTO CHUNKS", GUILayout.Height(35)))
        {
            if (EditorUtility.DisplayDialog("Split Scene",
                $"This will split '{Path.GetFileNameWithoutExtension(m_SourceScenePath)}' into {m_Config.gridSizeX * m_Config.gridSizeZ} chunk scenes.\n\nThis cannot be undone. Continue?",
                "Split", "Cancel"))
            {
                SplitScene();
            }
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        EditorGUILayout.Space();

        // Config display - EDITABLE
        EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        int newGridX = EditorGUILayout.IntField("Grid Size X", m_Config.gridSizeX);
        int newGridZ = EditorGUILayout.IntField("Grid Size Z", m_Config.gridSizeZ);
        float newChunkSize = EditorGUILayout.FloatField("Chunk Size (m)", m_Config.chunkSize);
        if (EditorGUI.EndChangeCheck())
        {
            m_Config.gridSizeX = newGridX;
            m_Config.gridSizeZ = newGridZ;
            m_Config.chunkSize = newChunkSize;
            EditorUtility.SetDirty(m_Config);
        }

        EditorGUILayout.LabelField($"Total World: {m_Config.gridSizeX * m_Config.chunkSize}m x {m_Config.gridSizeZ * m_Config.chunkSize}m");

        EditorGUILayout.BeginHorizontal();
        m_Config.worldOrigin = EditorGUILayout.Vector3Field("World Origin", m_Config.worldOrigin);
        if (m_AnalyzedScene && GUILayout.Button("Auto-Fit", GUILayout.Width(70)))
        {
            // Set origin to world min, snapped to chunk size
            float chunkSize = m_Config.chunkSize;
            m_Config.worldOrigin = new Vector3(
                Mathf.Floor(m_WorldMin.x / chunkSize) * chunkSize,
                0,
                Mathf.Floor(m_WorldMin.z / chunkSize) * chunkSize
            );
            EditorUtility.SetDirty(m_Config);
            // Re-analyze with new origin
            AnalyzeScene();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Analysis
        EditorGUILayout.LabelField("Scene Analysis", EditorStyles.boldLabel);

        if (GUILayout.Button("Analyze Scene"))
        {
            AnalyzeScene();
        }

        if (m_AnalyzedScene)
        {
            EditorGUILayout.LabelField($"Root Objects: {m_TotalRootObjects} ({m_TotalAllObjects} total including children)");
            EditorGUILayout.LabelField($"World Bounds: ({m_WorldMin.x:F0}, {m_WorldMin.z:F0}) to ({m_WorldMax.x:F0}, {m_WorldMax.z:F0})");

            // Show suggested origin if objects are in negative space
            if (m_WorldMin.x < 0 || m_WorldMin.z < 0)
            {
                EditorGUILayout.HelpBox($"Objects in negative space detected! Click 'Auto-Fit' to adjust World Origin.", MessageType.Warning);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Objects per Chunk:", EditorStyles.boldLabel);

            for (int z = m_Config.gridSizeZ - 1; z >= 0; z--)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < m_Config.gridSizeX; x++)
                {
                    var coord = new Vector2Int(x, z);
                    int count = m_ChunkObjectCounts.ContainsKey(coord) ? m_ChunkObjectCounts[coord] : 0;

                    Color bgColor = count > 5000 ? Color.red : (count > 2000 ? Color.yellow : Color.green);
                    GUI.backgroundColor = bgColor;
                    GUILayout.Box($"({x},{z})\n{count}", GUILayout.Width(60), GUILayout.Height(40));
                    GUI.backgroundColor = Color.white;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

    }

    private void CreateDefaultConfig()
    {
        string path = "Assets/Resources";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        ChunkConfig config = ScriptableObject.CreateInstance<ChunkConfig>();
        config.gridSizeX = 3;
        config.gridSizeZ = 3;
        config.chunkSize = 300f;
        config.loadRadius = 1;

        AssetDatabase.CreateAsset(config, path + "/ChunkConfig.asset");
        AssetDatabase.SaveAssets();

        m_Config = config;
        Selection.activeObject = config;

        Debug.Log("[ChunkSplitter] Created ChunkConfig at " + path + "/ChunkConfig.asset");
    }

    private void AnalyzeScene()
    {
        if (string.IsNullOrEmpty(m_SourceScenePath))
        {
            EditorUtility.DisplayDialog("Error", "Please select a source scene first.", "OK");
            return;
        }

        // Open scene if not already open
        Scene scene = EditorSceneManager.OpenScene(m_SourceScenePath, OpenSceneMode.Additive);

        m_ChunkObjectCounts.Clear();
        m_TotalRootObjects = 0;
        m_TotalAllObjects = 0;
        m_WorldMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        m_WorldMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        GameObject[] rootObjects = scene.GetRootGameObjects();

        foreach (var obj in rootObjects)
        {
            // Skip certain objects
            if (ShouldStayInPersistent(obj))
                continue;

            m_TotalRootObjects++;

            // Count all children recursively and find bounds
            int childCount = CountAllChildren(obj.transform);
            m_TotalAllObjects += childCount;

            // Get bounds from all renderers in hierarchy
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                m_WorldMin = Vector3.Min(m_WorldMin, renderer.bounds.min);
                m_WorldMax = Vector3.Max(m_WorldMax, renderer.bounds.max);
            }

            // Also check transform position for objects without renderers
            Transform[] transforms = obj.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                m_WorldMin = Vector3.Min(m_WorldMin, t.position);
                m_WorldMax = Vector3.Max(m_WorldMax, t.position);
            }

            // Assign to chunk based on root position (keeps hierarchies together)
            Vector2Int chunk = m_Config.WorldToChunk(obj.transform.position);
            if (!m_ChunkObjectCounts.ContainsKey(chunk))
                m_ChunkObjectCounts[chunk] = 0;
            m_ChunkObjectCounts[chunk] += childCount;
        }

        m_AnalyzedScene = true;

        // Close the scene if we opened it
        if (SceneManager.GetActiveScene() != scene)
        {
            EditorSceneManager.CloseScene(scene, true);
        }

        Debug.Log($"[ChunkSplitter] Analyzed {m_TotalRootObjects} root objects ({m_TotalAllObjects} total). World bounds: {m_WorldMin} to {m_WorldMax}");
    }

    private int CountAllChildren(Transform parent)
    {
        int count = 1; // Count self
        foreach (Transform child in parent)
        {
            count += CountAllChildren(child);
        }
        return count;
    }

    private void SplitScene()
    {
        // Create output folder
        if (!Directory.Exists(m_OutputFolder))
        {
            Directory.CreateDirectory(m_OutputFolder);
        }

        // Open source scene
        Scene sourceScene = EditorSceneManager.OpenScene(m_SourceScenePath, OpenSceneMode.Single);
        GameObject[] allObjects = sourceScene.GetRootGameObjects();

        // Categorize objects by chunk
        Dictionary<Vector2Int, List<GameObject>> objectsByChunk = new Dictionary<Vector2Int, List<GameObject>>();
        List<GameObject> persistentObjects = new List<GameObject>();

        foreach (var obj in allObjects)
        {
            if (ShouldStayInPersistent(obj))
            {
                persistentObjects.Add(obj);
                continue;
            }

            // Check if this is a container object (empty parent with children)
            if (IsContainerObject(obj))
            {
                // Split children individually based on their positions
                SplitContainerChildren(obj, objectsByChunk);
            }
            else
            {
                // Assign whole object to chunk based on its position
                Vector2Int chunk = m_Config.WorldToChunk(obj.transform.position);

                // Clamp to valid range
                chunk.x = Mathf.Clamp(chunk.x, 0, m_Config.gridSizeX - 1);
                chunk.y = Mathf.Clamp(chunk.y, 0, m_Config.gridSizeZ - 1);

                if (!objectsByChunk.ContainsKey(chunk))
                    objectsByChunk[chunk] = new List<GameObject>();

                objectsByChunk[chunk].Add(obj);
            }
        }

        // Create persistent scene
        if (m_CreatePersistentScene)
        {
            Scene persistentScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            foreach (var obj in persistentObjects)
            {
                SceneManager.MoveGameObjectToScene(obj, persistentScene);
            }

            string persistentPath = Path.Combine(m_OutputFolder, m_Config.persistentSceneName + ".unity");
            EditorSceneManager.SaveScene(persistentScene, persistentPath);
            Debug.Log($"[ChunkSplitter] Created persistent scene with {persistentObjects.Count} objects: {persistentPath}");
        }

        // Create chunk scenes
        List<string> createdScenes = new List<string>();

        for (int x = 0; x < m_Config.gridSizeX; x++)
        {
            for (int z = 0; z < m_Config.gridSizeZ; z++)
            {
                Vector2Int coord = new Vector2Int(x, z);
                string sceneName = m_Config.GetChunkSceneName(x, z);
                string scenePath = Path.Combine(m_OutputFolder, sceneName + ".unity");

                Scene chunkScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

                if (objectsByChunk.ContainsKey(coord))
                {
                    foreach (var obj in objectsByChunk[coord])
                    {
                        SceneManager.MoveGameObjectToScene(obj, chunkScene);
                    }
                }

                EditorSceneManager.SaveScene(chunkScene, scenePath);
                createdScenes.Add(scenePath);

                int objCount = objectsByChunk.ContainsKey(coord) ? objectsByChunk[coord].Count : 0;
                Debug.Log($"[ChunkSplitter] Created {sceneName} with {objCount} objects");
            }
        }

        // Add scenes to build settings
        AddScenesToBuildSettings(createdScenes);

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success",
            $"Created {m_Config.gridSizeX * m_Config.gridSizeZ} chunk scenes in:\n{m_OutputFolder}\n\nScenes have been added to Build Settings.",
            "OK");
    }

    private bool ShouldStayInPersistent(GameObject obj)
    {
        string name = obj.name.ToLower();

        // Keep these in persistent scene - use exact/specific matches
        if (name.Contains("player")) return true;
        if (name.Contains("camera")) return true;
        if (name.Contains("light") && obj.GetComponent<Light>()?.type == LightType.Directional) return true;
        if (name.Contains("sun")) return true;
        if (name.Contains("canvas")) return true;
        if (name.Contains("manager")) return true;
        if (name.Contains("eventsystem")) return true;
        if (name.Contains("cozy")) return true; // Weather system
        if (name.Contains("chunkloader")) return true;
        if (name.Contains("inventorysystemmanager")) return true;
        if (name.Contains("simplelootui")) return true;
        if (name.Contains("simpleinventoryui")) return true;
        if (name.Contains("achievementmanager")) return true;
        if (name.Contains("achievementpopup")) return true;
        if (name.Contains("poidiscoverypopup")) return true;
        if (name.Contains("characterinfopopup")) return true;
        if (name.Contains("crosshair")) return true;
        if (name.Contains("playerspawn")) return true; // Player spawn points only
        if (name.Contains("spawnpoint")) return true;

        // Check for specific components
        if (obj.GetComponent<Camera>() != null) return true;
        if (obj.GetComponent<Canvas>() != null) return true;
        if (obj.GetComponent<ChunkLoader>() != null) return true;
        if (obj.GetComponent<AudioListener>() != null) return true;

        // UI components
        if (obj.GetComponent<SimpleLootUI>() != null) return true;
        if (obj.GetComponent<SimpleInventoryUI>() != null) return true;

        // Opsive components
        var cameraController = obj.GetComponent("CameraController");
        if (cameraController != null) return true;

        // UIS InventorySystemManager (may have empty name)
        var inventorySystemManager = obj.GetComponent("InventorySystemManager");
        if (inventorySystemManager != null) return true;

        // Check children for InventorySystemManager (it might be on a child)
        var childISM = obj.GetComponentInChildren(System.Type.GetType("Opsive.UltimateInventorySystem.Core.InventorySystemManager, Opsive.UltimateInventorySystem"));
        if (childISM != null) return true;

        return false;
    }

    /// <summary>
    /// Check if an object is a container (empty parent used for organization).
    /// Container objects have children but no meaningful components.
    /// </summary>
    private bool IsContainerObject(GameObject obj)
    {
        // Must have children to be a container
        if (obj.transform.childCount == 0)
            return false;

        // Check if it has any meaningful components (not just Transform)
        Component[] components = obj.GetComponents<Component>();
        foreach (var comp in components)
        {
            if (comp == null) continue;

            // Transform is always present, skip it
            if (comp is Transform) continue;

            // If it has any other component, it's not just a container
            return false;
        }

        // It's an empty parent with children - treat as container
        return true;
    }

    /// <summary>
    /// Split children of a container object into chunks based on each child's position.
    /// </summary>
    private void SplitContainerChildren(GameObject container, Dictionary<Vector2Int, List<GameObject>> objectsByChunk)
    {
        // Get all direct children (we'll unparent them)
        List<Transform> children = new List<Transform>();
        foreach (Transform child in container.transform)
        {
            children.Add(child);
        }

        foreach (var child in children)
        {
            // Recursively check if this child is also a container
            if (IsContainerObject(child.gameObject))
            {
                SplitContainerChildren(child.gameObject, objectsByChunk);
            }
            else
            {
                // Unparent the child so it becomes a root object
                child.SetParent(null);

                // Assign to chunk based on child's position
                Vector2Int chunk = m_Config.WorldToChunk(child.position);

                // Clamp to valid range
                chunk.x = Mathf.Clamp(chunk.x, 0, m_Config.gridSizeX - 1);
                chunk.y = Mathf.Clamp(chunk.y, 0, m_Config.gridSizeZ - 1);

                if (!objectsByChunk.ContainsKey(chunk))
                    objectsByChunk[chunk] = new List<GameObject>();

                objectsByChunk[chunk].Add(child.gameObject);
            }
        }

        // Destroy the now-empty container
        Object.DestroyImmediate(container);
    }

    private void AddScenesToBuildSettings(List<string> scenePaths)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        // Add persistent scene first if it exists
        string persistentPath = Path.Combine(m_OutputFolder, m_Config.persistentSceneName + ".unity");
        if (File.Exists(persistentPath) && !scenes.Exists(s => s.path == persistentPath))
        {
            scenes.Insert(0, new EditorBuildSettingsScene(persistentPath, true));
        }

        foreach (var path in scenePaths)
        {
            if (!scenes.Exists(s => s.path == path))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
