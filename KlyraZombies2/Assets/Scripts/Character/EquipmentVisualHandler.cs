using UnityEngine;
using System.Collections.Generic;
using Opsive.UltimateInventorySystem.Core;
using Opsive.UltimateInventorySystem.Core.DataStructures;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;
using Opsive.Shared.Events;

/// <summary>
/// Handles spawning/removing equipment visuals on a character based on inventory equipment changes.
/// Attach to the player character alongside the Inventory component.
/// </summary>
public class EquipmentVisualHandler : MonoBehaviour
{
    [Header("Inventory Settings")]
    [Tooltip("The inventory component (auto-finds if not set)")]
    [SerializeField] private Inventory m_Inventory;

    [Tooltip("Name of the item collection that holds equipped items")]
    [SerializeField] private string m_EquippedCollectionName = "Equipped";

    [Header("Equipment Slots")]
    [Tooltip("Configure each equipment slot with its attachment point")]
    [SerializeField] private List<EquipmentSlotConfig> m_EquipmentSlots = new List<EquipmentSlotConfig>();

    [Header("Attribute Names")]
    [Tooltip("Item attribute name for the visual prefab")]
    [SerializeField] private string m_PrefabAttributeName = "Prefabs";

    [Tooltip("Item attribute name for the equipment slot type")]
    [SerializeField] private string m_SlotTypeAttributeName = "EquipmentSlot";

    [Header("Debug")]
    [SerializeField] private bool m_DebugLog = false;

    // Track spawned equipment by slot
    private Dictionary<EquipmentSlotType, SpawnedEquipment> m_SpawnedEquipment = new Dictionary<EquipmentSlotType, SpawnedEquipment>();

    // Reference to the equipped collection
    private ItemCollection m_EquippedCollection;

    [System.Serializable]
    public class EquipmentSlotConfig
    {
        public EquipmentSlotType slotType;
        public Transform attachPoint;
        [Tooltip("Category name that can equip to this slot (e.g., 'Helmet', 'Shirt')")]
        public string categoryName;
        [Tooltip("For Sidekick integration - the CharacterPartType this slot maps to")]
        public string sidekickPartType;
    }

    private class SpawnedEquipment
    {
        public GameObject visualObject;
        public ItemDefinition itemDefinition;
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        // Get inventory if not assigned
        if (m_Inventory == null)
        {
            m_Inventory = GetComponent<Inventory>();
        }

        if (m_Inventory == null)
        {
            Debug.LogError("[EquipmentVisualHandler] No Inventory component found!");
            return;
        }

        // Find the equipped collection
        m_EquippedCollection = m_Inventory.GetItemCollection(m_EquippedCollectionName);
        if (m_EquippedCollection == null)
        {
            Debug.LogWarning($"[EquipmentVisualHandler] Could not find collection '{m_EquippedCollectionName}'");
            return;
        }

        // Subscribe to inventory events
        m_EquippedCollection.OnItemAdded += OnItemEquipped;
        m_EquippedCollection.OnItemRemoved += OnItemUnequipped;

        // Check for already equipped items
        RefreshAllEquipment();

        if (m_DebugLog)
        {
            Debug.Log($"[EquipmentVisualHandler] Initialized with {m_EquipmentSlots.Count} slots");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (m_EquippedCollection != null)
        {
            m_EquippedCollection.OnItemAdded -= OnItemEquipped;
            m_EquippedCollection.OnItemRemoved -= OnItemUnequipped;
        }
    }

    /// <summary>
    /// Called when an item is added to the equipped collection
    /// </summary>
    private void OnItemEquipped(ItemInfo originalItemInfo, ItemStack addedItemStack)
    {
        if (addedItemStack?.Item?.ItemDefinition == null) return;

        var slot = GetSlotForItem(addedItemStack.Item.ItemDefinition);
        if (slot == null)
        {
            if (m_DebugLog)
            {
                Debug.Log($"[EquipmentVisualHandler] No slot found for {addedItemStack.Item.ItemDefinition.name}");
            }
            return;
        }

        SpawnEquipmentVisual(slot, addedItemStack.Item.ItemDefinition);
    }

    /// <summary>
    /// Called when an item is removed from the equipped collection
    /// </summary>
    private void OnItemUnequipped(ItemInfo originalItemInfo)
    {
        if (originalItemInfo.Item?.ItemDefinition == null) return;

        var slot = GetSlotForItem(originalItemInfo.Item.ItemDefinition);
        if (slot == null) return;

        RemoveEquipmentVisual(slot.slotType);
    }

