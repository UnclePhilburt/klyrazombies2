using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Spawns zombies for the main menu background scene.
/// Zombies will just wander around since there's no player to chase.
/// </summary>
public class MenuZombieSpawner : MonoBehaviour
{
    [Header("Zombie Prefabs")]
    [Tooltip("Zombie prefabs to spawn randomly from")]
    [SerializeField] private GameObject[] m_ZombiePrefabs;

    [Header("Spawn Settings")]
    [SerializeField] private int m_ZombieCount = 5;
    [SerializeField] private float m_SpawnRadius = 15f;
    [SerializeField] private float m_SpawnHeight = 0f;

    [Header("Wander Area")]
    [Tooltip("If set, zombies will wander within this area. Otherwise uses spawn radius.")]
    [SerializeField] private Transform m_WanderAreaCenter;
    [SerializeField] private float m_WanderRadius = 20f;

    [Header("Zombie Settings")]
    [SerializeField] private bool m_DisablePlayerDetection = true;
    [SerializeField] private float m_WanderSpeed = 1f;

    [Header("Camera (Optional)")]
    [Tooltip("Camera to position looking at zombies")]
    [SerializeField] private Camera m_MenuCamera;
    [SerializeField] private Vector3 m_CameraOffset = new Vector3(0, 5, -10);
    [SerializeField] private bool m_AutoPositionCamera = false;

    [Header("Debug")]
    [SerializeField] private bool m_SpawnOnStart = true;
    [SerializeField] private bool m_ShowGizmos = true;

    private List<GameObject> m_SpawnedZombies = new List<GameObject>();

    private void Start()
    {
        if (m_SpawnOnStart)
        {
            SpawnZombies();
        }

        if (m_AutoPositionCamera && m_MenuCamera != null)
        {
            PositionCamera();
        }
    }

    /// <summary>
    /// Spawn all zombies for the menu.
    /// </summary>
    public void SpawnZombies()
    {
        if (m_ZombiePrefabs == null || m_ZombiePrefabs.Length == 0)
        {
            Debug.LogWarning("[MenuZombieSpawner] No zombie prefabs assigned!");
            return;
        }

        ClearZombies();

        Vector3 center = m_WanderAreaCenter != null ? m_WanderAreaCenter.position : transform.position;

        for (int i = 0; i < m_ZombieCount; i++)
        {
            SpawnZombie(center);
        }

        Debug.Log($"[MenuZombieSpawner] Spawned {m_SpawnedZombies.Count} zombies");
    }

    private void SpawnZombie(Vector3 center)
    {
        // Pick random prefab
        GameObject prefab = m_ZombiePrefabs[Random.Range(0, m_ZombiePrefabs.Length)];
        if (prefab == null) return;

        // Find spawn position on NavMesh
        Vector3 randomPos = center + Random.insideUnitSphere * m_SpawnRadius;
        randomPos.y = center.y + m_SpawnHeight;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPos, out hit, m_SpawnRadius, NavMesh.AllAreas))
        {
            // Spawn zombie
            GameObject zombie = Instantiate(prefab, hit.position, Quaternion.Euler(0, Random.Range(0, 360), 0));
            zombie.name = $"MenuZombie_{m_SpawnedZombies.Count}";

            // Configure for menu (no player detection)
            ConfigureZombieForMenu(zombie);

            m_SpawnedZombies.Add(zombie);
        }
        else
        {
            Debug.LogWarning($"[MenuZombieSpawner] Could not find NavMesh position near {randomPos}");
        }
    }

    private void ConfigureZombieForMenu(GameObject zombie)
    {
        // Get ZombieAI component
        var zombieAI = zombie.GetComponent<ZombieAI>();
        if (zombieAI != null)
        {
            // Set wander area
            Vector3 wanderCenter = m_WanderAreaCenter != null ? m_WanderAreaCenter.position : transform.position;

            // Use reflection to set private fields for menu mode
            var type = typeof(ZombieAI);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            // Disable player detection for menu
            if (m_DisablePlayerDetection)
            {
                var sightRangeField = type.GetField("m_SightRange", flags);
                if (sightRangeField != null)
                    sightRangeField.SetValue(zombieAI, 0f);

                var hearingRangeField = type.GetField("m_HearingRange", flags);
                if (hearingRangeField != null)
                    hearingRangeField.SetValue(zombieAI, 0f);
            }

            // Set wander radius
            var wanderRadiusField = type.GetField("m_WanderRadius", flags);
            if (wanderRadiusField != null)
                wanderRadiusField.SetValue(zombieAI, m_WanderRadius);

            // Set walk speed
            var walkSpeedField = type.GetField("m_WalkSpeed", flags);
            if (walkSpeedField != null)
                walkSpeedField.SetValue(zombieAI, m_WanderSpeed);
        }

        // Disable health bar for cleaner visuals
        var zombieHealth = zombie.GetComponent<ZombieHealth>();
        if (zombieHealth != null)
        {
            var type = typeof(ZombieHealth);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var showHealthBarField = type.GetField("m_ShowFloatingHealthBar", flags);
            if (showHealthBarField != null)
                showHealthBarField.SetValue(zombieHealth, false);
        }

        // Make sure NavMeshAgent is enabled
        var agent = zombie.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = m_WanderSpeed;
        }
    }

    /// <summary>
    /// Clear all spawned zombies.
    /// </summary>
    public void ClearZombies()
    {
        foreach (var zombie in m_SpawnedZombies)
        {
            if (zombie != null)
                Destroy(zombie);
        }
        m_SpawnedZombies.Clear();
    }

    /// <summary>
    /// Position the camera to look at the zombie area.
    /// </summary>
    public void PositionCamera()
    {
        if (m_MenuCamera == null) return;

        Vector3 center = m_WanderAreaCenter != null ? m_WanderAreaCenter.position : transform.position;
        m_MenuCamera.transform.position = center + m_CameraOffset;
        m_MenuCamera.transform.LookAt(center);
    }

    private void OnDestroy()
    {
        ClearZombies();
    }

    private void OnDrawGizmosSelected()
    {
        if (!m_ShowGizmos) return;

        Vector3 center = m_WanderAreaCenter != null ? m_WanderAreaCenter.position : transform.position;

        // Spawn area (green)
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(center, m_SpawnRadius);

        // Wander area (blue)
        Gizmos.color = new Color(0, 0.5f, 1, 0.3f);
        Gizmos.DrawWireSphere(center, m_WanderRadius);

        // Camera position (yellow)
        if (m_AutoPositionCamera)
        {
            Gizmos.color = Color.yellow;
            Vector3 camPos = center + m_CameraOffset;
            Gizmos.DrawWireSphere(camPos, 0.5f);
            Gizmos.DrawLine(camPos, center);
        }
    }
}
