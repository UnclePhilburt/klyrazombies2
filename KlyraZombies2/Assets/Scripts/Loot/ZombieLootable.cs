using UnityEngine;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;

/// <summary>
/// Component added to dead zombies to make them lootable.
/// Uses a prefab with pre-configured Inventory to avoid dynamic setup issues.
/// </summary>
public class ZombieLootable : MonoBehaviour
{
    [Header("Loot Settings")]
    [Tooltip("The loot table to use for zombie corpses")]
    [SerializeField] private LootTable m_LootTable;

    [Tooltip("If no loot table is assigned, try to load this from Resources")]
    [SerializeField] private string m_DefaultLootTablePath = "LootTables/ZombieLootTable";

    [Tooltip("Prefab with pre-configured Inventory/LootableContainer")]
    [SerializeField] private GameObject m_LootContainerPrefab;

    private LootableContainer m_Container;
    private InteractionHighlight m_Highlight;
    private GameObject m_SpawnedContainer;
    private bool m_Initialized = false;

    /// <summary>
    /// Display name for this lootable object
    /// </summary>
    public string DisplayName => "Zombie Corpse";

    /// <summary>
    /// Get the LootableContainer (for interaction system)
    /// </summary>
    public LootableContainer Container => m_Container;

    /// <summary>
    /// Initialize the zombie corpse for looting
    /// </summary>
    public void Initialize(LootTable lootTable = null)
    {
        if (m_Initialized) return;
        m_Initialized = true;

        Debug.Log($"[ZombieLootable] Initializing {gameObject.name}");

        if (lootTable != null)
        {
            m_LootTable = lootTable;
        }

        // Load default loot table if none assigned
        if (m_LootTable == null)
        {
            m_LootTable = Resources.Load<LootTable>(m_DefaultLootTablePath);

            if (m_LootTable == null)
            {
                m_LootTable = Resources.Load<LootTable>("ZombieLootTable");
            }
        }

        if (m_LootTable == null)
        {
            Debug.LogWarning($"[ZombieLootable] {gameObject.name} has no loot table!");
            return;
        }

        // Setup collider for interaction
        SetupCollider();

        // Add InteractionHighlight immediately so icon can show
        m_Highlight = GetComponent<InteractionHighlight>();
        if (m_Highlight == null)
        {
            m_Highlight = gameObject.AddComponent<InteractionHighlight>();
            Debug.Log($"[ZombieLootable] Added InteractionHighlight to {gameObject.name}");
        }

        // Setup the loot container
        StartCoroutine(SetupLootContainer());
    }

    private void SetupCollider()
    {
        var collider = GetComponent<Collider>();

        if (collider == null)
        {
            // Add a capsule collider around the corpse
            var capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.height = 1f;
            capsule.radius = 0.5f;
            capsule.center = new Vector3(0, 0.5f, 0);
            capsule.isTrigger = true;
        }
        else
        {
            collider.enabled = true;
            collider.isTrigger = true;
        }
    }

    private System.Collections.IEnumerator SetupLootContainer()
    {
        // Wait a frame for things to settle
        yield return null;

        Debug.Log($"[ZombieLootable] {gameObject.name} - Starting loot setup with table: {(m_LootTable != null ? m_LootTable.name : "NULL")}");

        // Try to load the loot container prefab from Resources
        if (m_LootContainerPrefab == null)
        {
            m_LootContainerPrefab = Resources.Load<GameObject>("Prefabs/ZombieLootContainer");
            Debug.Log($"[ZombieLootable] Loaded prefab from Resources: {(m_LootContainerPrefab != null ? "SUCCESS" : "FAILED")}");
        }

        if (m_LootContainerPrefab != null)
        {
            // Instantiate the pre-configured container as a child
            m_SpawnedContainer = Instantiate(m_LootContainerPrefab, transform);
            m_SpawnedContainer.transform.localPosition = Vector3.zero;
            m_SpawnedContainer.transform.localRotation = Quaternion.identity;
            m_SpawnedContainer.name = "LootContainer";

            // Get the LootableContainer from the prefab
            m_Container = m_SpawnedContainer.GetComponent<LootableContainer>();

            if (m_Container != null)
            {
                m_Container.lootTable = m_LootTable;
                m_Container.populateOnStart = false;
                m_Container.debugLog = true;

                // Wait for the prefab's inventory to initialize
                yield return new WaitForSeconds(0.3f);

                // Populate the loot
                m_Container.PopulateLoot();
                Debug.Log($"[ZombieLootable] {gameObject.name} - Loot populated");
            }
            else
            {
                Debug.LogError($"[ZombieLootable] Prefab missing LootableContainer component!");
            }
        }
        else
        {
            // No prefab found - log error but highlight is already added
            Debug.LogError($"[ZombieLootable] No prefab found at Resources/Prefabs/ZombieLootContainer. " +
                "Run 'Tools > Create Zombie Loot Container Prefab' in Unity Editor.");
        }
    }

    private System.Collections.IEnumerator CreateDynamicContainer()
    {
        // Dynamic creation doesn't work well with Opsive's Inventory system
        // because ItemCollection needs to be configured before Inventory initializes.
        // Log an error and tell the user to create the prefab.
        Debug.LogError("[ZombieLootable] Cannot create loot container dynamically. " +
            "Please run 'Tools > Create Zombie Loot Container Prefab' in Unity Editor " +
            "and set the ItemCollection's Purpose to 'Main'.");
        yield break;
    }

    /// <summary>
    /// Static method to make an existing zombie lootable after death
    /// </summary>
    public static ZombieLootable MakeZombieLootable(GameObject zombie, LootTable lootTable = null)
    {
        var lootable = zombie.GetComponent<ZombieLootable>();
        if (lootable == null)
        {
            lootable = zombie.AddComponent<ZombieLootable>();
        }

        lootable.Initialize(lootTable);
        return lootable;
    }

    private void OnDestroy()
    {
        if (m_SpawnedContainer != null)
        {
            Destroy(m_SpawnedContainer);
        }
    }
}
