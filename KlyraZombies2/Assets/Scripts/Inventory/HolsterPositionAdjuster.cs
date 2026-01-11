using UnityEngine;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;
using Opsive.UltimateCharacterController.Objects;

/// <summary>
/// Moves the RifleHolster (ObjectIdentifier ID 1002) to different positions based on equipped backpack.
/// When Opsive holsters the weapon, it parents to this ObjectIdentifier, so moving it moves the weapon.
/// </summary>
public class HolsterPositionAdjuster : MonoBehaviour
{
    public enum BackpackSize { None, Small, Medium, Large }

    [Header("References")]
    [SerializeField] private Inventory m_Inventory;

    [Header("Holster ID")]
    [Tooltip("ObjectIdentifier ID for the rifle holster (matches weapon's HolsterTarget ID)")]
    [SerializeField] private uint m_RifleHolsterID = 1002;

    [Header("Holster Spots (positions the holster moves to)")]
    [SerializeField] private Transform m_DefaultHolsterSpot;
    [SerializeField] private Transform m_SmallBackpackHolsterSpot;
    [SerializeField] private Transform m_MediumBackpackHolsterSpot;
    [SerializeField] private Transform m_LargeBackpackHolsterSpot;

    [Header("Settings")]
    [SerializeField] private bool m_DebugLog = true;

    [Header("Runtime (Read Only)")]
    [SerializeField] private BackpackSize m_CurrentBackpack = BackpackSize.None;
    [SerializeField] private Transform m_RifleHolster;

    private bool m_Initialized;

    private void Start()
    {
        if (m_Inventory == null)
            m_Inventory = GetComponentInParent<Inventory>();

        if (m_Inventory == null)
            m_Inventory = GetComponent<Inventory>();

        Initialize();
    }

    private void LateUpdate()
    {
        if (!m_Initialized)
            Initialize();

        CheckBackpackState();
        UpdateHolsterPosition();
    }

    private void Initialize()
    {
        // Find the RifleHolster by ObjectIdentifier ID
        // IMPORTANT: We need the one on the OPSIVE skeleton, not the Sidekick character
        if (m_RifleHolster == null)
        {
            var identifiers = GetComponentsInChildren<ObjectIdentifier>(true);

            // First, try to find one that is NOT under a SidekickCharacter
            foreach (var id in identifiers)
            {
                if (id.ID == m_RifleHolsterID)
                {
                    // Check if this is under a SidekickCharacter (we don't want that one)
                    bool isUnderSidekick = false;
                    Transform parent = id.transform.parent;
                    while (parent != null)
                    {
                        if (parent.name.Contains("SidekickCharacter") || parent.name.Contains("Sidekick_"))
                        {
                            isUnderSidekick = true;
                            break;
                        }
                        parent = parent.parent;
                    }

                    if (!isUnderSidekick)
                    {
                        m_RifleHolster = id.transform;
                        if (m_DebugLog) Debug.Log($"[HolsterAdjuster] Found RifleHolster on Opsive skeleton: {id.name}");
                        break;
                    }
                    else if (m_DebugLog)
                    {
                        Debug.Log($"[HolsterAdjuster] Skipping RifleHolster on SidekickCharacter: {id.name}");
                    }
                }
            }

            // Fallback: if we didn't find one outside Sidekick, use any one we find
            if (m_RifleHolster == null)
            {
                foreach (var id in identifiers)
                {
                    if (id.ID == m_RifleHolsterID)
                    {
                        m_RifleHolster = id.transform;
                        if (m_DebugLog) Debug.Log($"[HolsterAdjuster] Fallback - Found RifleHolster: {id.name}");
                        break;
                    }
                }
            }
        }

        // Find holster spots by name
        FindHolsterSpots();

        m_Initialized = m_RifleHolster != null &&
                        m_DefaultHolsterSpot != null &&
                        m_SmallBackpackHolsterSpot != null &&
                        m_MediumBackpackHolsterSpot != null &&
                        m_LargeBackpackHolsterSpot != null;

        if (m_DebugLog)
        {
            Debug.Log($"[HolsterAdjuster] Initialized: {m_Initialized}");
            Debug.Log($"  RifleHolster: {(m_RifleHolster != null ? m_RifleHolster.name : "NULL")}");
            Debug.Log($"  DefaultSpot: {(m_DefaultHolsterSpot != null ? m_DefaultHolsterSpot.name : "NULL")}");
            Debug.Log($"  SmallSpot: {(m_SmallBackpackHolsterSpot != null ? m_SmallBackpackHolsterSpot.name : "NULL")}");
            Debug.Log($"  MediumSpot: {(m_MediumBackpackHolsterSpot != null ? m_MediumBackpackHolsterSpot.name : "NULL")}");
            Debug.Log($"  LargeSpot: {(m_LargeBackpackHolsterSpot != null ? m_LargeBackpackHolsterSpot.name : "NULL")}");
            Debug.Log($"  Inventory: {(m_Inventory != null ? m_Inventory.name : "NULL")}");
        }
    }

