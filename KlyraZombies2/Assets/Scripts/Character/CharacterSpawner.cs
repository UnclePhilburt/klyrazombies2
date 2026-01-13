using UnityEngine;
using System.Collections;

/// <summary>
/// Spawns a random player character from a list of prefabs.
/// Attach this to a GameObject in the main game scene.
/// </summary>
public class CharacterSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Possible spawn locations - one is picked randomly")]
    [SerializeField] private Transform[] m_SpawnPoints;

    [Header("Character Prefabs")]
    [Tooltip("List of complete player prefabs - one is picked randomly")]
    [SerializeField] private GameObject[] m_PlayerPrefabs;

    [Header("Character Info Popup")]
    [Tooltip("Show character backstory popup on spawn")]
    [SerializeField] private bool m_ShowBackstoryPopup = true;
    [Tooltip("Delay before showing popup (lets player orient themselves)")]
    [SerializeField] private float m_PopupDelay = 1f;

    [Header("Chunk Loading")]
    [Tooltip("Wait for chunks to load before spawning")]
    [SerializeField] private bool m_WaitForChunks = false;
    [Tooltip("Maximum time to wait for chunks (seconds)")]
    [SerializeField] private float m_MaxWaitTime = 120f;

    [Header("Debug")]
    [SerializeField] private bool m_SpawnOnStart = true;
    [SerializeField] private int m_DebugCharacterIndex = -1; // -1 = random

    private GameObject m_SpawnedPlayer;
    private CharacterBackstory m_CurrentBackstory;

    private void Awake()
    {
        Debug.Log("[CharacterSpawner] Awake called");
    }

    private void Start()
    {
        Debug.Log($"[CharacterSpawner] Start called. SpawnOnStart={m_SpawnOnStart}, WaitForChunks={m_WaitForChunks}");

        if (m_SpawnOnStart)
        {
            if (m_WaitForChunks)
            {
                StartCoroutine(WaitForChunksThenSpawn());
            }
            else
            {
                SpawnPlayer();
            }
        }
    }

    private IEnumerator WaitForChunksThenSpawn()
    {
        float waitTime = 0f;

        Debug.Log("[CharacterSpawner] WaitForChunksThenSpawn started...");

        // Wait for ChunkLoader to exist
        while (ChunkLoader.Instance == null && waitTime < m_MaxWaitTime)
        {
            yield return null;
            waitTime += Time.deltaTime;
        }

        if (ChunkLoader.Instance != null)
        {
            Debug.Log($"[CharacterSpawner] ChunkLoader found, waiting for chunks... (waited {waitTime:F1}s so far)");

            // Wait for initial load to complete - NO timeout for this part
            while (!ChunkLoader.Instance.InitialLoadComplete)
            {
                yield return null;
                waitTime += Time.deltaTime;

                // Log progress every 5 seconds
                if (Mathf.FloorToInt(waitTime) % 5 == 0 && Time.deltaTime > 0)
                {
                    Debug.Log($"[CharacterSpawner] Still waiting for chunks... ({waitTime:F1}s)");
                }
            }

            Debug.Log($"[CharacterSpawner] Chunks loaded after {waitTime:F1}s, spawning player...");
        }
        else
        {
            Debug.LogWarning("[CharacterSpawner] No ChunkLoader found after timeout, spawning immediately");
        }

        SpawnPlayer();
    }

    /// <summary>
    /// Spawn a random player prefab at a random spawn point
    /// </summary>
    public GameObject SpawnPlayer()
    {
        if (m_PlayerPrefabs == null || m_PlayerPrefabs.Length == 0)
        {
            Debug.LogError("[CharacterSpawner] No player prefabs assigned!");
            return null;
        }

        // Pick random spawn point
        Vector3 spawnPos = transform.position;
        Quaternion spawnRot = transform.rotation;

        if (m_SpawnPoints != null && m_SpawnPoints.Length > 0)
        {
            Transform spawnPoint = m_SpawnPoints[Random.Range(0, m_SpawnPoints.Length)];
            spawnPos = spawnPoint.position;
            spawnRot = spawnPoint.rotation;
            Debug.Log($"[CharacterSpawner] Spawn point: {spawnPoint.name}");
        }

        // Pick random character prefab
        int index = m_DebugCharacterIndex >= 0
            ? Mathf.Clamp(m_DebugCharacterIndex, 0, m_PlayerPrefabs.Length - 1)
            : Random.Range(0, m_PlayerPrefabs.Length);

        GameObject prefab = m_PlayerPrefabs[index];
        if (prefab == null)
        {
            Debug.LogError($"[CharacterSpawner] Prefab at index {index} is null!");
            return null;
        }

        m_SpawnedPlayer = Instantiate(prefab, spawnPos, spawnRot);

        // Add destruction tracker
        var tracker = m_SpawnedPlayer.AddComponent<DestructionTracker>();
        tracker.spawnerRef = this;

        if (m_SpawnedPlayer == null)
        {
            Debug.LogError("[CharacterSpawner] Instantiate returned null!");
            return null;
        }

        m_SpawnedPlayer.name = "Player";
        m_SpawnedPlayer.tag = "Player";

        Debug.Log($"[CharacterSpawner] Spawned: {prefab.name} at {spawnPos} ({index + 1}/{m_PlayerPrefabs.Length})");
        Debug.Log($"[CharacterSpawner] Player tag is: {m_SpawnedPlayer.tag}, activeInHierarchy: {m_SpawnedPlayer.activeInHierarchy}");

        // Directly assign to camera
        var cameraController = FindFirstObjectByType<Opsive.UltimateCharacterController.Camera.CameraController>();
        if (cameraController != null)
        {
            cameraController.Character = m_SpawnedPlayer;
            Debug.Log($"[CharacterSpawner] Assigned player to camera controller");
        }
        else
        {
            Debug.LogWarning("[CharacterSpawner] No CameraController found!");
        }

        // Check if player still exists after a moment
        StartCoroutine(CheckPlayerExists());

        // Generate and show backstory
        if (m_ShowBackstoryPopup)
        {
            m_CurrentBackstory = CharacterBackstory.GenerateRandom();
            Invoke(nameof(ShowBackstoryPopup), m_PopupDelay);
        }

        return m_SpawnedPlayer;
    }

    private void ShowBackstoryPopup()
    {
        if (m_CurrentBackstory == null) return;

        // Find or create the popup
        CharacterInfoPopup popup = CharacterInfoPopup.Instance;
        if (popup == null)
        {
            popup = FindFirstObjectByType<CharacterInfoPopup>();
        }
        if (popup == null)
        {
            // Create one if it doesn't exist
            GameObject popupObj = new GameObject("CharacterInfoPopup");
            popup = popupObj.AddComponent<CharacterInfoPopup>();
        }

        popup.Show(m_CurrentBackstory);
        Debug.Log($"[CharacterSpawner] Showing backstory for: {m_CurrentBackstory.characterName}");
    }

    public GameObject GetSpawnedPlayer() => m_SpawnedPlayer;
    public CharacterBackstory GetCurrentBackstory() => m_CurrentBackstory;

    private IEnumerator CheckPlayerExists()
    {
        yield return new WaitForSeconds(0.1f);

        if (m_SpawnedPlayer == null)
        {
            Debug.LogError("[CharacterSpawner] Player was DESTROYED within 0.1 seconds of spawning!");
        }
        else
        {
            Debug.Log($"[CharacterSpawner] Player still exists at {m_SpawnedPlayer.transform.position}");
        }

        yield return new WaitForSeconds(1f);

        if (m_SpawnedPlayer == null)
        {
            Debug.LogError("[CharacterSpawner] Player was DESTROYED within 1 second of spawning!");
        }
        else
        {
            Debug.Log($"[CharacterSpawner] Player still exists after 1s at {m_SpawnedPlayer.transform.position}");
        }
    }
}
