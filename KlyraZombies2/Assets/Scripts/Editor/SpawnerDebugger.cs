using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Linq;

/// <summary>
/// Debug tool to diagnose zombie spawner issues.
/// Menu: Project Klyra > Debug > Spawner Debugger
/// </summary>
public class SpawnerDebugger : EditorWindow
{
    [MenuItem("Project Klyra/Debug/Spawner Debugger")]
    public static void ShowWindow()
    {
        GetWindow<SpawnerDebugger>("Spawner Debugger");
    }

    private void OnGUI()
    {
        GUILayout.Label("Spawner Debugger", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Click 'Analyze Spawners' to check why zombies aren't spawning.", MessageType.Info);

        if (GUILayout.Button("Analyze All Spawners", GUILayout.Height(30)))
        {
            AnalyzeSpawners();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Find All Spawners in Scene", GUILayout.Height(30)))
        {
            FindAllSpawners();
        }
    }

    private void AnalyzeSpawners()
    {
        Debug.Log("=== SPAWNER ANALYSIS ===");

        // Find all spawners in loaded scenes
        ZombieSpawner[] spawners = FindObjectsOfType<ZombieSpawner>();

        if (spawners.Length == 0)
        {
            Debug.LogError("✗ NO ZOMBIE SPAWNERS FOUND IN LOADED SCENES!");
            Debug.Log("Make sure you have spawners in your scene and the scene is loaded.");
            return;
        }

        Debug.Log($"Found {spawners.Length} zombie spawners");

        // Check player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("✗ NO PLAYER FOUND! Spawners need a player to activate.");
        }
        else
        {
            Debug.Log($"✓ Player found: {player.name} at {player.transform.position}");
        }

        // Check ZombieManager
        ZombieManager manager = FindObjectOfType<ZombieManager>();
        if (manager == null)
        {
            Debug.LogWarning("⚠ NO ZOMBIE MANAGER FOUND! Spawners may be limited.");
        }
        else
        {
            Debug.Log($"✓ ZombieManager found");
        }

        // Check ChunkLoader
        ChunkLoader chunkLoader = FindObjectOfType<ChunkLoader>();
        if (chunkLoader == null)
        {
            Debug.LogWarning("⚠ NO CHUNK LOADER FOUND! Chunk-based spawning disabled.");
        }
        else
        {
            Debug.Log($"✓ ChunkLoader found - {chunkLoader.LoadedChunks.Count} chunks loaded");
        }

        // Check each spawner
        for (int i = 0; i < spawners.Length; i++)
        {
            ZombieSpawner spawner = spawners[i];
            Debug.Log($"\n--- Spawner {i + 1}: {spawner.name} ---");
            Debug.Log($"Position: {spawner.transform.position}");
            Debug.Log($"Scene: {spawner.gameObject.scene.name}");

            // Check if scene is loaded
            if (!spawner.gameObject.scene.isLoaded)
            {
                Debug.LogError($"✗ Spawner's scene is NOT LOADED!");
                continue;
            }

            // Check if GameObject is active
            if (!spawner.gameObject.activeInHierarchy)
            {
                Debug.LogError($"✗ Spawner GameObject is INACTIVE!");
                continue;
            }

            // Check if component is enabled
            if (!spawner.enabled)
            {
                Debug.LogError($"✗ Spawner component is DISABLED!");
                continue;
            }

            // Check zombie prefabs using reflection
            var prefabsField = typeof(ZombieSpawner).GetField("m_ZombiePrefabs",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (prefabsField != null)
            {
                GameObject[] prefabs = prefabsField.GetValue(spawner) as GameObject[];
                if (prefabs == null || prefabs.Length == 0)
                {
                    Debug.LogError($"✗ NO ZOMBIE PREFABS ASSIGNED!");
                }
                else
                {
                    Debug.Log($"✓ {prefabs.Length} zombie prefabs assigned");
                }
            }

            // Check distance to player
            if (player != null)
            {
                float distance = Vector3.Distance(spawner.transform.position, player.transform.position);
                Debug.Log($"Distance to player: {distance:F1}m");

                // Get min/max distances using reflection
                var minDistField = typeof(ZombieSpawner).GetField("m_MinPlayerDistance",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var maxDistField = typeof(ZombieSpawner).GetField("m_MaxPlayerDistance",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (minDistField != null && maxDistField != null)
                {
                    float minDist = (float)minDistField.GetValue(spawner);
                    float maxDist = (float)maxDistField.GetValue(spawner);
                    Debug.Log($"Spawn range: {minDist}m - {maxDist}m");

                    if (distance < minDist)
                    {
                        Debug.LogWarning($"⚠ Player TOO CLOSE (need {minDist}m minimum)");
                    }
                    else if (distance > maxDist)
                    {
                        Debug.LogWarning($"⚠ Player TOO FAR (need {maxDist}m maximum)");
                    }
                    else
                    {
                        Debug.Log($"✓ Player in spawn range!");
                    }
                }
            }

            // Check if chunk is loaded (if using chunks)
            if (chunkLoader != null)
            {
                var configField = typeof(ZombieSpawner).GetField("m_ChunkConfig",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (configField != null)
                {
                    ChunkConfig config = configField.GetValue(spawner) as ChunkConfig;
                    if (config != null)
                    {
                        Vector2Int myChunk = config.WorldToChunk(spawner.transform.position);
                        string chunkName = config.GetChunkSceneName(myChunk);
                        bool isLoaded = chunkLoader.LoadedChunks.Contains(chunkName);

                        Debug.Log($"Spawner chunk: {chunkName} ({myChunk})");
                        if (isLoaded)
                        {
                            Debug.Log($"✓ Chunk IS LOADED");
                        }
                        else
                        {
                            Debug.LogError($"✗ Chunk NOT LOADED! Spawner will not activate.");
                        }
                    }
                }
            }
        }

        Debug.Log("\n=== END ANALYSIS ===");
    }

    private void FindAllSpawners()
    {
        // Find in all loaded scenes
        ZombieSpawner[] spawners = FindObjectsOfType<ZombieSpawner>();

        Debug.Log($"=== Found {spawners.Length} Zombie Spawners ===");

        foreach (var spawner in spawners)
        {
            Debug.Log($"- {spawner.name} in scene '{spawner.gameObject.scene.name}' at {spawner.transform.position}", spawner.gameObject);
        }

        // Also check unloaded scenes in build settings
        int totalScenes = SceneManager.sceneCountInBuildSettings;
        Debug.Log($"\nTotal scenes in build: {totalScenes}");
        Debug.Log("Loaded scenes:");
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            Debug.Log($"  - {scene.name} (loaded: {scene.isLoaded})");
        }
    }
}
