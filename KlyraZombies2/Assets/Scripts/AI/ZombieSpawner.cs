using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Surroundead-style area-based zombie spawner.
/// Spawns zombies within an area, respects spawn limits, and handles respawning.
///
/// CHUNK-BASED SPAWNING MODE (default):
/// - Spawns when chunk loads and player is at least MinPlayerDistance away
/// - Ignores MaxPlayerDistance (spawns even if player is 100m+ away)
/// - Perfect for chunk streaming systems
///
/// CLASSIC MODE (UseChunkBasedSpawning = false):
/// - Only spawns when player is 15m-80m away (classic Surroundead behavior)
/// - Use this for non-chunked games
/// </summary>
public class ZombieSpawner : MonoBehaviour
{
    [Header("Spawn Area")]
    [SerializeField] private float m_SpawnRadius = 20f;
    [SerializeField] private bool m_UseBoxArea = false;
    [SerializeField] private Vector3 m_BoxSize = new Vector3(20, 5, 20);

    [Header("Zombie Prefabs")]
    [SerializeField] private GameObject[] m_ZombiePrefabs;
    [SerializeField] private float m_RunnerChance = 0.1f;

    [Header("Spawn Settings")]
    [SerializeField] private int m_MinSpawnCount = 2;
    [SerializeField] private int m_MaxSpawnCount = 8;
    [SerializeField] private float m_InitialSpawnDelay = 1f;
    [SerializeField] private float m_RespawnTime = 300f; // 5 minutes like Surroundead
    [SerializeField] private bool m_RespawnEnabled = true;

    [Header("Player Distance")]
    [Tooltip("Minimum distance from player to spawn (prevents spawning on top of player)")]
    [SerializeField] private float m_MinPlayerDistance = 15f;
    [Tooltip("Maximum spawn distance (only used if chunk-based spawning is disabled)")]
    [SerializeField] private float m_MaxPlayerDistance = 80f;
    [Tooltip("Distance at which zombies despawn")]
    [SerializeField] private float m_DespawnDistance = 100f;
    [Tooltip("If true, spawns when chunk loads (ignores max distance). If false, uses classic 15-80m range.")]
    [SerializeField] private bool m_UseChunkBasedSpawning = true;

    [Header("Spawn Blocking")]
    [SerializeField] private bool m_CanBeBlocked = true;
    [SerializeField] private float m_BlockCheckRadius = 10f;
    [SerializeField] private string m_BlockingTag = "SpawnBlocker";

    private List<ZombieAI> m_SpawnedZombies = new List<ZombieAI>();
    private float m_RespawnTimer;
    private bool m_HasSpawnedInitial = false;
    private Transform m_PlayerTransform;
    private ChunkConfig m_ChunkConfig;

    private void Start()
    {
        // Load chunk config for chunk-based spawning
        m_ChunkConfig = Resources.Load<ChunkConfig>("ChunkConfig");

        // Find player
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            m_PlayerTransform = player.transform;

        StartCoroutine(InitialSpawnRoutine());
    }

    private void Update()
    {
        // Check if our chunk is loaded - if not, despawn and wait
        if (!IsChunkLoaded())
        {
            if (m_SpawnedZombies.Count > 0)
            {
                DespawnAll();
            }
            return;
        }

        if (m_PlayerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                m_PlayerTransform = player.transform;
            return;
        }

        // Check distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, m_PlayerTransform.position);

        // Despawn if too far
        if (distanceToPlayer > m_DespawnDistance)
        {
            DespawnAll();
            return;
        }

        // Don't spawn if player too close
        if (distanceToPlayer < m_MinPlayerDistance)
        {
            return;
        }

        // In chunk mode, ignore max distance - spawn as long as chunk is loaded
        // In non-chunk mode, enforce max distance
        if (!m_UseChunkBasedSpawning && distanceToPlayer > m_MaxPlayerDistance)
        {
            return;
        }

        // Check for respawn
        if (m_RespawnEnabled && m_HasSpawnedInitial)
        {
            CleanupDeadZombies();

            if (m_SpawnedZombies.Count == 0)
            {
                m_RespawnTimer += Time.deltaTime;
                if (m_RespawnTimer >= m_RespawnTime)
                {
                    SpawnZombies();
                    m_RespawnTimer = 0;
                }
            }
        }
    }

    /// <summary>
    /// Check if this spawner's chunk is currently loaded.
    /// </summary>
    private bool IsChunkLoaded()
    {
        // No chunk system - always loaded
        if (m_ChunkConfig == null || ChunkLoader.Instance == null)
            return true;

        // Get our chunk coordinate
        Vector2Int myChunk = m_ChunkConfig.WorldToChunk(transform.position);
        string chunkName = m_ChunkConfig.GetChunkSceneName(myChunk);

        // IMPORTANT: If we're IN a chunk scene, we're loaded (even if ChunkLoader hasn't registered us yet)
        // This handles race conditions during scene loading
        if (gameObject.scene.name == chunkName)
            return true;

        // Check if loaded via ChunkLoader
        return ChunkLoader.Instance.LoadedChunks.Contains(chunkName);
    }

