using UnityEngine;
using Opsive.UltimateInventorySystem.Core;
using Opsive.UltimateInventorySystem.Core.DataStructures;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;
using Opsive.UltimateInventorySystem.ItemActions;

/// <summary>
/// Item action that equips/unequips backpacks by moving them to/from the Equipped collection.
/// </summary>
[System.Serializable]
public class EquipBackpackItemAction : ItemAction
{
    [Tooltip("The name of the equipped collection.")]
    [SerializeField] private string m_EquippedCollectionName = "Equippable";

    [Tooltip("The name of the default/inventory collection.")]
    [SerializeField] private string m_DefaultCollectionName = "Default";

    /// <summary>
    /// Can the action be invoked?
    /// </summary>
    protected override bool CanInvokeInternal(ItemInfo itemInfo, ItemUser itemUser)
    {
        if (itemInfo.Item == null) return false;
        if (itemInfo.Inventory == null) return false;
        return true;
    }

    /// <summary>
    /// Invoke the action - toggle equip/unequip.
    /// </summary>
    protected override void InvokeActionInternal(ItemInfo itemInfo, ItemUser itemUser)
    {
        var inventory = itemInfo.Inventory;
        if (inventory == null)
        {
            Debug.LogWarning("[EquipBackpackItemAction] Inventory is null");
            return;
        }

        var equippedCollection = inventory.GetItemCollection(m_EquippedCollectionName);
        var defaultCollection = inventory.GetItemCollection(m_DefaultCollectionName);

        // Debug: List all available collections if not found
        if (equippedCollection == null || defaultCollection == null)
        {
            // Cast to Inventory to access collections
            var inv = inventory as Inventory;
            string availableCollections = "Available collections: ";
            if (inv != null)
            {
                foreach (var col in inv.ItemCollectionsReadOnly)
                {
                    availableCollections += $"'{col.Name}', ";
                }
            }
            Debug.LogWarning($"[EquipBackpackItemAction] Could not find collections: '{m_EquippedCollectionName}' or '{m_DefaultCollectionName}'. {availableCollections}");
            return;
        }

        // Check if item is currently equipped
        bool isEquipped = itemInfo.ItemCollection == equippedCollection;

        if (isEquipped)
        {
            // Unequip: Move from Equipped to Default
            var removed = equippedCollection.RemoveItem(itemInfo);
            if (removed.Amount > 0)
            {
                defaultCollection.AddItem(removed);
            }
        }
        else
        {
            // Equip: First check if another backpack is already equipped
            // Remove any existing backpack from equipped collection
            var equippedItems = equippedCollection.GetAllItemStacks();
            for (int i = equippedItems.Count - 1; i >= 0; i--)
            {
                var stack = equippedItems[i];
                if (stack.Item != null && stack.Item.ItemDefinition.Category == itemInfo.Item.ItemDefinition.Category)
                {
                    // Same category (Backpack) - unequip the old one first
                    var oldItem = equippedCollection.RemoveItem((ItemInfo)stack);
                    if (oldItem.Amount > 0)
                    {
                        defaultCollection.AddItem(oldItem);
                    }
                }
            }

            // Now equip the new backpack
            var removed = itemInfo.ItemCollection.RemoveItem(itemInfo);
            if (removed.Amount > 0)
            {
                equippedCollection.AddItem(removed);
            }
        }
    }
}
