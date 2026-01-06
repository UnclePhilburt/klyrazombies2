/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem
{
    using Opsive.Shared.Utility;
    using Opsive.UltimateCharacterController.Inventory;
    using UnityEngine;

    public abstract class InventorySystemItemSetRule : ItemSetRule
    {
        // Nothing inside, this class is simply used to share a custom inspector.
        [Tooltip("If set to true, It will allow the same item set to be used within one ItemSet.")]
        [SerializeField] protected bool m_AllowSameItemIdentifierInOneItemSet = false;
        [Tooltip("If set to true, The item set rule will not create an itemSet if an existing item set already has the item. (Only works for item sets before in the ItemSetRuleStream)")]
        [SerializeField] protected bool m_DoNotShareItemBetweenSet = false;
        [Tooltip("If set to true, check that the number of Items in the Inventory exactly match the number of slots used by that item.")]
        [SerializeField] protected bool m_ExactAmountValidation = false;
        
        /// <summary>
        /// Determines if the specified item set shares any items with other item sets in the given item set group.
        /// </summary>
        /// <param name="itemSetGroup">The group of item sets to check against.</param>
        /// <param name="itemSet">The item set to check for shared items.</param>
        /// <returns>True if the item set shares any items with other valid item sets in the group; otherwise, false.</returns>
        public bool SharesItemBetweenSet(ItemSet itemSet)
        {
            // Do not share with other item set rules.
            var itemSetStateSoFar = itemSet.ItemSetGroup.ItemSetStateList;
        
            foreach (var itemSetState in itemSetStateSoFar) {
        
                // Only check item sets above this one.
                if (itemSetState.ItemSet == itemSet) {
                    break;
                }
        
                // Ignore item sets that will be removed.
                if (itemSetState.State == ItemSetStateInfo.SetState.Remove) {
                    continue;
                }
        
                // Ignore invalid item sets.
                if (itemSetState.ItemSet.IsValid == false) {
                    continue;
                }
        
                foreach (var itemIdentifier in itemSetState.ItemSet.ItemIdentifiers) {
                    if (itemSet.ItemIdentifiers.Contains(itemIdentifier)) {
                        return true;
                    }
                }
            }
        
            return false;
        }
    }
}