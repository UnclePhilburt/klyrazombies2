/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem
{
    using System.Collections.Generic;
    using Opsive.Shared.Game;
    using Opsive.Shared.Inventory;
    using Opsive.Shared.Utility;
    using Opsive.UltimateCharacterController.Inventory;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using UnityEngine;
    using Inventory = Opsive.UltimateInventorySystem.Core.InventoryCollections.Inventory;

    /// <summary>
    /// An Item Set Rule used to match the Character ItemSet slots with the Inventory ItemSlotCollection slots 
    /// </summary>
    [CreateAssetMenu(menuName = "Opsive/Ultimate Character Controller/Inventory/Item Slot Collection Item Set Rule", fileName = "MyItemSlotCollectionItemSetRule", order = 0)]
    public class ItemSlotCollectionItemSetRule : ItemSetRuleBase
    {
        [SerializeField] protected string m_ItemSlotCollectionName = "Equippable Slots";
        [Tooltip("The default Item Set that is used to create the runtime item sets.")]
        [SerializeField] protected ItemSet m_DefaultItemSet = new ItemSet("{0}");
        [Tooltip("Is the ItemSet the default ItemSet?")]
        [SerializeField] protected bool m_Default = false;
        [Tooltip("Should an empty ItemSet be created if there are no matching items?")]
        [SerializeField] protected bool m_AllowEmptySet;
        
        [Shared.Utility.NonSerialized] public string State { get { return m_DefaultItemSet.State; } set { m_DefaultItemSet.State = value; } }
        public bool Default { get { return m_Default; } set { m_Default = value; } }
        
        public ItemSet DefaultItemSet { get { return m_DefaultItemSet; } set { m_DefaultItemSet = value; } }

        protected List<ItemSetStateInfo> m_TemporaryItemSetStateInfos;
        protected List<IItemIdentifier> m_CachedItemSetIdentifiers;

        /// <summary>
        /// Returns if an item set is valid for the allowed slots mask.
        /// </summary>
        /// <param name="itemSet">The item set to check.</param>
        /// <param name="allowedSlotsMask">The allowed slots mask.</param>
        /// <returns>Returns true if the item set is valid.</returns>
        public override bool IsItemSetValid(ItemSet itemSet, int allowedSlotsMask)
        {
            return true;
        }

        /// <summary>
        /// From the Item Set Rule Stream Data return the next item set state info.
        /// </summary>
        /// <param name="itemSetRuleStreamData">The item set rule stream data.</param>
        /// <returns>Return the item set state info.</returns>
        public override ListSlice<ItemSetStateInfo> GetNextItemSetsStateInfo(ItemSetRuleStreamData itemSetRuleStreamData)
        {
            var itemSetManager = itemSetRuleStreamData.ItemSetManager;
            var groupIndex = itemSetRuleStreamData.GroupIndex;
            var slotCount = itemSetManager.SlotCount;
            var itemSetGroup = itemSetManager.ItemSetGroups[groupIndex];
            var currentItemSets = itemSetGroup.GetRuleItemSetList(this);
            var itemSetRuleInfo = new ItemSetRuleInfo(itemSetGroup, this);

            // Initialize the lists
            if (m_TemporaryItemSetStateInfos == null) {
                m_TemporaryItemSetStateInfos = new List<ItemSetStateInfo>();
            } else {
                m_TemporaryItemSetStateInfos.Clear();
            }
            
            // Initialize the lists
            if (m_CachedItemSetIdentifiers == null) {
                m_CachedItemSetIdentifiers = new List<IItemIdentifier>();
            } 
            if (m_CachedItemSetIdentifiers.Count != slotCount) {
                m_CachedItemSetIdentifiers.EnsureSize(slotCount);
            }

            // First check if there is a current ItemSet and set it as to be removed
            for (int i = 0; i < currentItemSets.Count; i++) {
                m_TemporaryItemSetStateInfos.Add(
                    new ItemSetStateInfo(itemSetRuleInfo, currentItemSets[i], ItemSetStateInfo.SetState.Remove, m_Default));
            }

            var inventory = itemSetManager.gameObject.GetCachedComponent<Inventory>();
            if (inventory == null) {
                Debug.LogWarning("The Inventory could not be found on the character.", itemSetManager.gameObject);
                return m_TemporaryItemSetStateInfos;
            }
            
            var itemSlotCollection = inventory.GetItemCollection(m_ItemSlotCollectionName) as ItemSlotCollection;
            if (itemSlotCollection == null) {
                Debug.LogWarning($"The ItemCollection with name {m_ItemSlotCollectionName} does not exist or is not of type ItemSlotCollection.", itemSetManager.gameObject);
                return m_TemporaryItemSetStateInfos;
            }

            var foundAtLeastOneCharacterItemMatch = false;
            for (int i = 0; i < slotCount; i++) {
                var slotItem = itemSlotCollection.GetItemAtSlot(i);

                if (slotItem == null) {
                    m_CachedItemSetIdentifiers[i] = null;
                    continue;
                }

                var characterItems = itemSetRuleStreamData.CharacterItemsBySlot[i];

                for (int j = 0; j < characterItems.Count; j++) {
                    if (characterItems[j].ItemIdentifier != slotItem) { continue; }

                    // Found a match.
                    foundAtLeastOneCharacterItemMatch = true;
                    m_CachedItemSetIdentifiers[i] = slotItem;
                    break;
                }
            }

            // If there is no item set then return.
            if (foundAtLeastOneCharacterItemMatch == false && m_AllowEmptySet == false) {
                return m_TemporaryItemSetStateInfos;
            }
            
            // Clear the item set state infos, it will now compute whether it keeps, remove, or adds the item set.
            m_TemporaryItemSetStateInfos.Clear();

            var foundSetMatch = false;
            //Set what item set are to keep, add or remove.
            for (int i = 0; i < currentItemSets.Count; i++) {

                var currentItemSet = currentItemSets[i];

                var allSlotMatch = true;
                for (int k = 0; k < slotCount; k++) {
                    if (currentItemSet.ItemIdentifiers[k] != m_CachedItemSetIdentifiers[k]) {
                        allSlotMatch = false;
                        break;
                    }
                }

                if (!allSlotMatch) {
                    m_TemporaryItemSetStateInfos.Add(
                        new ItemSetStateInfo(itemSetRuleInfo, currentItemSet,ItemSetStateInfo.SetState.Remove, m_Default));
                } else {
                    foundSetMatch = true;
                    m_TemporaryItemSetStateInfos.Add(
                        new ItemSetStateInfo(itemSetRuleInfo, currentItemSet,ItemSetStateInfo.SetState.Keep, m_Default));
                }
            }

            if (foundSetMatch == false) {
                //New item set index set to -1 it will be updated once it is really set.
                var newItemSet = CreateItemSet(itemSetManager, m_CachedItemSetIdentifiers);
                newItemSet.SetItemSetGroup(itemSetRuleStreamData.ItemSetGroup);
                
                m_TemporaryItemSetStateInfos.Add(
                    new ItemSetStateInfo(itemSetRuleInfo, newItemSet,ItemSetStateInfo.SetState.Add, m_Default));
            }
            
            return m_TemporaryItemSetStateInfos;
        }
        
        /// <summary>
        /// Create an item set.
        /// </summary>
        /// <param name="itemSetManager">The item set manager.</param>
        /// <param name="itemsInSet">The items within the item set.</param>
        /// <returns>The item set.</returns>
        protected virtual ItemSet CreateItemSet(ItemSetManagerBase itemSetManager, ListSlice<IItemIdentifier> itemsInSet)
        {
            var slotCount = itemSetManager.SlotCount;
            var itemSet = itemSetManager.PopItemSetFromPool(this, DefaultItemSet);

            if (itemSet.ItemIdentifiers == null || itemSet.ItemIdentifiers.Length != slotCount) {
                itemSet.ItemIdentifiers = new IItemIdentifier[slotCount];
            }

            for (int i = 0; i < slotCount; i++) {
                itemSet.ItemIdentifiers[i] = itemsInSet[i];
            }
            
            itemSet.State = GetMainStateNameForItemSet(itemSet, itemSetManager, itemsInSet);

            return itemSet;
        }

        /// <summary>
        /// Get the state name for the item set.
        /// </summary>
        /// <param name="itemSet">The item set to get the state name from.</param>
        /// <param name="itemSetManager">The item set manager.</param>
        /// <param name="itemsInSet">The items in the set.</param>
        /// <returns>The state name for the item set.</returns>
        protected virtual string GetMainStateNameForItemSet(ItemSet itemSet, ItemSetManagerBase itemSetManager, ListSlice<IItemIdentifier> itemsInSet)
        {
            if (State.Contains("{0}") == false) {
                return State;
            }

            var itemNames = "";
            for (int i = 0; i < itemSetManager.SlotCount; i++) {
                itemNames += itemsInSet[i]?.GetItemDefinition()?.name;
            }
            return string.Format(State, itemNames);
        }
    }
}