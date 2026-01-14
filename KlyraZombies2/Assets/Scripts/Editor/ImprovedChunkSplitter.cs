using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Improved chunk splitter that assigns objects to chunks based on their ACTUAL rendered position, not pivot.
/// Menu: Project Klyra > World > Improved Chunk Splitter
/// </summary>
public class ImprovedChunkSplitter : EditorWindow
{
    private ChunkConfig m_Config;
    private string m_SourceScenePath = "";
    private string m_OutputFolder = "Assets/Scenes/Chunks";
    private bool m_CreatePersistentScene = true;

    // NEW: Better splitting modes
    private SplitMode m_SplitMode = SplitMode.SmartByBounds;

    private Vector3 m_WorldMin = Vector3.zero;
    private Vector3 m_WorldMax = new Vector3(900, 100, 900);
    private bool m_AnalyzedScene = false;
    private int m_TotalRootObjects = 0;
    private int m_TotalAllObjects = 0;
    private Dictionary<Vector2Int, int> m_ChunkObjectCounts = new Dictionary<Vector2Int, int>();
    private Vector2 m_ScrollPosition = Vector2.zero;

    private enum SplitMode
    {
        SmartByBounds,      // Split based on rendered bounds center (recommended)
        AggressiveSplit,    // Split all large objects into individual children
        KeepHierarchies     // Keep parent-child relationships (may cause misplacements)
    }

    [MenuItem("Project Klyra/World/Improved Chunk Splitter")]
    public static void ShowWindow()
    {
        GetWindow<ImprovedChunkSplitter>("Improved Chunk Splitter");
    }

    private void OnGUI()
    {
        GUILayout.Label("Improved World Chunk Splitter", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This improved splitter assigns objects based on their ACTUAL rendered position, not pivot point.", MessageType.Info);
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

        // Split mode
        EditorGUILayout.LabelField("Split Mode", EditorStyles.boldLabel);
        m_SplitMode = (SplitMode)EditorGUILayout.EnumPopup("Mode", m_SplitMode);

        switch (m_SplitMode)
        {
            case SplitMode.SmartByBounds:
                EditorGUILayout.HelpBox("Uses center of rendered bounds. Best for most cases. Objects spanning multiple chunks are assigned to the chunk containing most of their content.", MessageType.Info);
                break;
            case SplitMode.AggressiveSplit:
                EditorGUILayout.HelpBox("Splits ALL objects with children. Maximum accuracy but breaks some hierarchies. Use for problematic scenes.", MessageType.Warning);
                break;
            case SplitMode.KeepHierarchies:
                EditorGUILayout.HelpBox("Keeps parent-child relationships intact. May cause objects to be in wrong chunks if they span multiple chunks.", MessageType.Warning);
                break;
        }

        EditorGUILayout.Space();

        // Output folder
        m_OutputFolder = EditorGUILayout.TextField("Output Folder", m_OutputFolder);
        m_CreatePersistentScene = EditorGUILayout.Toggle("Create Persistent Scene", m_CreatePersistentScene);

        EditorGUILayout.Space();

        // Split button
        GUI.enabled = !string.IsNullOrEmpty(m_SourceScenePath) && m_Config != null;
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("SPLIT SCENE INTO CHUNKS", GUILayout.Height(35)))
        {
            if (EditorUtility.DisplayDialog("Split Scene",
                $"This will split '{Path.GetFileNameWithoutExtension(m_SourceScenePath)}' into {m_Config.gridSizeX * m_Config.gridSizeZ} chunk scenes.\n\nMode: {m_SplitMode}\n\nThis cannot be undone. Continue?",
                "Split", "Cancel"))
            {
                SplitScene();
            }
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        EditorGUILayout.Space();

        // Config display
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
            AutoFitOrigin();
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

            if (m_WorldMin.x < 0 || m_WorldMin.z < 0)
            {
                EditorGUILayout.HelpBox($"Objects in negative space detected! Click 'Auto-Fit' to adjust World Origin.", MessageType.Warning);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Objects per Chunk:", EditorStyles.boldLabel);

            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition, GUILayout.ExpandHeight(true));

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

            EditorGUILayout.EndScrollView();
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
        config.gridSizeX = 10;
        config.gridSizeZ = 10;
        config.chunkSize = 100f;
        config.loadRadius = 1;

        AssetDatabase.CreateAsset(config, path + "/ChunkConfig.asset");
        AssetDatabase.SaveAssets();

        m_Config = config;
        Selection.activeObject = config;

        Debug.Log("[ImprovedChunkSplitter] Created ChunkConfig at " + path + "/ChunkConfig.asset");
    }

    private void AutoFitOrigin()
    {
        float chunkSize = m_Config.chunkSize;
        m_Config.worldOrigin = new Vector3(
            Mathf.Floor(m_WorldMin.x / chunkSize) * chunkSize,
            0,
            Mathf.Floor(m_WorldMin.z / chunkSize) * chunkSize
        );

        float worldSizeX = m_WorldMax.x - m_Config.worldOrigin.x;
        float worldSizeZ = m_WorldMax.z - m_Config.worldOrigin.z;
        m_Config.gridSizeX = Mathf.CeilToInt(worldSizeX / chunkSize);
        m_Config.gridSizeZ = Mathf.CeilToInt(worldSizeZ / chunkSize);

        m_Config.gridSizeX = Mathf.Max(1, m_Config.gridSizeX);
        m_Config.gridSizeZ = Mathf.Max(1, m_Config.gridSizeZ);

        EditorUtility.SetDirty(m_Config);
        AnalyzeScene();

        Debug.Log($"[ImprovedChunkSplitter] Auto-Fit: Origin={m_Config.worldOrigin}, Grid={m_Config.gridSizeX}x{m_Config.gridSizeZ}");
    }

    private void AnalyzeScene()
    {
        if (string.IsNullOrEmpty(m_SourceScenePath))
        {
            EditorUtility.DisplayDialog("Error", "Please select a source scene first.", "OK");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(m_SourceScenePath, OpenSceneMode.Additive);

        m_ChunkObjectCounts.Clear();
        m_TotalRootObjects = 0;
        m_TotalAllObjects = 0;
        m_WorldMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        m_WorldMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        GameObject[] rootObjects = scene.GetRootGameObjects();

        foreach (var obj in rootObjects)
        {
            if (ShouldStayInPersistent(obj))
                continue;

            m_TotalRootObjects++;

            int childCount = CountAllChildren(obj.transform);
            m_TotalAllObjects += childCount;

            // Get bounds
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                m_WorldMin = Vector3.Min(m_WorldMin, renderer.bounds.min);
                m_WorldMax = Vector3.Max(m_WorldMax, renderer.bounds.max);
            }

            Transform[] transforms = obj.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                m_WorldMin = Vector3.Min(m_WorldMin, t.position);
                m_WorldMax = Vector3.Max(m_WorldMax, t.position);
            }

            // Analyze chunk assignment
            AnalyzeObjectForChunks(obj);
        }

        m_AnalyzedScene = true;

        if (SceneManager.GetActiveScene() != scene)
        {
            EditorSceneManager.CloseScene(scene, true);
        }

        Debug.Log($"[ImprovedChunkSplitter] Analyzed {m_TotalRootObjects} root objects ({m_TotalAllObjects} total). World bounds: {m_WorldMin} to {m_WorldMax}");
    }

