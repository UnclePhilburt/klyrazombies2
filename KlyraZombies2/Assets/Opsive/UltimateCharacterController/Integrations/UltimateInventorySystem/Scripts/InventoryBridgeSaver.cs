/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem
{
    using Opsive.Shared.Events;
    using Opsive.Shared.Game;
    using Opsive.Shared.Utility;
    using Opsive.UltimateCharacterController.Inventory;
    using Opsive.UltimateCharacterController.Items.Actions;
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.AttributeSystem;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using Opsive.UltimateInventorySystem.SaveSystem;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Inventory = Opsive.UltimateInventorySystem.Core.InventoryCollections.Inventory;

    /// <summary>
    /// A saver component that saves the content of an inventory.
    /// </summary>
    public class InventoryBridgeSaver : SaverBase
    {
        /// <summary>
        /// The inventory save data.
        /// </summary>
        [System.Serializable]
        public struct ItemSetGroupSaveData
        {
            [SerializeField] public uint CategoryID;
            [SerializeField] public string CategoryName;
            [SerializeField] public int ActiveItemSetIndex;
        }
        
        /// <summary>
        /// The inventory save data.
        /// </summary>
        [System.Serializable]
        public struct InventoryBridgeSaveData
        {
            public IDAmountSaveData[][] ItemIDAmountsPerCollection;
            public ItemSetGroupSaveData[] ItemSetGroupSaveDataArray;
        }

        [Tooltip("Is the save data added to the loadout of does it overwrite it.")]
        [SerializeField] protected bool m_Additive;

        private Character.UltimateCharacterLocomotion m_CharacterLocomotion;
        private Inventory m_Inventory;
        private InventoryItemSetManager m_InventoryItemSetManager;
        private CharacterInventoryBridge m_InventoryBridge;

        /// <summary>
        /// Initialize the component.
        /// </summary>
        protected override void Awake()
        {
            m_CharacterLocomotion = gameObject.GetCachedComponent<Character.UltimateCharacterLocomotion>();
            m_Inventory = gameObject.GetCachedComponent<Inventory>();
            m_InventoryItemSetManager = gameObject.GetCachedComponent<InventoryItemSetManager>();
            m_InventoryBridge = gameObject.GetCachedComponent<CharacterInventoryBridge>();
            
            EventHandler.RegisterEvent<int>(EventNames.c_WillStartLoadingSave_Index, OnWillStartLoading);

            base.Awake();
        }

        /// <summary>
        /// Serialize the save data.
        /// </summary>
        /// <returns>The serialized data.</returns>
        public override Serialization SerializeSaveData()
        {
            if (m_Inventory == null) { return null; }
            
            var itemCollectionCount = m_Inventory.GetItemCollectionCount();
            var newItemAmountsArray = new IDAmountSaveData[itemCollectionCount][];
            var listItemIDs = new List<uint>();

            for (int i = 0; i < itemCollectionCount; i++) {
                
                var itemCollection = m_Inventory.GetItemCollection(i);
                
                if (itemCollection.Purpose == ItemCollectionPurpose.Loadout) {
                    continue;
                }
                
                IReadOnlyList<ItemStack> allItemStacks;
                // Get the items by slot to save the correct index
                if (itemCollection is ItemSlotCollection itemSlotCollection) {
                    allItemStacks = itemSlotCollection.ItemsBySlot;
                } else {
                    allItemStacks = itemCollection.GetAllItemStacks();
                }

                var itemAmounts = new IDAmountSaveData[allItemStacks?.Count ?? 0];
                for (int j = 0; j < itemAmounts.Length; j++) {
                    var itemStack = allItemStacks[j];
                    
                    if (itemStack?.Item == null) {
                        itemAmounts[j] = new IDAmountSaveData() {
                            ID = 0,
                            Amount = 0
                        };
                        continue;
                    }
                    
                    itemAmounts[j] = new IDAmountSaveData() {
                        ID = itemStack.Item.ID,
                        Amount = itemStack.Amount
                    };

                    listItemIDs.Add(itemStack.Item.ID);
                }
                
                newItemAmountsArray[i] = itemAmounts;
            }
            
            SaveSystemManager.InventorySystemManagerItemSaver.SetItemsToSave(FullKey, listItemIDs);
            
            //Save only part of the Item Set Group. Item Sets created by the Item Set Rules don't need to be saved.
            var categoryItemSetsToSave = new ItemSetGroupSaveData[m_InventoryItemSetManager.ItemSetGroups.Length];
            for (int i = 0; i < m_InventoryItemSetManager.ItemSetGroups.Length; i++) {

                var categoryItemSet = m_InventoryItemSetManager.ItemSetGroups[i];
                categoryItemSetsToSave[i] = new ItemSetGroupSaveData(){
                    CategoryID = categoryItemSet.CategoryID,
                    CategoryName = categoryItemSet.CategoryName,
                    ActiveItemSetIndex = categoryItemSet.ActiveItemSetIndex
                };
            }
            
            var saveData = new InventoryBridgeSaveData {
                ItemIDAmountsPerCollection = newItemAmountsArray,
                ItemSetGroupSaveDataArray = categoryItemSetsToSave
            };

            return Serialization.Serialize(saveData);
        }

        private void OnWillStartLoading(int saveIndex)
        {
            //Items Need to be unequipped and removed before the items are loaded from the save data otherwise
            //Item Object bound attribute values might be updated when unequipping item after they were loaded from save.
            
            m_InventoryBridge.OnWillLoadSaveData();
            EventHandler.ExecuteEvent(gameObject, IntegrationEventNames.c_GameObject_OnInventoryBridgeSaverWillLoad);
            
            //Unequip everything first.
            var categoryCounts = m_InventoryItemSetManager.CategoryCount;
            for (int i = 0; i < categoryCounts; i++) {
                m_InventoryBridge.Equip(i,-1,true,true);
            }
            
            // The previous inventory should be cleared.
            var itemCollectionCount = m_Inventory.GetItemCollectionCount();
            for (int i = 0; i < itemCollectionCount; i++) {
                var itemCollection = m_Inventory.GetItemCollection(i);
                if (itemCollection.Purpose == ItemCollectionPurpose.Loadout) {
                    continue;
                }
                itemCollection.RemoveAll();
            }
            m_InventoryBridge.RemoveAllItems(false);
        }
        
        /// <summary>
        /// Deserialize and load the save data.
        /// </summary>
        /// <param name="serializedSaveData">The serialized save data.</param>
        public override void DeserializeAndLoadSaveData(Serialization serializedSaveData)
        {
            if (m_Inventory == null) { return; }

            var savedData = serializedSaveData.DeserializeFields(MemberVisibility.All) as InventoryBridgeSaveData?;

            if (!m_Additive) {
                var itemCollectionCount = m_Inventory.GetItemCollectionCount();

                for (int i = 0; i < itemCollectionCount; i++) {
                    m_Inventory.GetItemCollection(i).RemoveAll();
                }
            }
            
            if (savedData.HasValue == false) {
                return;
            }

            var inventorySaveData = savedData.Value;
            if (inventorySaveData.ItemIDAmountsPerCollection == null) { return; }
            
            EventHandler.ExecuteEvent(m_Inventory.gameObject, EventNames.c_InventoryGameObject_InventoryMonitorListen_Bool, false);

            // Restore the items.
            for (int i = 0; i < inventorySaveData.ItemIDAmountsPerCollection.Length; i++) {
                var itemCollection = m_Inventory.GetItemCollection(i);
                if (itemCollection == null) {
                    Debug.LogWarning("Item Collection from save data is missing in the scene.");
                    continue;
                }

                // The Loadout should not be restored as it stays constant.
                if (itemCollection.Purpose == ItemCollectionPurpose.Loadout) {
                    continue;
                }

                var itemIDAmounts = inventorySaveData.ItemIDAmountsPerCollection[i];
                var itemAmounts = new ItemAmount[itemIDAmounts.Length];
                for (int j = 0; j < itemIDAmounts.Length; j++) {
                    var idAmountSaveData = itemIDAmounts[j];
                    // The id can be 0 for item slot collections.
                    if(idAmountSaveData.ID == 0){ continue;}
                    
                    if (InventorySystemManager.ItemRegister.TryGetValue(itemIDAmounts[j].ID, out var item) == false) {
                        Debug.LogWarning($"Saved Item ID {itemIDAmounts[j].ID} could not be retrieved from the Inventory System Manager.");
                        continue;
                    }

                    itemAmounts[j] = new ItemAmount(item, itemIDAmounts[j].Amount);
                }
                
                if (itemCollection is ItemSlotCollection itemSlotCollection) {
                    for (int j = 0; j < itemAmounts.Length; j++) {
                        var itemAmount = itemAmounts[j];
                        if(itemAmount.Item == null){ continue; }
                        itemSlotCollection.AddItem(new ItemInfo(itemAmount), j);
                    }
                } else {
                    itemCollection.AddItems(itemAmounts);
                }
                    
            }
            
            EventHandler.ExecuteEvent(m_Inventory.gameObject, EventNames.c_InventoryGameObject_InventoryMonitorListen_Bool, true);
            
            //Wait for a frame such that the start function of items can get called.
            StartCoroutine(EquipItemNextFrame(inventorySaveData));
        }

        private IEnumerator EquipItemNextFrame(InventoryBridgeSaveData inventorySaveData)
        {
            //Wait a frame
            yield return null;
            
            // Restore the active ItemSets.
            if (inventorySaveData.ItemSetGroupSaveDataArray != null && inventorySaveData.ItemSetGroupSaveDataArray.Length > 0) {
                for (int i = 0; i < inventorySaveData.ItemSetGroupSaveDataArray.Length; i++) {
                    m_InventoryBridge.Equip(i,inventorySaveData.ItemSetGroupSaveDataArray[i].ActiveItemSetIndex,true,true);
                }
            }
            
            //Send event that the bridge finished
            m_InventoryBridge.OnSaveDataLoaded();
            EventHandler.ExecuteEvent(gameObject, IntegrationEventNames.c_GameObject_OnInventoryBridgeSaverLoaded);
        }

        /// <summary>
        /// Unregister events on destroy.
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            EventHandler.UnregisterEvent<int>(EventNames.c_WillStartLoadingSave_Index, OnWillStartLoading);
        }
    }
}