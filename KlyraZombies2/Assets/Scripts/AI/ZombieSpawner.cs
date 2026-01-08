using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Surroundead-style area-based zombie spawner.
/// Spawns zombies within an area, respects spawn limits, and handles respawning.
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
    [SerializeField] private float m_MinPlayerDistance = 15f;
    [SerializeField] private float m_MaxPlayerDistance = 80f;
    [SerializeField] private float m_DespawnDistance = 100f;

    [Header("Spawn Blocking")]
    [SerializeField] private bool m_CanBeBlocked = true;
    [SerializeField] private float m_BlockCheckRadius = 10f;
    [SerializeField] private string m_BlockingTag = "SpawnBlocker";

    private List<ZombieAI> m_SpawnedZombies = new List<ZombieAI>();
    private float m_RespawnTimer;
    private bool m_HasSpawnedInitial = false;
    private Transform m_PlayerTransform;

    private void Start()
    {
        // Find player
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            m_PlayerTransform = player.transform;

        StartCoroutine(InitialSpawnRoutine());
    }

    private void Update()
    {
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

        // Don't spawn if player too close or too far
        if (distanceToPlayer < m_MinPlayerDistance || distanceToPlayer > m_MaxPlayerDistance)
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

    private IEnumerator InitialSpawnRoutine()
    {
        yield return new WaitForSeconds(m_InitialSpawnDelay);

        // Wait until player is in range
        while (m_PlayerTransform == null)
        {
            yield return new WaitForSeconds(1f);
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                m_PlayerTransform = player.transform;
        }

        // Wait for player to be in range but not too close
        while (true)
        {
            float dist = Vector3.Distance(transform.position, m_PlayerTransform.position);
            if (dist >= m_MinPlayerDistance && dist <= m_MaxPlayerDistance)
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

            // Find valid NavMesh position
            if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                // Check if not too close to player
                if (m_PlayerTransform != null)
                {
                    if (Vector3.Distance(hit.position, m_PlayerTransform.position) < m_MinPlayerDistance)
                        continue;
                }

                return hit.position;
            }
        }

        Debug.LogWarning($"[ZombieSpawner] Could not find valid spawn position at {name}");
        return Vector3.zero;
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