    private void AnalyzeObjectForChunks(GameObject obj)
    {
        // Special handling for spawner containers - analyze children individually
        if (IsSpawnerContainer(obj))
        {
            foreach (Transform child in obj.transform)
            {
                Vector2Int spawnerChunk = m_Config.WorldToChunk(child.position);
                spawnerChunk.x = Mathf.Clamp(spawnerChunk.x, 0, m_Config.gridSizeX - 1);
                spawnerChunk.y = Mathf.Clamp(spawnerChunk.y, 0, m_Config.gridSizeZ - 1);

                if (!m_ChunkObjectCounts.ContainsKey(spawnerChunk))
                    m_ChunkObjectCounts[spawnerChunk] = 0;
                m_ChunkObjectCounts[spawnerChunk] += 1;
            }
            return;
        }

        int childCount = CountAllChildren(obj.transform);

        // Get the chunk based on rendered content
        Vector2Int chunk = GetChunkFromRenderedBounds(obj);

        // Clamp to valid range
        chunk.x = Mathf.Clamp(chunk.x, 0, m_Config.gridSizeX - 1);
        chunk.y = Mathf.Clamp(chunk.y, 0, m_Config.gridSizeZ - 1);

        if (!m_ChunkObjectCounts.ContainsKey(chunk))
            m_ChunkObjectCounts[chunk] = 0;
        m_ChunkObjectCounts[chunk] += childCount;
    }

