using UnityEngine;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;
using Opsive.UltimateCharacterController.Objects;

/// <summary>
/// Manages rifle holster position based on equipped backpack.
/// Moves the SINGLE holster spot (with ObjectIdentifier ID 1002) to different positions.
/// Auto-finds holsters at runtime since they're created dynamically.
/// </summary>
public class RifleHolsterManager : MonoBehaviour
{
    public enum BackpackSize { None, Small, Medium, Large }

    [Header("Auto-Find Settings")]
    [Tooltip("The ObjectIdentifier ID for the rifle holster (usually 1002)")]
    public uint RifleHolsterID = 1002;

    [Header("Settings")]
    [Tooltip("Enable debug logging")]
    public bool DebugLog = true;

    [Header("Runtime Info (Read Only)")]
    [SerializeField] private BackpackSize m_CurrentBackpack = BackpackSize.None;
    [SerializeField] private Transform m_RifleHolster;
    [SerializeField] private Transform m_DefaultPosition;
    [SerializeField] private Transform m_SmallBackpackPosition;
    [SerializeField] private Transform m_MediumBackpackPosition;
    [SerializeField] private Transform m_LargeBackpackPosition;

    private Inventory m_Inventory;
    private bool m_Initialized;

    private void Start()
    {
        m_Inventory = GetComponentInParent<Inventory>();
    }

    private void LateUpdate()
    {
        if (!m_Initialized)
        {
            FindHolsterReferences();
        }

        DetectBackpack();
        UpdateHolsterPosition();
    }

    private void FindHolsterReferences()
    {
        // Find the rifle holster by ObjectIdentifier ID
        if (m_RifleHolster == null)
        {
            var allIdentifiers = GetComponentsInChildren<ObjectIdentifier>(true);
            foreach (var id in allIdentifiers)
            {
                if (id.ID == RifleHolsterID)
                {
                    m_RifleHolster = id.transform;
                    if (DebugLog) Debug.Log($"[RifleHolsterManager] Found RifleHolster: {id.transform.name}");
                    break;
                }
            }
        }

        // Find position references by name
        if (m_DefaultPosition == null || m_SmallBackpackPosition == null ||
            m_MediumBackpackPosition == null || m_LargeBackpackPosition == null)
        {
            var allTransforms = GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                string name = t.name.ToLower();

                if (m_DefaultPosition == null && name.Contains("default") && name.Contains("holster"))
                {
                    m_DefaultPosition = t;
                    if (DebugLog) Debug.Log($"[RifleHolsterManager] Found DefaultPosition: {t.name}");
                }
                else if (m_SmallBackpackPosition == null && name.Contains("small") && name.Contains("backpack") && name.Contains("holster"))
                {
                    m_SmallBackpackPosition = t;
                    if (DebugLog) Debug.Log($"[RifleHolsterManager] Found SmallBackpackPosition: {t.name}");
                }
                else if (m_MediumBackpackPosition == null && name.Contains("medium") && name.Contains("backpack") && name.Contains("holster"))
                {
                    m_MediumBackpackPosition = t;
                    if (DebugLog) Debug.Log($"[RifleHolsterManager] Found MediumBackpackPosition: {t.name}");
                }
                else if (m_LargeBackpackPosition == null && name.Contains("large") && name.Contains("backpack") && name.Contains("holster"))
                {
                    m_LargeBackpackPosition = t;
                    if (DebugLog) Debug.Log($"[RifleHolsterManager] Found LargeBackpackPosition: {t.name}");
                }
            }
        }

        // Check if we have everything
        m_Initialized = m_RifleHolster != null && m_DefaultPosition != null &&
                        m_SmallBackpackPosition != null && m_MediumBackpackPosition != null &&
                        m_LargeBackpackPosition != null;

