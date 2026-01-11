using UnityEngine;

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

    [Header("Debug")]
    [SerializeField] private bool m_SpawnOnStart = true;
    [SerializeField] private int m_DebugCharacterIndex = -1; // -1 = random

    private GameObject m_SpawnedPlayer;

    private void Awake()
    {
        if (m_SpawnOnStart)
        {
            SpawnPlayer();
        }
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

        // Spawn
        m_SpawnedPlayer = Instantiate(prefab, spawnPos, spawnRot);
        m_SpawnedPlayer.name = "Player";
        m_SpawnedPlayer.tag = "Player";

        Debug.Log($"[CharacterSpawner] Spawned: {prefab.name} ({index + 1}/{m_PlayerPrefabs.Length})");

        return m_SpawnedPlayer;
    }

    public GameObject GetSpawnedPlayer() => m_SpawnedPlayer;
}