    /// <summary>
    /// Get chunk coordinate based on the CENTER OF RENDERED BOUNDS (not pivot).
    /// This is where the object ACTUALLY is visually.
    /// </summary>
    private Vector2Int GetChunkFromRenderedBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length > 0)
        {
            // Calculate combined bounds of all renderers
            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }

            // Use bounds center (where most of the visual content is)
            return m_Config.WorldToChunk(combinedBounds.center);
        }

        // No renderers - use transform position as fallback
        return m_Config.WorldToChunk(obj.transform.position);
    }

    private int CountAllChildren(Transform parent)
    {
        int count = 1;
        foreach (Transform child in parent)
        {
            count += CountAllChildren(child);
        }
        return count;
    }

    private void SplitScene()
    {
        if (!Directory.Exists(m_OutputFolder))
        {
            Directory.CreateDirectory(m_OutputFolder);
        }

        Scene sourceScene = EditorSceneManager.OpenScene(m_SourceScenePath, OpenSceneMode.Single);
        GameObject[] allObjects = sourceScene.GetRootGameObjects();

        Dictionary<Vector2Int, List<GameObject>> objectsByChunk = new Dictionary<Vector2Int, List<GameObject>>();
        List<GameObject> persistentObjects = new List<GameObject>();

        foreach (var obj in allObjects)
        {
            if (ShouldStayInPersistent(obj))
            {
                persistentObjects.Add(obj);
                continue;
            }

            // SPECIAL: Always split spawner containers
            if (IsSpawnerContainer(obj))
            {
                Debug.Log($"[ImprovedChunkSplitter] Detected spawner container: {obj.name} - splitting children individually");
                SplitSpawnerContainer(obj, objectsByChunk);
                continue;
            }

            switch (m_SplitMode)
            {
                case SplitMode.SmartByBounds:
                    AssignObjectByRenderedBounds(obj, objectsByChunk);
                    break;

                case SplitMode.AggressiveSplit:
                    if (obj.transform.childCount > 0)
                    {
                        SplitAllChildren(obj, objectsByChunk);
                    }
                    else
                    {
                        AssignObjectByRenderedBounds(obj, objectsByChunk);
                    }
                    break;

                case SplitMode.KeepHierarchies:
                    // Use transform position (old behavior)
                    Vector2Int chunk = m_Config.WorldToChunk(obj.transform.position);
                    chunk.x = Mathf.Clamp(chunk.x, 0, m_Config.gridSizeX - 1);
                    chunk.y = Mathf.Clamp(chunk.y, 0, m_Config.gridSizeZ - 1);

                    if (!objectsByChunk.ContainsKey(chunk))
                        objectsByChunk[chunk] = new List<GameObject>();
                    objectsByChunk[chunk].Add(obj);
                    break;
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
            Debug.Log($"[ImprovedChunkSplitter] Created persistent scene with {persistentObjects.Count} objects: {persistentPath}");
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
                        if (obj != null)
                        {
                            SceneManager.MoveGameObjectToScene(obj, chunkScene);
                        }
                    }
                }

                EditorSceneManager.SaveScene(chunkScene, scenePath);
                createdScenes.Add(scenePath);

                int objCount = objectsByChunk.ContainsKey(coord) ? objectsByChunk[coord].Count : 0;
                Debug.Log($"[ImprovedChunkSplitter] Created {sceneName} with {objCount} objects");
            }
        }

        // Add scenes to build settings
        AddScenesToBuildSettings(createdScenes);

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success",
            $"Created {m_Config.gridSizeX * m_Config.gridSizeZ} chunk scenes in:\n{m_OutputFolder}\n\nScenes have been added to Build Settings.",
            "OK");
    }

    /// <summary>
    /// Assign an object to a chunk based on its RENDERED BOUNDS CENTER, not pivot.
    /// </summary>
    private void AssignObjectByRenderedBounds(GameObject obj, Dictionary<Vector2Int, List<GameObject>> objectsByChunk)
    {
        Vector2Int chunk = GetChunkFromRenderedBounds(obj);

        // Clamp to valid range
        chunk.x = Mathf.Clamp(chunk.x, 0, m_Config.gridSizeX - 1);
        chunk.y = Mathf.Clamp(chunk.y, 0, m_Config.gridSizeZ - 1);

        if (!objectsByChunk.ContainsKey(chunk))
            objectsByChunk[chunk] = new List<GameObject>();

        objectsByChunk[chunk].Add(obj);
    }

    /// <summary>
    /// Aggressively split all children into individual objects assigned to their actual positions.
    /// </summary>
    private void SplitAllChildren(GameObject obj, Dictionary<Vector2Int, List<GameObject>> objectsByChunk)
    {
        // Get all children
        List<Transform> children = new List<Transform>();
        foreach (Transform child in obj.transform)
        {
            children.Add(child);
        }

        foreach (var child in children)
        {
            // Unparent
            child.SetParent(null);

            // Recursively split if it has children too
            if (child.childCount > 0)
            {
                SplitAllChildren(child.gameObject, objectsByChunk);
            }
            else
            {
                // Assign based on rendered bounds
                AssignObjectByRenderedBounds(child.gameObject, objectsByChunk);
            }
        }

        // Destroy the now-empty parent
        if (obj.transform.childCount == 0)
        {
            Object.DestroyImmediate(obj);
        }
    }

    /// <summary>
    /// Check if this is a spawner container that should be split.
    /// </summary>
    private bool IsSpawnerContainer(GameObject obj)
    {
        string name = obj.name.ToLower();

        // Check name patterns
        if (name.Contains("spawn") && name.Contains("container")) return true;
        if (name.Contains("spawners") && obj.transform.childCount > 0) return true;
        if (name == "zombiespawners" || name == "zombie spawners") return true;
        if (name == "enemyspawners" || name == "enemy spawners") return true;
        if (name.Contains("spawnpoints") && obj.transform.childCount > 0) return true;

        // Check if it's an empty parent with multiple spawner children
        if (obj.transform.childCount >= 3) // At least 3 children
        {
            int spawnerCount = 0;
            foreach (Transform child in obj.transform)
            {
                if (child.GetComponent<ZombieSpawner>() != null)
                {
                    spawnerCount++;
                    if (spawnerCount >= 3) return true; // 3+ spawners = container
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Split spawner container children into their respective chunks.
    /// </summary>
    private void SplitSpawnerContainer(GameObject container, Dictionary<Vector2Int, List<GameObject>> objectsByChunk)
    {
        List<Transform> children = new List<Transform>();
        foreach (Transform child in container.transform)
        {
            children.Add(child);
        }

        foreach (var child in children)
        {
            // IMPORTANT: Capture world position BEFORE unparenting
            // After SetParent(null), child.position may not reflect correct world position immediately
            Vector3 worldPos = child.position;

            // Unparent the spawner
            child.SetParent(null);

            // Assign to chunk based on spawner's captured world position
            Vector2Int chunk = m_Config.WorldToChunk(worldPos);
            chunk.x = Mathf.Clamp(chunk.x, 0, m_Config.gridSizeX - 1);
            chunk.y = Mathf.Clamp(chunk.y, 0, m_Config.gridSizeZ - 1);

            if (!objectsByChunk.ContainsKey(chunk))
                objectsByChunk[chunk] = new List<GameObject>();

            objectsByChunk[chunk].Add(child.gameObject);

            Debug.Log($"[ImprovedChunkSplitter] Spawner '{child.name}' at {worldPos} → Chunk ({chunk.x},{chunk.y})");
        }

        // Destroy the now-empty container
        Object.DestroyImmediate(container);
    }

    private bool ShouldStayInPersistent(GameObject obj)
    {
        string name = obj.name.ToLower();

        // Keep these in persistent scene
        if (name.Contains("player")) return true;
        if (name.Contains("camera")) return true;
        Light light = obj.GetComponent<Light>();
        if (name.Contains("light") && light != null && light.type == LightType.Directional) return true;
        if (name.Contains("sun")) return true;
        if (name.Contains("canvas")) return true;
        if (name.Contains("manager")) return true;
        if (name.Contains("eventsystem")) return true;
        if (name.Contains("cozy")) return true;
        if (name.Contains("chunkloader")) return true;
        if (name.Contains("inventorysystemmanager")) return true;
        if (name.Contains("simplelootui")) return true;
        if (name.Contains("simpleinventoryui")) return true;
        if (name.Contains("achievementmanager")) return true;
        if (name.Contains("achievementpopup")) return true;
        if (name.Contains("poidiscoverypopup")) return true;
        if (name.Contains("characterinfopopup")) return true;
        if (name.Contains("crosshair")) return true;
        if (name.Contains("playerspawn")) return true;
        if (name.Contains("spawnpoint")) return true;
        if (name.Contains("dogcompanion")) return true;

        // Check for specific components
        if (obj.GetComponent<Camera>() != null) return true;
        if (obj.GetComponent<Canvas>() != null) return true;
        if (obj.GetComponent<ChunkLoader>() != null) return true;
        if (obj.GetComponent<AudioListener>() != null) return true;
        if (obj.GetComponent<SimpleLootUI>() != null) return true;
        if (obj.GetComponent<SimpleInventoryUI>() != null) return true;

        var cameraController = obj.GetComponent("CameraController");
        if (cameraController != null) return true;

        var inventorySystemManager = obj.GetComponent("InventorySystemManager");
        if (inventorySystemManager != null) return true;

        var childISM = obj.GetComponentInChildren(System.Type.GetType("Opsive.UltimateInventorySystem.Core.InventorySystemManager, Opsive.UltimateInventorySystem"));
        if (childISM != null) return true;

        return false;
    }

    private void AddScenesToBuildSettings(List<string> scenePaths)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

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