    private IEnumerator InitialSpawnRoutine()
    {
        yield return new WaitForSeconds(m_InitialSpawnDelay);

        // Wait for chunk to be loaded (with timeout to handle race conditions)
        float waitTime = 0f;
        float maxWaitTime = 10f; // Give up after 10 seconds

        while (!IsChunkLoaded() && waitTime < maxWaitTime)
        {
            yield return new WaitForSeconds(0.5f);
            waitTime += 0.5f;
        }

        // If we timed out, assume we're in a loaded chunk (we wouldn't be running if we weren't)
        if (waitTime >= maxWaitTime)
        {
            Debug.LogWarning($"[ZombieSpawner] {name} timed out waiting for chunk confirmation. Spawning anyway.");
        }

        // Wait until player is in range
        while (m_PlayerTransform == null)
        {
            yield return new WaitForSeconds(1f);
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                m_PlayerTransform = player.transform;
        }

        // Wait for player to be in valid range
        while (true)
        {
            // Also check chunk is still loaded
            if (!IsChunkLoaded())
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            float dist = Vector3.Distance(transform.position, m_PlayerTransform.position);

            // In chunk mode: only need to be far enough away (min distance)
            // In non-chunk mode: need to be in the 15-80m range
            bool inRange = m_UseChunkBasedSpawning
                ? (dist >= m_MinPlayerDistance && dist <= m_DespawnDistance)
                : (dist >= m_MinPlayerDistance && dist <= m_MaxPlayerDistance);

            if (inRange)
                break;

            yield return new WaitForSeconds(1f);
        }

        SpawnZombies();
        m_HasSpawnedInitial = true;
    }

    private void SpawnZombies()
    {
        // Check if blocked
        if (m_CanBeBlocked && IsBlocked())
        {
            Debug.Log($"[ZombieSpawner] {name} is blocked by nearby object");
            return;
        }

        // Check global limit
        if (ZombieManager.Instance != null && !ZombieManager.Instance.CanSpawnZombie())
        {
            Debug.Log("[ZombieSpawner] Global zombie limit reached");
            return;
        }

        int spawnCount = Random.Range(m_MinSpawnCount, m_MaxSpawnCount + 1);

        for (int i = 0; i < spawnCount; i++)
        {
            // Check global limit each spawn
            if (ZombieManager.Instance != null && !ZombieManager.Instance.CanSpawnZombie())
                break;

            SpawnSingleZombie();
        }

        Debug.Log($"[ZombieSpawner] Spawned {m_SpawnedZombies.Count} zombies at {name}");
    }

    private void SpawnSingleZombie()
    {
        if (m_ZombiePrefabs == null || m_ZombiePrefabs.Length == 0)
        {
            Debug.LogWarning("[ZombieSpawner] No zombie prefabs assigned!");
            return;
        }

        // Get spawn position
        Vector3 spawnPos = GetRandomSpawnPosition();
        if (spawnPos == Vector3.zero) return;

        // Select random prefab
        GameObject prefab = m_ZombiePrefabs[Random.Range(0, m_ZombiePrefabs.Length)];

        // Spawn zombie
        GameObject zombieObj = Instantiate(prefab, spawnPos, Quaternion.Euler(0, Random.Range(0, 360), 0));

        var zombieAI = zombieObj.GetComponent<ZombieAI>();
        if (zombieAI != null)
        {
            m_SpawnedZombies.Add(zombieAI);
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        int maxAttempts = 10;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 randomPos;

            if (m_UseBoxArea)
            {
                randomPos = transform.position + new Vector3(
                    Random.Range(-m_BoxSize.x / 2, m_BoxSize.x / 2),
                    0,
                    Random.Range(-m_BoxSize.z / 2, m_BoxSize.z / 2)
                );
            }
            else
            {
                Vector2 randomCircle = Random.insideUnitCircle * m_SpawnRadius;
                randomPos = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            }

            // Try NavMesh first
            if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 50f, NavMesh.AllAreas))
            {
                // Check if not too close to player
                if (m_PlayerTransform != null)
                {
                    if (Vector3.Distance(hit.position, m_PlayerTransform.position) < m_MinPlayerDistance)
                        continue;
                }

                return hit.position;
            }

            // Fallback: Raycast to find ground
            Vector3 rayStart = randomPos + Vector3.up * 100f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit groundHit, 200f))
            {
                Vector3 spawnPos = groundHit.point;

                // Check if not too close to player
                if (m_PlayerTransform != null)
                {
                    if (Vector3.Distance(spawnPos, m_PlayerTransform.position) < m_MinPlayerDistance)
                        continue;
                }

                return spawnPos;
            }
        }

        // Last resort: spawn at spawner position
        Debug.LogWarning($"[ZombieSpawner] Using fallback spawn position at {name}");
        return transform.position;
    }

    private bool IsBlocked()
    {
        // If no blocking tag specified, not blocked
        if (string.IsNullOrEmpty(m_BlockingTag))
            return false;

        Collider[] blockers = Physics.OverlapSphere(transform.position, m_BlockCheckRadius);
        foreach (var collider in blockers)
        {
            // Use gameObject.tag instead of CompareTag to avoid exception on undefined tags
            try
            {
                if (collider.CompareTag(m_BlockingTag))
                    return true;
            }
            catch
            {
                // Tag doesn't exist, that's fine - not blocked
            }
        }
        return false;
    }

    private void CleanupDeadZombies()
    {
        m_SpawnedZombies.RemoveAll(z => z == null || z.CurrentState == ZombieAI.ZombieState.Dead);
    }

    private void DespawnAll()
    {
        foreach (var zombie in m_SpawnedZombies)
        {
            if (zombie != null)
            {
                Destroy(zombie.gameObject);
            }
        }
        m_SpawnedZombies.Clear();
        m_HasSpawnedInitial = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);

        if (m_UseBoxArea)
        {
            Gizmos.DrawCube(transform.position, m_BoxSize);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, m_BoxSize);
        }
        else
        {
            // Draw sphere
            Gizmos.DrawSphere(transform.position, m_SpawnRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, m_SpawnRadius);
        }

        // Draw min player distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, m_MinPlayerDistance);

        // Draw block check radius
        if (m_CanBeBlocked)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, m_BlockCheckRadius);
        }
    }
}
