/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem
{
    using System;
    using System.Collections.Generic;
    using Opsive.Shared.Inventory;
    using Opsive.Shared.Utility;
    using Opsive.UltimateCharacterController.Inventory;
    using Opsive.UltimateCharacterController.Items;
    using Opsive.UltimateInventorySystem.Core;
    using UnityEngine;

    /// <summary>
    /// The Item Category Item Set Rule allow you to create item sets for any item matching the item category and the slot.
    /// </summary>
    [CreateAssetMenu(menuName = "Opsive/Ultimate Character Controller/Inventory/Item Category Item Set Rule", fileName = "MyItemCategoryItemSetRule", order = 0)]
    [Serializable]
    public class ItemCategoryItemSetRule : InventorySystemItemSetRule
    {
        
        [Tooltip("The Item Categories that occupy the item slots.")]
        [SerializeField] protected DynamicItemCategoryArray m_ItemCategorySlots;
        [Tooltip("The Item Definitions that should not occupy the item slots.")]
        [SerializeField] protected DynamicItemDefinitionArray m_ItemDefinitionExceptions;
        [Tooltip("The Item Definitions that should not occupy the item slots.")]
        [SerializeField] protected DynamicItemCategoryArray m_ItemCategoryExceptions;
        

        protected Dictionary<IItemIdentifier, int> m_CachedItemAmount;
        
        [Shared.Utility.NonSerialized] public ItemCategory[] ItemCategorySlots { get { return m_ItemCategorySlots; } set { m_ItemCategorySlots = value; } }
        [Shared.Utility.NonSerialized] public ItemDefinition[] ItemDefinitionExceptions { get { return m_ItemDefinitionExceptions; } set { m_ItemDefinitionExceptions = value; } }
        [Shared.Utility.NonSerialized] public ItemCategory[] ItemCategoryExceptions { get { return m_ItemCategoryExceptions; } set { m_ItemCategoryExceptions = value; } }

        /// <summary>
        /// Does the character item match this rule.
        /// </summary>
        /// <param name="itemSetRuleStreamData">The item set rule stream data.</param>
        /// <param name="currentPermutation">The current item permutation so far.</param>
        /// <param name="characterItem">The character item to check.</param>
        /// <returns>True if the character item matches this rule.</returns>
        public override bool DoesCharacterItemMatchRule(ItemSetRuleStreamData itemSetRuleStreamData,
            ListSlice<IItemIdentifier> currentPermutation, CharacterItem characterItem)
        {
            var item = characterItem.ItemIdentifier;
            var slotID = characterItem.SlotID;

            var categorySlots = m_ItemCategorySlots.Value;
            
            if (slotID < 0 || slotID >= categorySlots.Length) {
                return false;
            }
            
            if (categorySlots[slotID] == null) {
                return false;
            }
            if (item.InherentlyContainedByCategory(categorySlots[slotID].ID) == false) {
                return false;
            }

            var definitionExceptions = m_ItemDefinitionExceptions.Value;

            for (int i = 0; i < definitionExceptions.Length; i++) {
                if (definitionExceptions[i].InherentlyContains(item)) {
                    return false;
                }
            }

            var itemCategoryExceptions = m_ItemCategoryExceptions.Value;
            
            for (int i = 0; i < itemCategoryExceptions.Length; i++) {
                if (item.InherentlyContainedByCategory(itemCategoryExceptions[i].ID)) {
                    return false;
                }
            }

            if (!m_AllowSameItemIdentifierInOneItemSet) {
                if (currentPermutation.Contains(item)) {
                    return false;
                }
            }
            

            return true;
        }

        /// <summary>
        /// Can the slot be empty for this rule.
        /// </summary>
        /// <param name="slotID">The slot ID to check.</param>
        /// <returns>True if it can be empty.</returns>
        protected override bool CanSlotBeNull(int slotID)
        {
            var categorySlots = m_ItemCategorySlots.Value;
            
            if (slotID < 0 || slotID >= categorySlots.Length) {
                return true;
            }

            return categorySlots[slotID] == null;
        }

        /// <summary>
        /// Returns if an item set is valid for the allowed slots mask.
        /// </summary>
        /// <param name="itemSet">The item set to check.</param>
        /// <param name="allowedSlotsMask">The allowed slots mask.</param>
        /// <returns>Returns true if the item set is valid.</returns>
        public override bool IsItemSetValid(ItemSet itemSet, int allowedSlotsMask)
        {
            if (m_DoNotShareItemBetweenSet && SharesItemBetweenSet(itemSet)) {
                return false;
            }
            
            if (!m_ExactAmountValidation) {
                return true;
            }

            if (m_CachedItemAmount == null) {
                m_CachedItemAmount = new Dictionary<IItemIdentifier, int>();
            } else {
                m_CachedItemAmount.Clear();
            }

            var characterInventory = itemSet.ItemSetGroup.ItemSetManager.CharacterInventory;
                
            for (int i = 0; i < itemSet.ItemIdentifiers.Length; i++) {
                var item = itemSet.ItemIdentifiers[i];
                if(item == null){ continue; }

                if (m_CachedItemAmount.TryGetValue(item, out var value)) {
                    m_CachedItemAmount[item] = value + 1;
                } else {
                    m_CachedItemAmount[item] = 1;
                }
                    
                    
                var count = characterInventory.GetItemIdentifierAmount(item);
            }

            foreach (var itemAmount in m_CachedItemAmount) {
                if (characterInventory.GetItemIdentifierAmount(itemAmount.Key) != itemAmount.Value) {
                    return false;
                }
            }

            return true;
        }
    }
}