        if (m_Initialized && DebugLog)
        {
            Debug.Log("[RifleHolsterManager] All references found!");
        }
    }

    private bool m_HasLoggedInventoryContents = false;

    private void DetectBackpack()
    {
        if (m_Inventory == null)
        {
            // Try to find inventory again
            m_Inventory = GetComponentInParent<Inventory>();
            if (m_Inventory == null)
            {
                // Also try GetComponent on same object
                m_Inventory = GetComponent<Inventory>();
            }
            if (m_Inventory == null && DebugLog)
            {
                Debug.LogWarning("[RifleHolsterManager] No Inventory found! Check if this component is on the player.");
            }
            return;
        }

        // Log inventory contents once for debugging
        if (DebugLog && !m_HasLoggedInventoryContents)
        {
            LogAllInventoryContents();
            m_HasLoggedInventoryContents = true;
        }

        BackpackSize detected = BackpackSize.None;

        // Check multiple collection names - try all common ones
        string[] collections = { "Equippable", "Equipped", "Equipment", "Equippable Slots", "Default", "Backpack", "Bags" };

        foreach (var collectionName in collections)
        {
            var collection = m_Inventory.GetItemCollection(collectionName);
            if (collection == null) continue;

            var stacks = collection.GetAllItemStacks();
            if (stacks == null) continue;

            foreach (var stack in stacks)
            {
                if (stack?.Item == null) continue;

                string itemName = stack.Item.name;
                string categoryName = stack.Item.Category?.name ?? "NoCategory";

                // Get parent categories (UIS uses multiple parents)
                string parentCategoryNames = "NoParent";
                if (stack.Item.Category != null)
                {
                    var parents = stack.Item.Category.GetDirectParents();
                    if (parents != null && parents.Count > 0)
                    {
                        parentCategoryNames = string.Join(", ", System.Linq.Enumerable.Select(parents, p => p.name));
                    }
                }

                // Check if it's a backpack by:
                // 1. Category name contains "Backpack" or "Bag"
                // 2. Any parent category name contains "Backpack" or "Bag"
                // 3. Item name contains "backpack" or "bag"
                bool isBackpack = categoryName.Contains("Backpack") || categoryName.Contains("Bag") ||
                                  parentCategoryNames.Contains("Backpack") || parentCategoryNames.Contains("Bag") ||
                                  itemName.ToLower().Contains("backpack") || itemName.ToLower().Contains("bag");

                if (!isBackpack) continue;

                if (DebugLog && detected == BackpackSize.None)
                {
                    Debug.Log($"[RifleHolsterManager] FOUND BACKPACK: '{itemName}' (Category: {categoryName}, Parents: {parentCategoryNames}) in collection '{collectionName}'");
                }

                // Found a backpack - check size
                string lowerName = itemName.ToLower();
                if (lowerName.Contains("small"))
                    detected = BackpackSize.Small;
                else if (lowerName.Contains("large"))
                    detected = BackpackSize.Large;
                else
                    detected = BackpackSize.Medium;

                break;
            }

            if (detected != BackpackSize.None) break;
        }

        if (detected != m_CurrentBackpack)
        {
            m_CurrentBackpack = detected;
            if (DebugLog) Debug.Log($"[RifleHolsterManager] Backpack size changed to: {m_CurrentBackpack}");
        }
    }

    private void LogAllInventoryContents()
    {
        Debug.Log("[RifleHolsterManager] === INVENTORY CONTENTS DUMP ===");

        // List all available collections
        var allCollections = m_Inventory.ItemCollectionsReadOnly;
        if (allCollections != null)
        {
            Debug.Log($"[RifleHolsterManager] Found {allCollections.Count} collections:");
            foreach (var col in allCollections)
            {
                var stacks = col.GetAllItemStacks();
                int itemCount = stacks?.Count ?? 0;
                Debug.Log($"  - Collection '{col.Name}': {itemCount} items");

                if (stacks != null)
                {
                    foreach (var stack in stacks)
                    {
                        if (stack?.Item != null)
                        {
                            string catName = stack.Item.Category?.name ?? "NoCategory";

                            // Get parent categories
                            string parentCats = "NoParent";
                            if (stack.Item.Category != null)
                            {
                                var parents = stack.Item.Category.GetDirectParents();
                                if (parents != null && parents.Count > 0)
                                {
                                    parentCats = string.Join(", ", System.Linq.Enumerable.Select(parents, p => p.name));
                                }
                            }

                            Debug.Log($"      * '{stack.Item.name}' x{stack.Amount} (Cat: {catName}, Parents: {parentCats})");
                        }
                    }
                }
            }
        }
        else
        {
            Debug.Log("[RifleHolsterManager] No ItemCollections found!");
        }

        Debug.Log("[RifleHolsterManager] === END INVENTORY DUMP ===");
    }

    private void UpdateHolsterPosition()
    {
        if (m_RifleHolster == null) return;

        // Get target position based on backpack
        Transform targetPos = m_CurrentBackpack switch
        {
            BackpackSize.Small => m_SmallBackpackPosition,
            BackpackSize.Medium => m_MediumBackpackPosition,
            BackpackSize.Large => m_LargeBackpackPosition,
            _ => m_DefaultPosition
        };

        if (targetPos == null) return;

        // Move the holster to match the target position
        m_RifleHolster.localPosition = targetPos.localPosition;
        m_RifleHolster.localRotation = targetPos.localRotation;
    }
}
