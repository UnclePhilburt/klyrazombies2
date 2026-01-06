/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem
{
    using Opsive.Shared.Inventory;
    using Opsive.UltimateCharacterController.Inventory;
    using UnityEngine;
    using Inventory = Opsive.UltimateInventorySystem.Core.InventoryCollections.Inventory;

    /// <summary>
    /// The ItemSetManager manages the ItemSets belonging to the character.
    /// </summary>
    public class InventoryItemSetManager : ItemSetManagerBase
    {
        

        /// <summary>
        /// Get the default ItemCategory to use when an ItemSetGroup does not have a category assigned.
        /// </summary>
        /// <returns>The default ItemCategory.</returns>
        public override CategoryBase GetDefaultCategory()
        {
            var inventoryBridge = GetComponent<CharacterInventoryBridge>();
            if (inventoryBridge != null) {
                return inventoryBridge.EquippableCategory;
            }

            return null;
        }
    }
}