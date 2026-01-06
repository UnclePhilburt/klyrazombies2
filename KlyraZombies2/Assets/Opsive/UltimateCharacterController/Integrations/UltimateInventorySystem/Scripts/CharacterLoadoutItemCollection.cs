/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem
{
    using System;
    using System.Collections.Generic;
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using UnityEngine;

    /// <summary>
    /// This Item Collection will add items to another Item Collection when the game starts
    /// It can also be used to set whether the item should be soft or active equipped.
    /// This component must be used with a Character Inventory Bridge.
    /// </summary>
    [Serializable]
    public class CharacterLoadoutItemCollection : ItemCollection
    {
        [Tooltip("The Item Collection where the loadout Items will be added.")]
        [SerializeField] protected string m_GiveToItemCollection = "Default";
        [Tooltip("Should the load out items be equipped? The item collection to give the items to must be equippable.")]
        [SerializeField] protected bool m_SetActiveEquipped;

        public string GiveToItemCollection
        {
            get => m_GiveToItemCollection;
            set => m_GiveToItemCollection = value;
        }

        public bool SetActiveEquipped
        {
            get => m_SetActiveEquipped;
            set => m_SetActiveEquipped = value;
        }

        protected List<ItemInfo> m_CachedAddedItems;

        /// <summary>
        /// Load the Character loadout by giving the items to the item collection and equipping it if possible.
        /// </summary>
        /// <param name="characterInventoryBridge"></param>
        public void LoadCharacterLoadout(CharacterInventoryBridge characterInventoryBridge)
        {
            var itemCollectionToGiveItemsTo = m_Inventory.GetItemCollection(m_GiveToItemCollection);

            if (itemCollectionToGiveItemsTo == null) {
                Debug.LogError($"The Character Loadout Item Collection cannot find the item collection with name {m_GiveToItemCollection}");
                return;
            }

            if (m_CachedAddedItems == null) {
                m_CachedAddedItems = new List<ItemInfo>();
            } else {
                m_CachedAddedItems.Clear();
            }
            
            itemCollectionToGiveItemsTo.UpdateEventDisabled = true;
            // Not all items can belong to multiple collections.
            // Create a new item and add that item to the default.
            // The loadout should remain the same.
            var items = GetAllItemStacks();
            for (int i = 0; i < items.Count; ++i) {
                var duplicateItem = InventorySystemManager.CreateItem(items[i].Item);
                var addedItem = itemCollectionToGiveItemsTo.AddItem((ItemInfo)(duplicateItem, items[i].Amount));
                m_CachedAddedItems.Add(addedItem);
            }
            itemCollectionToGiveItemsTo.UpdateEventDisabled = false;
            UpdateCollection();
            
            if(characterInventoryBridge.BridgeItemCollections.Contains(itemCollectionToGiveItemsTo) == false)
            { return; }
            
            //Equip the items that were set
            for (int i = 0; i < m_CachedAddedItems.Count; i++) {
                var addedItem = m_CachedAddedItems[i];
                characterInventoryBridge.Equip(addedItem, m_SetActiveEquipped, true, true);
            }
        }
    }
}