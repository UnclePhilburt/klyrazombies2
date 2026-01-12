using UnityEngine;

/// <summary>
/// Handles E key interaction for zombie corpses.
/// Uses InteractionHighlight's crosshair targeting to determine which zombie to loot.
/// </summary>
public class LootableInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("Key to interact with lootables")]
    [SerializeField] private KeyCode m_InteractKey = KeyCode.E;

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
        // Use the crosshair target from InteractionHighlight instead of distance-based search
        var crosshairTarget = InteractionHighlight.CurrentTarget;

        ZombieLootable targetZombie = null;

        if (crosshairTarget != null)
        {
            // Check if the crosshair target has a ZombieLootable
            targetZombie = crosshairTarget.GetComponent<ZombieLootable>();
            if (targetZombie == null)
            {
                targetZombie = crosshairTarget.GetComponentInParent<ZombieLootable>();
            }
            if (targetZombie == null)
            {
                targetZombie = crosshairTarget.transform.root.GetComponentInChildren<ZombieLootable>();
            }
        }

        // Update target
        if (targetZombie != m_CurrentTarget)
        {
            m_CurrentTarget = targetZombie;
            m_CurrentHighlight = crosshairTarget;
        }
    }

    private void ClearTarget()
    {
        // InteractionHighlight manages its own visibility based on crosshair targeting
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

}