    /// <summary>
    /// Find which slot an item should equip to
    /// </summary>
    private EquipmentSlotConfig GetSlotForItem(ItemDefinition itemDef)
    {
        // First try to get slot type from item attribute
        if (itemDef.TryGetAttributeValue<string>(m_SlotTypeAttributeName, out var slotTypeName))
        {
            if (System.Enum.TryParse<EquipmentSlotType>(slotTypeName, out var slotType))
            {
                return m_EquipmentSlots.Find(s => s.slotType == slotType);
            }
        }

        // Fall back to matching by category name
        var category = itemDef.Category;
        if (category != null)
        {
            // Check if any slot matches this category
            var slot = m_EquipmentSlots.Find(s => s.categoryName == category.name);
            if (slot != null) return slot;

            // Check if the category inherently contains items that match any slot's category
            foreach (var equipSlot in m_EquipmentSlots)
            {
                if (!string.IsNullOrEmpty(equipSlot.categoryName) &&
                    category.name.Contains(equipSlot.categoryName))
                {
                    return equipSlot;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Spawn the visual for an equipment item
    /// </summary>
    private void SpawnEquipmentVisual(EquipmentSlotConfig slot, ItemDefinition itemDef)
    {
        if (slot.attachPoint == null)
        {
            Debug.LogWarning($"[EquipmentVisualHandler] No attach point for slot {slot.slotType}");
            return;
        }

        // Remove existing equipment in this slot
        RemoveEquipmentVisual(slot.slotType);

        // Get the prefab from the item's attributes
        GameObject prefab = null;
        if (itemDef.TryGetAttributeValue<GameObject>(m_PrefabAttributeName, out var directPrefab))
        {
            prefab = directPrefab;
        }
        else if (itemDef.TryGetAttributeValue<GameObject[]>(m_PrefabAttributeName, out var prefabArray) && prefabArray.Length > 0)
        {
            prefab = prefabArray[0];
        }

        if (prefab == null)
        {
            if (m_DebugLog)
            {
                Debug.Log($"[EquipmentVisualHandler] No prefab found for {itemDef.name}");
            }
            return;
        }

        // Spawn the visual
        var visual = Instantiate(prefab, slot.attachPoint);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        visual.name = $"Equipment_{slot.slotType}_{itemDef.name}";

        // Track the spawned equipment
        m_SpawnedEquipment[slot.slotType] = new SpawnedEquipment
        {
            visualObject = visual,
            itemDefinition = itemDef
        };

        if (m_DebugLog)
        {
            Debug.Log($"[EquipmentVisualHandler] Spawned {itemDef.name} at {slot.slotType}");
        }
    }

    /// <summary>
    /// Remove the visual for an equipment slot
    /// </summary>
    private void RemoveEquipmentVisual(EquipmentSlotType slotType)
    {
        if (m_SpawnedEquipment.TryGetValue(slotType, out var equipment))
        {
            if (equipment.visualObject != null)
            {
                Destroy(equipment.visualObject);

                if (m_DebugLog)
                {
                    Debug.Log($"[EquipmentVisualHandler] Removed equipment from {slotType}");
                }
            }
            m_SpawnedEquipment.Remove(slotType);
        }
    }

    /// <summary>
    /// Refresh all equipment visuals based on current inventory
    /// </summary>
    public void RefreshAllEquipment()
    {
        if (m_EquippedCollection == null) return;

        // Clear existing visuals
        foreach (var slot in m_SpawnedEquipment.Values)
        {
            if (slot.visualObject != null)
            {
                Destroy(slot.visualObject);
            }
        }
        m_SpawnedEquipment.Clear();

        // Spawn visuals for all equipped items
        var allItems = m_EquippedCollection.GetAllItemStacks();
        foreach (var itemStack in allItems)
        {
            if (itemStack.Item?.ItemDefinition == null) continue;

            var slot = GetSlotForItem(itemStack.Item.ItemDefinition);
            if (slot != null)
            {
                SpawnEquipmentVisual(slot, itemStack.Item.ItemDefinition);
            }
        }
    }

    /// <summary>
    /// Check if a slot has equipment
    /// </summary>
    public bool HasEquipment(EquipmentSlotType slotType)
    {
        return m_SpawnedEquipment.ContainsKey(slotType);
    }

    /// <summary>
    /// Get the item definition equipped in a slot
    /// </summary>
    public ItemDefinition GetEquippedItem(EquipmentSlotType slotType)
    {
        if (m_SpawnedEquipment.TryGetValue(slotType, out var equipment))
        {
            return equipment.itemDefinition;
        }
        return null;
    }

    /// <summary>
    /// Get the visual GameObject for a slot
    /// </summary>
    public GameObject GetEquipmentVisual(EquipmentSlotType slotType)
    {
        if (m_SpawnedEquipment.TryGetValue(slotType, out var equipment))
        {
            return equipment.visualObject;
        }
        return null;
    }
}
