using UnityEngine;
using Opsive.UltimateInventorySystem.Core;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;
using Opsive.UltimateInventorySystem.Core.DataStructures;

/// <summary>
/// Handles spawning and destroying the visual backpack model when equipped/unequipped.
/// Listens to an ItemCollectionGroup (equipment collection) for backpack category items.
/// </summary>
public class BackpackEquipHandler : MonoBehaviour
{
    [Tooltip("The inventory to monitor.")]
    [SerializeField] private Inventory m_Inventory;

    [Tooltip("Name of the item collection where equipment is stored.")]
    [SerializeField] private string m_EquipmentCollectionName = "Equippable";

    [Tooltip("The category for backpack items.")]
    [SerializeField] private DynamicItemCategory m_BackpackCategory = new DynamicItemCategory("Backpack");

    /// <summary>
    /// Gets the resolved ItemCategory from the DynamicItemCategory.
    /// </summary>
    public ItemCategory BackpackCategory => m_BackpackCategory;

    [Tooltip("The attribute name for the visual prefab.")]
    [SerializeField] private string m_PrefabAttributeName = "Prefab";

    [Header("Attachment Points (by size)")]
    [Tooltip("Attach point for Small Backpack.")]
    [SerializeField] private Transform m_SmallBackpackAttachPoint;

    [Tooltip("Attach point for Medium/Regular Backpack.")]
    [SerializeField] private Transform m_MediumBackpackAttachPoint;

    [Tooltip("Attach point for Large Backpack.")]
    [SerializeField] private Transform m_LargeBackpackAttachPoint;

    private ItemCollectionGroup m_EquipmentCollections;
    private GameObject m_SpawnedBackpack;
    private Item m_EquippedBackpackItem;
    private bool m_Initialized;

    private void Start()
    {
        if (m_Inventory == null)
        {
            m_Inventory = GetComponent<Inventory>();
        }

        Initialize(false);
    }

    /// <summary>
    /// Initialize the handler and start listening for equip/unequip events.
    /// </summary>
    public void Initialize(bool force)
    {
        if (m_Initialized && !force) return;
        if (m_Inventory == null)
        {
            Debug.LogWarning("[BackpackEquipHandler] No inventory assigned.", this);
            return;
        }

        if (m_EquipmentCollections == null)
        {
            m_EquipmentCollections = new ItemCollectionGroup();
            m_EquipmentCollections.OnItemAdded += HandleItemEquipped;
            m_EquipmentCollections.OnItemRemoved += HandleItemUnequipped;
        }

        m_EquipmentCollections.SetItemCollections(
            m_Inventory,
            new[] { m_EquipmentCollectionName },
            true, true
        );

        // Check if a backpack is already equipped (e.g., from save/load)
        CheckForExistingBackpack();

        m_Initialized = true;
    }

    /// <summary>
    /// Check if a backpack is already in the equipment collection.
    /// </summary>
    private void CheckForExistingBackpack()
    {
        if (m_EquipmentCollections == null) return;

        var itemInfos = m_EquipmentCollections.GetAllItemInfos();
        for (int i = 0; i < itemInfos.Count; i++)
        {
            var itemInfo = itemInfos[i];
            if (itemInfo.Item != null && IsBackpackItem(itemInfo.Item))
            {
                SpawnBackpackVisual(itemInfo.Item);
                break;
            }
        }
    }

    /// <summary>
    /// Called when an item is added to the equipment collection.
    /// </summary>
    private void HandleItemEquipped(ItemInfo originalItemInfo, ItemStack addedItemStack)
    {
        if (addedItemStack?.Item == null) return;
        if (!IsBackpackItem(addedItemStack.Item)) return;

        SpawnBackpackVisual(addedItemStack.Item);
    }

    /// <summary>
    /// Called when an item is removed from the equipment collection.
    /// </summary>
    private void HandleItemUnequipped(ItemInfo itemRemoved)
    {
        if (itemRemoved.Item == null) return;
        if (!IsBackpackItem(itemRemoved.Item)) return;

        DestroyBackpackVisual();
    }

    /// <summary>
    /// Check if an item belongs to the backpack category.
    /// </summary>
    private bool IsBackpackItem(Item item)
    {
        if (BackpackCategory == null) return false;
        return BackpackCategory.InherentlyContains(item);
    }

    /// <summary>
    /// Spawn the visual backpack prefab at the attach point.
    /// </summary>
    private void SpawnBackpackVisual(Item backpackItem)
    {
        // Destroy existing visual if any
        DestroyBackpackVisual();

        // Determine attach point based on backpack size
        Transform attachPoint = GetAttachPointForBackpack(backpackItem);

        if (attachPoint == null)
        {
            Debug.LogWarning($"[BackpackEquipHandler] No attach point assigned for backpack '{backpackItem.name}'.", this);
            return;
        }

        // Try to get the prefab from the item's Prefabs attribute
        GameObject prefab = null;

        // Try getting as a single GameObject
        if (backpackItem.TryGetAttributeValue<GameObject>(m_PrefabAttributeName, out var singlePrefab))
        {
            prefab = singlePrefab;
        }
        // Try getting as array (common in UIS)
        else if (backpackItem.TryGetAttributeValue<GameObject[]>(m_PrefabAttributeName, out var prefabArray))
        {
            if (prefabArray != null && prefabArray.Length > 0)
            {
                prefab = prefabArray[0];
            }
        }

        if (prefab == null)
        {
            Debug.LogWarning($"[BackpackEquipHandler] No prefab found for backpack '{backpackItem.name}'.", this);
            return;
        }

        m_SpawnedBackpack = Instantiate(prefab, attachPoint);
        m_SpawnedBackpack.transform.localPosition = Vector3.zero;
        m_SpawnedBackpack.transform.localRotation = Quaternion.identity;
        m_SpawnedBackpack.transform.localScale = Vector3.one;
        m_EquippedBackpackItem = backpackItem;

        Debug.Log($"[BackpackEquipHandler] Spawned {backpackItem.name} at {attachPoint.name}");
    }

    /// <summary>
    /// Get the correct attach point based on backpack size.
    /// </summary>
    private Transform GetAttachPointForBackpack(Item backpackItem)
    {
        string itemName = backpackItem.name.ToLower();

        if (itemName.Contains("small"))
            return m_SmallBackpackAttachPoint;
        else if (itemName.Contains("large"))
            return m_LargeBackpackAttachPoint;
        else
            return m_MediumBackpackAttachPoint; // Default "Backpack" is medium
    }

    /// <summary>
    /// Destroy the currently spawned backpack visual.
    /// </summary>
    private void DestroyBackpackVisual()
    {
        if (m_SpawnedBackpack != null)
        {
            Destroy(m_SpawnedBackpack);
            m_SpawnedBackpack = null;
        }
        m_EquippedBackpackItem = null;
    }

    private void OnDestroy()
    {
        // Clean up event subscriptions
        if (m_EquipmentCollections != null)
        {
            m_EquipmentCollections.OnItemAdded -= HandleItemEquipped;
            m_EquipmentCollections.OnItemRemoved -= HandleItemUnequipped;
        }

        DestroyBackpackVisual();
    }

    /// <summary>
    /// Get the currently equipped backpack item, if any.
    /// </summary>
    public Item GetEquippedBackpack() => m_EquippedBackpackItem;

    /// <summary>
    /// Check if a backpack is currently equipped.
    /// </summary>
    public bool HasBackpackEquipped() => m_EquippedBackpackItem != null;
}
