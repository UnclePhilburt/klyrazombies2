using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif

/// <summary>
/// Loads and unloads world chunks based on player position.
/// Attach to player or a persistent manager object.
/// Supports both regular SceneManager and Addressables for streaming.
/// </summary>
public class ChunkLoader : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Chunk configuration asset")]
    [SerializeField] private ChunkConfig m_Config;

    [Tooltip("Use Addressables for chunk loading (reduces initial download size for WebGL)")]
    [SerializeField] private bool m_UseAddressables = false;

    [Tooltip("Transform to track (usually player). If null, uses this transform.")]
    [SerializeField] private Transform m_Target;

    [Header("Initial Load")]
    [Tooltip("Load all chunks on start, then switch to streaming after player spawns")]
    [SerializeField] private bool m_LoadAllOnStart = true;

    [Tooltip("If not loading all, load chunks around this position at start")]
    [SerializeField] private Vector3 m_InitialLoadPosition = Vector3.zero;

    [Tooltip("Multiple spawn points - picks one randomly (or use CharacterSpawner)")]
    [SerializeField] private Transform[] m_SpawnPoints;

    [Tooltip("Reference to CharacterSpawner to get spawn points from it")]
    [SerializeField] private CharacterSpawner m_CharacterSpawner;

    [Tooltip("Time to wait after loading all chunks before switching to streaming mode")]
    [SerializeField] private float m_InitialLoadDelay = 2f;

    [Header("Debug")]
    [SerializeField] private bool m_ShowDebugInfo = true;

    // State
    private Vector2Int m_CurrentChunk = new Vector2Int(-999, -999);
    private HashSet<string> m_LoadedChunks = new HashSet<string>();
    private HashSet<string> m_LoadingChunks = new HashSet<string>();
    private HashSet<string> m_UnloadingChunks = new HashSet<string>();
    private bool m_InitialLoadComplete = false;
    private bool m_StreamingEnabled = false;

    #if UNITY_ADDRESSABLES
    // Track Addressables handles for proper cleanup
    private Dictionary<string, AsyncOperationHandle<SceneInstance>> m_AddressableHandles = new Dictionary<string, AsyncOperationHandle<SceneInstance>>();
    #endif

    // Singleton for easy access
    public static ChunkLoader Instance { get; private set; }

    // Public properties
    public Vector2Int CurrentChunk => m_CurrentChunk;
    public IReadOnlyCollection<string> LoadedChunks => m_LoadedChunks;
    public ChunkConfig Config => m_Config;
    public bool InitialLoadComplete => m_InitialLoadComplete;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        Debug.Log($"[ChunkLoader] Start() - m_Config assigned: {m_Config != null}, m_LoadAllOnStart: {m_LoadAllOnStart}");

        if (m_Config == null)
        {
            m_Config = Resources.Load<ChunkConfig>("ChunkConfig");
            Debug.Log($"[ChunkLoader] Loaded ChunkConfig from Resources: {m_Config != null}");
            if (m_Config == null)
            {
                Debug.LogError("[ChunkLoader] No ChunkConfig assigned and none found in Resources!");
                enabled = false;
                return;
            }
        }

        Debug.Log($"[ChunkLoader] Config: gridSize={m_Config.gridSizeX}x{m_Config.gridSizeZ}, chunkSize={m_Config.chunkSize}, prefix={m_Config.chunkScenePrefix}");
        Debug.Log($"[ChunkLoader] Total scenes in build: {SceneManager.sceneCountInBuildSettings}");

        if (m_Target == null)
            m_Target = transform;

        if (m_LoadAllOnStart)
        {
            // Load ALL chunks first so spawn points are available
            Debug.Log("[ChunkLoader] Starting LoadAllChunksThenStream...");
            StartCoroutine(LoadAllChunksThenStream());
        }
        else
        {
            // Only load chunks around spawn position
            Debug.Log("[ChunkLoader] Starting in streaming mode with spawn position...");
            StartCoroutine(LoadAroundSpawnThenStream());
        }
    }

    private IEnumerator LoadAllChunksThenStream()
    {
        if (m_ShowDebugInfo)
            Debug.Log("[ChunkLoader] Loading all chunks for initial spawn...");

        // Find and freeze player while loading
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Rigidbody playerRb = null;
        Vector3 originalPosition = Vector3.zero;
        bool wasKinematic = false;

        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                originalPosition = player.transform.position;
                wasKinematic = playerRb.isKinematic;
                playerRb.isKinematic = true; // Freeze physics
                if (m_ShowDebugInfo)
                    Debug.Log("[ChunkLoader] Froze player physics while loading chunks");
            }
        }

        // Load every chunk
        for (int x = 0; x < m_Config.gridSizeX; x++)
        {
            for (int z = 0; z < m_Config.gridSizeZ; z++)
            {
                string sceneName = m_Config.GetChunkSceneName(x, z);
                if (!m_LoadedChunks.Contains(sceneName) && !m_LoadingChunks.Contains(sceneName))
                {
                    StartCoroutine(LoadChunkImmediate(sceneName));
                }
            }
        }

        // Wait for all chunks to load
        while (m_LoadingChunks.Count > 0)
        {
            yield return null;
        }

        m_InitialLoadComplete = true;

        if (m_ShowDebugInfo)
            Debug.Log($"[ChunkLoader] All {m_LoadedChunks.Count} chunks loaded.");

        // Wait a frame for physics to settle
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // Unfreeze player
        if (playerRb != null)
        {
            // Reset position in case they moved
            player.transform.position = originalPosition;
            playerRb.isKinematic = wasKinematic;
            playerRb.linearVelocity = Vector3.zero;
            if (m_ShowDebugInfo)
                Debug.Log("[ChunkLoader] Unfroze player physics");
        }

        // Wait for spawn to happen
        yield return new WaitForSeconds(m_InitialLoadDelay);

        // Find the player for tracking
        var spawnedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (spawnedPlayer != null)
        {
            m_Target = spawnedPlayer.transform;
            if (m_ShowDebugInfo)
                Debug.Log($"[ChunkLoader] Found spawned player: {spawnedPlayer.name} at {spawnedPlayer.transform.position}");
        }
        else if (m_ShowDebugInfo)
        {
            Debug.LogWarning("[ChunkLoader] No player found after initial load delay!");
        }

        // Now switch to streaming mode - unload distant chunks
        m_StreamingEnabled = true;

        if (m_Target != null)
        {
            m_CurrentChunk = m_Config.WorldToChunk(m_Target.position);
            if (m_ShowDebugInfo)
                Debug.Log($"[ChunkLoader] Switching to streaming mode. Player in chunk {m_CurrentChunk}");

            // Unload chunks that are too far from player
            UpdateChunks(false);
        }
        else if (m_ShowDebugInfo)
        {
            Debug.Log("[ChunkLoader] Streaming enabled but no target yet - will find player in Update()");
        }
    }

    private IEnumerator LoadAroundSpawnThenStream()
    {
        // Gather all possible spawn positions
        List<Vector3> spawnPositions = new List<Vector3>();

        // First priority: Get spawn points from CharacterSpawner
        if (m_CharacterSpawner != null && m_CharacterSpawner.SpawnPoints != null)
        {
            foreach (var point in m_CharacterSpawner.SpawnPoints)
            {
                if (point != null)
                    spawnPositions.Add(point.position);
            }
            if (m_ShowDebugInfo)
                Debug.Log($"[ChunkLoader] Got {spawnPositions.Count} spawn points from CharacterSpawner");
        }

        // Second priority: Use our own spawn points array
        if (spawnPositions.Count == 0 && m_SpawnPoints != null)
        {
            foreach (var point in m_SpawnPoints)
            {
                if (point != null)
                    spawnPositions.Add(point.position);
            }
            if (m_ShowDebugInfo)
                Debug.Log($"[ChunkLoader] Using {spawnPositions.Count} local spawn points");
        }

        // Fallback: Use initial load position
        if (spawnPositions.Count == 0)
        {
            spawnPositions.Add(m_InitialLoadPosition);
            if (m_ShowDebugInfo)
                Debug.Log($"[ChunkLoader] Using fallback initial load position: {m_InitialLoadPosition}");
        }

        if (m_ShowDebugInfo)
            Debug.Log($"[ChunkLoader] Loading chunks around {spawnPositions.Count} possible spawn point(s)");

        // Collect all unique chunks needed around ALL spawn positions
        HashSet<string> chunksToLoad = new HashSet<string>();
        int radius = m_Config.loadRadius;

        foreach (var spawnPos in spawnPositions)
        {
            Vector2Int spawnChunk = m_Config.WorldToChunk(spawnPos);

            if (m_ShowDebugInfo)
                Debug.Log($"[ChunkLoader] Spawn position {spawnPos} is in chunk {spawnChunk}");

            for (int x = spawnChunk.x - radius; x <= spawnChunk.x + radius; x++)
            {
                for (int z = spawnChunk.y - radius; z <= spawnChunk.y + radius; z++)
                {
                    if (m_Config.IsValidChunk(x, z))
                    {
                        string sceneName = m_Config.GetChunkSceneName(x, z);
                        if (!m_LoadedChunks.Contains(sceneName) && !m_LoadingChunks.Contains(sceneName))
                        {
                            chunksToLoad.Add(sceneName);
                        }
                    }
                }
            }
        }

        if (m_ShowDebugInfo)
            Debug.Log($"[ChunkLoader] Loading {chunksToLoad.Count} unique chunks around all spawn points...");

        // Load the chunks
        foreach (var sceneName in chunksToLoad)
        {
            StartCoroutine(LoadChunkImmediate(sceneName));
        }

        // Wait for all to load
        while (m_LoadingChunks.Count > 0)
        {
            yield return null;
        }

        m_InitialLoadComplete = true;

        // Set current chunk to first spawn position (will update when player spawns)
        m_CurrentChunk = m_Config.WorldToChunk(spawnPositions[0]);

        if (m_ShowDebugInfo)
            Debug.Log($"[ChunkLoader] Initial {m_LoadedChunks.Count} chunks loaded around spawn points.");

        // Wait a moment for things to settle
        yield return new WaitForSeconds(0.5f);

        // Enable streaming
        m_StreamingEnabled = true;

        if (m_ShowDebugInfo)
            Debug.Log("[ChunkLoader] Streaming mode enabled - will load/unload as player moves.");
    }

    private void Update()
    {
        if (m_Config == null)
        {
            if (m_ShowDebugInfo && Time.frameCount % 300 == 0)
                Debug.LogWarning("[ChunkLoader] Update: Config is null!");
            return;
        }

        if (!m_StreamingEnabled)
        {
            if (m_ShowDebugInfo && Time.frameCount % 300 == 0)
                Debug.LogWarning($"[ChunkLoader] Update: Streaming not enabled yet. InitialLoadComplete: {m_InitialLoadComplete}");
            return;
        }

        // Try to find player if we don't have a valid target
        if (m_Target == null || !m_Target.gameObject.activeInHierarchy)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                m_Target = player.transform;
                if (m_ShowDebugInfo)
                    Debug.Log($"[ChunkLoader] Found player target: {player.name}");
            }
            else
            {
                if (m_ShowDebugInfo && Time.frameCount % 300 == 0)
                    Debug.LogWarning("[ChunkLoader] Update: No player found!");
                return; // No player yet, wait
            }
        }

        Vector2Int newChunk = m_Config.WorldToChunk(m_Target.position);

        // Debug every 5 seconds if chunk hasn't changed
        if (m_ShowDebugInfo && Time.frameCount % 300 == 0)
        {
            Debug.Log($"[ChunkLoader] Update: Player at {m_Target.position}, Current chunk: {m_CurrentChunk}, Calculated chunk: {newChunk}");
        }

        if (newChunk != m_CurrentChunk)
        {
            if (m_ShowDebugInfo)
                Debug.Log($"[ChunkLoader] Chunk changed! {m_CurrentChunk} -> {newChunk}");

            m_CurrentChunk = newChunk;
            UpdateChunks(false);
        }
    }

    /// <summary>
    /// Update which chunks should be loaded/unloaded.
    /// </summary>
    private void UpdateChunks(bool immediate)
    {
        HashSet<string> neededChunks = GetNeededChunks();

        // Find chunks to unload
        List<string> toUnload = new List<string>();
        foreach (var chunk in m_LoadedChunks)
        {
            if (!neededChunks.Contains(chunk) && !m_UnloadingChunks.Contains(chunk))
            {
                toUnload.Add(chunk);
            }
        }

        // Find chunks to load
        List<string> toLoad = new List<string>();
        foreach (var chunk in neededChunks)
        {
            if (!m_LoadedChunks.Contains(chunk) && !m_LoadingChunks.Contains(chunk))
            {
                toLoad.Add(chunk);
            }
        }

        // Execute unloads
        foreach (var chunk in toUnload)
        {
            StartCoroutine(UnloadChunkAsync(chunk));
        }

        // Execute loads
        foreach (var chunk in toLoad)
        {
            if (immediate)
            {
                StartCoroutine(LoadChunkImmediate(chunk));
            }
            else
            {
                StartCoroutine(LoadChunkAsync(chunk));
            }
        }

        if (m_ShowDebugInfo && (toLoad.Count > 0 || toUnload.Count > 0))
        {
            Debug.Log($"[ChunkLoader] Player in chunk {m_CurrentChunk}. Loading: {toLoad.Count}, Unloading: {toUnload.Count}");
        }
    }

    /// <summary>
    /// Get all chunks that should be loaded based on player position.
    /// </summary>
    private HashSet<string> GetNeededChunks()
    {
        HashSet<string> needed = new HashSet<string>();
        int radius = m_Config.loadRadius;

        for (int x = m_CurrentChunk.x - radius; x <= m_CurrentChunk.x + radius; x++)
        {
            for (int z = m_CurrentChunk.y - radius; z <= m_CurrentChunk.y + radius; z++)
            {
                if (m_Config.IsValidChunk(x, z))
                {
                    needed.Add(m_Config.GetChunkSceneName(x, z));
                }
            }
        }

        return needed;
    }

    /// <summary>
    /// Load a chunk scene asynchronously with smooth activation.
    /// </summary>
    private IEnumerator LoadChunkAsync(string sceneName)
    {
        m_LoadingChunks.Add(sceneName);

        if (m_ShowDebugInfo)
            Debug.Log($"[ChunkLoader] Loading chunk: {sceneName} (Addressables: {m_UseAddressables})");

        #if UNITY_ADDRESSABLES
        if (m_UseAddressables)
        {
            // Use Addressables for loading
            var handle = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Additive, false);

            // Wait until download + load complete
            while (!handle.IsDone)
            {
                // Show download progress if available
                if (m_ShowDebugInfo && handle.PercentComplete < 0.9f)
                {
                    Debug.Log($"[ChunkLoader] Downloading {sceneName}: {handle.PercentComplete * 100:F0}%");
                }
                yield return null;
            }

            if (handle.Status == AsyncOperationStatus.Failed)
            {
                Debug.LogError($"[ChunkLoader] Failed to load chunk '{sceneName}': {handle.OperationException}");
                m_LoadingChunks.Remove(sceneName);
                yield break;
            }

            // Small delay to spread out activations
            yield return new WaitForSeconds(0.1f);

            // Activate the scene
            handle.Result.ActivateAsync();

            // Store handle for later unload
            m_AddressableHandles[sceneName] = handle;

            m_LoadingChunks.Remove(sceneName);
            m_LoadedChunks.Add(sceneName);

            if (m_ShowDebugInfo)
                Debug.Log($"[ChunkLoader] Loaded chunk: {sceneName}");

            yield break;
        }
        #endif

        // Use SceneManager for loading (non-Addressables mode)
        if (!IsSceneInBuild(sceneName))
        {
            Debug.LogWarning($"[ChunkLoader] Scene '{sceneName}' not found in build settings!");
            m_LoadingChunks.Remove(sceneName);
            yield break;
        }

        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (op == null)
        {
            m_LoadingChunks.Remove(sceneName);
            yield break;
        }

        op.allowSceneActivation = false;

        // Wait until 90% loaded
        while (op.progress < 0.9f)
        {
            yield return null;
        }

        // Small delay to spread out activations
        yield return new WaitForSeconds(0.1f);

        // Activate
        op.allowSceneActivation = true;

        // Wait for full load
        while (!op.isDone)
        {
            yield return null;
        }

        m_LoadingChunks.Remove(sceneName);
        m_LoadedChunks.Add(sceneName);

        if (m_ShowDebugInfo)
            Debug.Log($"[ChunkLoader] Loaded chunk: {sceneName}");
    }

    /// <summary>
    /// Load a chunk immediately (for initial load).
    /// </summary>
    private IEnumerator LoadChunkImmediate(string sceneName)
    {
        m_LoadingChunks.Add(sceneName);

        if (m_ShowDebugInfo)
            Debug.Log($"[ChunkLoader] Loading chunk (immediate): {sceneName} (Addressables: {m_UseAddressables})");

        #if UNITY_ADDRESSABLES
        if (m_UseAddressables)
        {
            // Use Addressables - activate immediately
            var handle = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Additive, true);

            while (!handle.IsDone)
            {
                yield return null;
            }

            if (handle.Status == AsyncOperationStatus.Failed)
            {
                Debug.LogError($"[ChunkLoader] Failed to load chunk '{sceneName}': {handle.OperationException}");
                m_LoadingChunks.Remove(sceneName);
                yield break;
            }

            // Store handle for later unload
            m_AddressableHandles[sceneName] = handle;

            m_LoadingChunks.Remove(sceneName);
            m_LoadedChunks.Add(sceneName);

            if (m_ShowDebugInfo)
                Debug.Log($"[ChunkLoader] Loaded chunk: {sceneName}");

            yield break;
        }
        #endif

        // Use SceneManager (non-Addressables mode)
        if (!IsSceneInBuild(sceneName))
        {
            Debug.LogWarning($"[ChunkLoader] Scene '{sceneName}' not found in build settings!");
            m_LoadingChunks.Remove(sceneName);
            yield break;
        }

        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (op == null)
        {
            m_LoadingChunks.Remove(sceneName);
            yield break;
        }

        while (!op.isDone)
        {
            yield return null;
        }

        m_LoadingChunks.Remove(sceneName);
        m_LoadedChunks.Add(sceneName);

        if (m_ShowDebugInfo)
            Debug.Log($"[ChunkLoader] Loaded chunk: {sceneName}");
    }

    /// <summary>
    /// Unload a chunk scene asynchronously.
    /// </summary>
    private IEnumerator UnloadChunkAsync(string sceneName)
    {
        m_UnloadingChunks.Add(sceneName);

        if (m_ShowDebugInfo)
            Debug.Log($"[ChunkLoader] Unloading chunk: {sceneName} (Addressables: {m_UseAddressables})");

        #if UNITY_ADDRESSABLES
        if (m_UseAddressables && m_AddressableHandles.ContainsKey(sceneName))
        {
            // Unload via Addressables
            var handle = m_AddressableHandles[sceneName];
            var unloadOp = Addressables.UnloadSceneAsync(handle);

            while (!unloadOp.IsDone)
            {
                yield return null;
            }

            if (unloadOp.Status == AsyncOperationStatus.Failed)
            {
                Debug.LogError($"[ChunkLoader] Failed to unload chunk '{sceneName}': {unloadOp.OperationException}");
            }

            m_AddressableHandles.Remove(sceneName);
            m_UnloadingChunks.Remove(sceneName);
            m_LoadedChunks.Remove(sceneName);

            if (m_ShowDebugInfo)
                Debug.Log($"[ChunkLoader] Unloaded chunk: {sceneName}");

            yield break;
        }
        #endif

        // Use SceneManager (non-Addressables mode)
        var op = SceneManager.UnloadSceneAsync(sceneName);
        if (op == null)
        {
            m_UnloadingChunks.Remove(sceneName);
            m_LoadedChunks.Remove(sceneName);
            yield break;
        }

        while (!op.isDone)
        {
            yield return null;
        }

        m_UnloadingChunks.Remove(sceneName);
        m_LoadedChunks.Remove(sceneName);

        if (m_ShowDebugInfo)
            Debug.Log($"[ChunkLoader] Unloaded chunk: {sceneName}");
    }

    /// <summary>
    /// Check if a scene is in the build settings.
    /// </summary>
    private bool IsSceneInBuild(string sceneName)
    {
        int sceneCount = SceneManager.sceneCountInBuildSettings;

        if (m_ShowDebugInfo && sceneCount == 0)
        {
            Debug.LogWarning("[ChunkLoader] sceneCountInBuildSettings is 0!");
        }

        for (int i = 0; i < sceneCount; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);

            // Handle both path formats - use simple string matching
            // Path could be "Assets/Scenes/Chunks/Chunk_0_0.unity" or just "Chunk_0_0"
            if (path.Contains(sceneName + ".unity") || path.EndsWith("/" + sceneName) || path == sceneName)
                return true;
        }

        if (m_ShowDebugInfo)
        {
            Debug.LogWarning($"[ChunkLoader] Scene '{sceneName}' not found in build settings (checked {sceneCount} scenes)");
        }

        return false;
    }

    /// <summary>
    /// Force reload all chunks (useful after teleporting player).
    /// </summary>
    public void ForceReloadChunks()
    {
        m_CurrentChunk = new Vector2Int(-999, -999);
        UpdateChunks(true);
    }

    private void OnGUI()
    {
        if (!m_ShowDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 250, 200));

        if (!m_InitialLoadComplete)
        {
            GUILayout.Label("Loading all chunks...");
            GUILayout.Label($"Loaded: {m_LoadedChunks.Count} / {m_Config.gridSizeX * m_Config.gridSizeZ}");
            GUILayout.Label($"Loading: {m_LoadingChunks.Count}");
        }
        else if (!m_StreamingEnabled)
        {
            GUILayout.Label("Waiting for player spawn...");
            GUILayout.Label($"All {m_LoadedChunks.Count} chunks loaded");
        }
        else
        {
            GUILayout.Label($"Current Chunk: {m_CurrentChunk}");
            GUILayout.Label($"Loaded: {m_LoadedChunks.Count}");
            GUILayout.Label($"Loading: {m_LoadingChunks.Count}");
            foreach (var chunk in m_LoadedChunks)
            {
                GUILayout.Label($"  - {chunk}");
            }
        }

        GUILayout.EndArea();
    }
}
