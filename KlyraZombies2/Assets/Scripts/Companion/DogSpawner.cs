using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Spawns a random dog companion when the player spawns.
/// Randomly selects from German Shepherd, Doberman, or Ridgeback (with collars).
/// </summary>
public class DogSpawner : MonoBehaviour
{
    [Header("Dog Prefabs")]
    [Tooltip("Dog prefabs to randomly choose from. If empty, will try to load from PolygonDog folder.")]
    [SerializeField] private GameObject[] m_DogPrefabs;

    [Header("Spawn Settings")]
    [SerializeField] private float m_SpawnDistance = 2f;
    [SerializeField] private float m_SpawnDelay = 1f;

    [Header("Default Prefab Paths (if no prefabs assigned)")]
    [SerializeField] private string[] m_DefaultPrefabPaths = new string[]
    {
        "Assets/PolygonDog/Prefabs/Dogs/Unity_SK_Animals_Dog_GermanShepherd_Collar_01.prefab",
        "Assets/PolygonDog/Prefabs/Dogs/Unity_SK_Animals_Dog_Doberman_Collar_01.prefab",
        "Assets/PolygonDog/Prefabs/Dogs/Unity_SK_Animals_Dog_Ridgeback_Collar_01.prefab"
    };

    [Header("Debug")]
    [SerializeField] private bool m_SpawnOnStart = true;
    [SerializeField] private int m_DebugDogIndex = -1; // -1 = random

    private GameObject m_SpawnedDog;
    private static readonly string[] BreedNames = { "German Shepherd", "Doberman", "Ridgeback" };

    // Singleton
    public static DogSpawner Instance { get; private set; }

    public GameObject SpawnedDog => m_SpawnedDog;

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
        if (m_SpawnOnStart)
        {
            Invoke(nameof(SpawnDog), m_SpawnDelay);
        }
    }

    /// <summary>
    /// Spawn a random dog near the player
    /// </summary>
    public GameObject SpawnDog()
    {
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("[DogSpawner] No player found, retrying in 1 second...");
            Invoke(nameof(SpawnDog), 1f);
            return null;
        }

        // Destroy existing dog if any
        if (m_SpawnedDog != null)
        {
            Destroy(m_SpawnedDog);
        }

        // Select dog prefab
        GameObject prefab = SelectDogPrefab();
        if (prefab == null)
        {
            Debug.LogError("[DogSpawner] No dog prefab available!");
            return null;
        }

        // Calculate spawn position (behind player)
        Vector3 spawnPos = player.transform.position - player.transform.forward * m_SpawnDistance;

        // Sample NavMesh to ensure valid position
        if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            spawnPos = hit.position;
        }

        // Spawn the dog
        m_SpawnedDog = Instantiate(prefab, spawnPos, player.transform.rotation);
        m_SpawnedDog.name = "DogCompanion";

        // Ensure it has required components
        SetupDogComponents(m_SpawnedDog, player.transform);

        Debug.Log($"[DogSpawner] Spawned dog: {prefab.name} at {spawnPos}");

        return m_SpawnedDog;
    }

    private GameObject SelectDogPrefab()
    {
        // Use assigned prefabs if available
        if (m_DogPrefabs != null && m_DogPrefabs.Length > 0)
        {
            int index = m_DebugDogIndex >= 0
                ? Mathf.Clamp(m_DebugDogIndex, 0, m_DogPrefabs.Length - 1)
                : Random.Range(0, m_DogPrefabs.Length);

            if (m_DogPrefabs[index] != null)
            {
                Debug.Log($"[DogSpawner] Selected breed: {(index < BreedNames.Length ? BreedNames[index] : "Unknown")}");
                return m_DogPrefabs[index];
            }
        }

        // Try to load from Resources
        Debug.LogWarning("[DogSpawner] No prefabs assigned, trying to load from default paths...");

#if UNITY_EDITOR
        // In editor, try to load from asset path
        int randomIndex = m_DebugDogIndex >= 0
            ? Mathf.Clamp(m_DebugDogIndex, 0, m_DefaultPrefabPaths.Length - 1)
            : Random.Range(0, m_DefaultPrefabPaths.Length);

        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(m_DefaultPrefabPaths[randomIndex]);
        if (prefab != null)
        {
            Debug.Log($"[DogSpawner] Loaded from AssetDatabase: {BreedNames[randomIndex]}");
            return prefab;
        }
#endif

        Debug.LogError("[DogSpawner] Could not load any dog prefab!");
        return null;
    }

    private void SetupDogComponents(GameObject dog, Transform owner)
    {
        // Add NavMeshAgent if not present
        NavMeshAgent agent = dog.GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = dog.AddComponent<NavMeshAgent>();
            agent.speed = 4f;
            agent.angularSpeed = 360f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 2f;
            agent.radius = 0.3f;
            agent.height = 0.8f;
            agent.baseOffset = 0f;
        }

        // Add DogCompanion if not present
        DogCompanion companion = dog.GetComponent<DogCompanion>();
        if (companion == null)
        {
            companion = dog.AddComponent<DogCompanion>();
        }
        companion.SetOwner(owner);

        // Add AudioSource if not present
        AudioSource audioSource = dog.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = dog.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.maxDistance = 20f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
        }

        // Ensure the dog has the Animator component properly referenced
        Animator animator = dog.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            // The animator should already have the dog controller assigned in the prefab
            Debug.Log($"[DogSpawner] Animator found: {animator.runtimeAnimatorController?.name ?? "No controller"}");

            // IMPORTANT: Explicitly set the animator reference on DogCompanion using reflection
            // because m_Animator is a private serialized field
            if (companion != null)
            {
                var animatorField = typeof(DogCompanion).GetField("m_Animator",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (animatorField != null)
                {
                    animatorField.SetValue(companion, animator);
                    Debug.Log($"[DogSpawner] Set animator reference on DogCompanion");
                }
            }
        }
        else
        {
            Debug.LogWarning("[DogSpawner] No Animator found on dog! Animations will not work.");
        }

        // Add a collider for interactions if needed
        Collider col = dog.GetComponent<Collider>();
        if (col == null)
        {
            CapsuleCollider capsule = dog.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0, 0.4f, 0);
            capsule.radius = 0.25f;
            capsule.height = 0.8f;
            capsule.direction = 2; // Z-axis (forward)
        }

        // Set layer (optional - for physics filtering)
        // dog.layer = LayerMask.NameToLayer("Companion");
    }

    /// <summary>
    /// Despawn the current dog
    /// </summary>
    public void DespawnDog()
    {
        if (m_SpawnedDog != null)
        {
            Destroy(m_SpawnedDog);
            m_SpawnedDog = null;
            Debug.Log("[DogSpawner] Dog despawned");
        }
    }

    /// <summary>
    /// Respawn the dog (new random breed)
    /// </summary>
    public void RespawnDog()
    {
        DespawnDog();
        SpawnDog();
    }
}
