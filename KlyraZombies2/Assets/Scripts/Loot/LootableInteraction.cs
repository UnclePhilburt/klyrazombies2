using UnityEngine;

/// <summary>
/// Shows the search icon above the closest lootable zombie corpse.
/// Press F to loot.
/// </summary>
public class LootableInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("Maximum distance to detect lootables")]
    [SerializeField] private float m_InteractionRange = 5f;

    [Tooltip("Key to interact with lootables")]
    [SerializeField] private KeyCode m_InteractKey = KeyCode.F;

    // State
    private ZombieLootable m_CurrentTarget;
    private InteractionHighlight m_CurrentHighlight;

    private void Start()
    {
        Debug.Log("[LootableInteraction] Started on " + gameObject.name);
    }

    private void Update()
    {
        // Don't check if loot UI is open
        if (SimpleLootUI.Instance != null && SimpleLootUI.Instance.IsOpen)
        {
            ClearTarget();
            return;
        }

        // Find closest lootable zombie
        FindClosestZombie();

        // Handle interaction input
        if (Input.GetKeyDown(m_InteractKey) && m_CurrentTarget != null)
        {
            TryLoot();
        }
    }

    private void FindClosestZombie()
    {
        ZombieLootable closestZombie = null;
        float closestDist = m_InteractionRange;

        // Find all ZombieLootable components in range
        var zombies = FindObjectsByType<ZombieLootable>(FindObjectsSortMode.None);

        foreach (var zombie in zombies)
        {
            if (zombie == null) continue;

            float dist = Vector3.Distance(transform.position, zombie.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestZombie = zombie;
            }
        }

        // Update target
        if (closestZombie != m_CurrentTarget)
        {
            // Hide previous highlight
            if (m_CurrentHighlight != null)
            {
                m_CurrentHighlight.ForceShowIcon(false);
                m_CurrentHighlight.SetHighlightActive(false);
            }

            m_CurrentTarget = closestZombie;

            // Show new highlight
            if (m_CurrentTarget != null)
            {
                Debug.Log($"[LootableInteraction] Found zombie: {m_CurrentTarget.gameObject.name} at distance {closestDist:F1}m");

                m_CurrentHighlight = m_CurrentTarget.GetComponent<InteractionHighlight>();
                if (m_CurrentHighlight == null)
                {
                    // Try to get from children (in case it's on a child object)
                    m_CurrentHighlight = m_CurrentTarget.GetComponentInChildren<InteractionHighlight>();
                }

                if (m_CurrentHighlight != null)
                {
                    Debug.Log($"[LootableInteraction] Found InteractionHighlight on {m_CurrentHighlight.gameObject.name}, calling ForceShowIcon(true)");
                    m_CurrentHighlight.SetHighlightActive(true);
                    m_CurrentHighlight.ForceShowIcon(true);
                }
                else
                {
                    Debug.LogWarning($"[LootableInteraction] {m_CurrentTarget.gameObject.name} has no InteractionHighlight! Adding one now...");
                    m_CurrentHighlight = m_CurrentTarget.gameObject.AddComponent<InteractionHighlight>();
                    m_CurrentHighlight.SetHighlightActive(true);
                    m_CurrentHighlight.ForceShowIcon(true);
                }
            }
            else
            {
                m_CurrentHighlight = null;
            }
        }
    }

    private void ClearTarget()
    {
        if (m_CurrentHighlight != null)
        {
            m_CurrentHighlight.ForceShowIcon(false);
            m_CurrentHighlight.SetHighlightActive(false);
        }
        m_CurrentTarget = null;
        m_CurrentHighlight = null;
    }

    private void TryLoot()
    {
        Debug.Log($"[LootableInteraction] TryLoot called. Target: {(m_CurrentTarget != null ? m_CurrentTarget.gameObject.name : "NULL")}");

        if (m_CurrentTarget == null)
        {
            Debug.LogWarning("[LootableInteraction] No current target!");
            return;
        }

        if (m_CurrentTarget.Container == null)
        {
            Debug.LogWarning($"[LootableInteraction] {m_CurrentTarget.gameObject.name} has no Container yet! (loot setup may still be in progress)");
            return;
        }

        // Check if container has inventory with items
        var containerInv = m_CurrentTarget.Container.GetComponent<Opsive.UltimateInventorySystem.Core.InventoryCollections.Inventory>();
        if (containerInv != null)
        {
            var mainCollection = containerInv.MainItemCollection;
            int itemCount = mainCollection?.GetAllItemStacks()?.Count ?? 0;
            Debug.Log($"[LootableInteraction] Container has {itemCount} item stacks in MainItemCollection");
        }

        var lootUI = SimpleLootUI.Instance;
        if (lootUI == null)
            lootUI = FindFirstObjectByType<SimpleLootUI>();

        if (lootUI == null)
        {
            Debug.LogError("[LootableInteraction] No SimpleLootUI found in scene!");
            return;
        }

        if (m_CurrentHighlight != null)
        {
            m_CurrentHighlight.MarkAsOpened();
        }

        Debug.Log($"[LootableInteraction] Opening loot UI for {m_CurrentTarget.gameObject.name}, Container: {m_CurrentTarget.Container.gameObject.name}");
        lootUI.Open(m_CurrentTarget.Container);
        Debug.Log($"[LootableInteraction] lootUI.Open() called, IsOpen: {lootUI.IsOpen}");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, m_InteractionRange);
    }
}