    private void FindHolsterSpots()
    {
        var allTransforms = GetComponentsInChildren<Transform>(true);

        foreach (var t in allTransforms)
        {
            string name = t.name.ToLower();

            // Skip the RifleHolster itself
            if (t == m_RifleHolster) continue;

            // Skip anything under SidekickCharacter - we need spots on the Opsive skeleton
            bool isUnderSidekick = false;
            Transform parent = t.parent;
            while (parent != null)
            {
                if (parent.name.Contains("SidekickCharacter") || parent.name.Contains("Sidekick_"))
                {
                    isUnderSidekick = true;
                    break;
                }
                parent = parent.parent;
            }
            if (isUnderSidekick) continue;

            if (m_DefaultHolsterSpot == null && name.Contains("default") && name.Contains("holster"))
            {
                m_DefaultHolsterSpot = t;
                if (m_DebugLog) Debug.Log($"[HolsterAdjuster] Found DefaultHolsterSpot: {t.name}");
            }
            else if (m_SmallBackpackHolsterSpot == null && name.Contains("small") && name.Contains("backpack") && name.Contains("holster"))
            {
                m_SmallBackpackHolsterSpot = t;
                if (m_DebugLog) Debug.Log($"[HolsterAdjuster] Found SmallBackpackHolsterSpot: {t.name}");
            }
            else if (m_MediumBackpackHolsterSpot == null && name.Contains("medium") && name.Contains("backpack") && name.Contains("holster"))
            {
                m_MediumBackpackHolsterSpot = t;
                if (m_DebugLog) Debug.Log($"[HolsterAdjuster] Found MediumBackpackHolsterSpot: {t.name}");
            }
            else if (m_LargeBackpackHolsterSpot == null && name.Contains("large") && name.Contains("backpack") && name.Contains("holster"))
            {
                m_LargeBackpackHolsterSpot = t;
                if (m_DebugLog) Debug.Log($"[HolsterAdjuster] Found LargeBackpackHolsterSpot: {t.name}");
            }
        }
    }

    private void CheckBackpackState()
    {
        if (m_Inventory == null) return;

        BackpackSize detectedSize = BackpackSize.None;

        // Check all collections for backpack items
        var collections = m_Inventory.ItemCollectionsReadOnly;
        if (collections == null) return;

        foreach (var collection in collections)
        {
            var stacks = collection.GetAllItemStacks();
            if (stacks == null) continue;

            foreach (var stack in stacks)
            {
                if (stack?.Item == null) continue;

                string itemName = stack.Item.name;
                string categoryName = stack.Item.Category?.name ?? "";

                // Check if it's a backpack
                bool isBackpack = categoryName.Contains("Backpack") || categoryName.Contains("Bag") ||
                                  itemName.ToLower().Contains("backpack") || itemName.ToLower().Contains("bag");

                if (!isBackpack) continue;

                // Determine size
                string lowerName = itemName.ToLower();
                if (lowerName.Contains("small"))
                    detectedSize = BackpackSize.Small;
                else if (lowerName.Contains("large"))
                    detectedSize = BackpackSize.Large;
                else
                    detectedSize = BackpackSize.Medium;

                if (m_DebugLog && detectedSize != m_CurrentBackpack)
                {
                    Debug.Log($"[HolsterAdjuster] Found backpack: {itemName} in {collection.Name} -> Size: {detectedSize}");
                }

                break;
            }

            if (detectedSize != BackpackSize.None) break;
        }

        if (detectedSize != m_CurrentBackpack)
        {
            m_CurrentBackpack = detectedSize;
            if (m_DebugLog) Debug.Log($"[HolsterAdjuster] Backpack size changed to: {m_CurrentBackpack}");
        }
    }

    private void UpdateHolsterPosition()
    {
        if (m_RifleHolster == null) return;

        // Get target spot based on backpack
        Transform targetSpot = m_CurrentBackpack switch
        {
            BackpackSize.Small => m_SmallBackpackHolsterSpot,
            BackpackSize.Medium => m_MediumBackpackHolsterSpot,
            BackpackSize.Large => m_LargeBackpackHolsterSpot,
            _ => m_DefaultHolsterSpot
        };

        if (targetSpot == null) return;

        // Already at correct position?
        if (m_RifleHolster.parent == targetSpot) return;

        // Move the RifleHolster to the target spot
        if (m_DebugLog)
        {
            Debug.Log($"[HolsterAdjuster] Moving RifleHolster from {m_RifleHolster.parent?.name} to {targetSpot.name}");
            Debug.Log($"[HolsterAdjuster] RifleHolster object: {m_RifleHolster.gameObject.name} (instance ID: {m_RifleHolster.GetInstanceID()})");
            Debug.Log($"[HolsterAdjuster] Target spot: {targetSpot.gameObject.name} (instance ID: {targetSpot.GetInstanceID()})");
        }

        // Store the current local position/rotation before reparenting
        Vector3 savedLocalPos = m_RifleHolster.localPosition;
        Quaternion savedLocalRot = m_RifleHolster.localRotation;

        m_RifleHolster.SetParent(targetSpot);

        // Restore manual position/rotation (don't zero them out)
        m_RifleHolster.localPosition = savedLocalPos;
        m_RifleHolster.localRotation = savedLocalRot;

        // Verify it worked
        if (m_DebugLog)
        {
            Debug.Log($"[HolsterAdjuster] AFTER SetParent - RifleHolster parent is now: {m_RifleHolster.parent?.name}");
        }
    }
}
