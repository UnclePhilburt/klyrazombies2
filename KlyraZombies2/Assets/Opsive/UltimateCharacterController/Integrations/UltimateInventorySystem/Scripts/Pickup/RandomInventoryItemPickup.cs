/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem
{
    using Opsive.Shared.Game;
    using Opsive.UltimateCharacterController.Inventory;
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.DropsAndPickups;
    using UnityEngine;

    using Inventory = Opsive.UltimateInventorySystem.Core.InventoryCollections.Inventory;

    public class RandomInventoryItemPickup : InventoryItemPickup
    {
        [Tooltip("The minimum amount of item that can be picked up.")]
        [SerializeField] protected int m_MinAmount = 1;
        [Tooltip("The maximum amount of item that can be picked up.")]
        [SerializeField] protected int m_MaxAmount = 2;
        [Tooltip("An animation curve to specify the probability of the amount dropped, 0->minAmount, 1->maxAmount.")]
        [SerializeField] protected AnimationCurve m_AmountProbabilityDistribution;

        protected ItemAmountProbabilityTable m_ItemAmountProbabilityTable;

        /// <summary>
        /// Initialize the probability table.
        /// </summary>
        protected virtual void Start()
        {
            m_ItemAmountProbabilityTable = new ItemAmountProbabilityTable((m_Inventory.MainItemCollection.GetAllItemStacks(), 0));
        }
        
        /// <summary>
        /// Internal method which picks up the ItemIdentifier.
        /// </summary>
        /// <param name="character">The character that should pick up the ItemIdentifier.</param>
        /// <param name="inventory">The inventory belonging to the character.</param>
        /// <param name="slotID">The slot ID that picked up the item. A -1 value will indicate no specified slot.</param>
        /// <param name="immediatePickup">Should the item be picked up immediately?</param>
        /// <param name="forceEquip">Should the item be force equipped?</param>
        /// <returns>True if an ItemIdentifier was picked up.</returns>
        protected override bool DoItemIdentifierPickupInternal(GameObject character, InventoryBase inventory, int slotID, bool immediatePickup, bool forceEquip)
        {
            if (m_ItemAmountProbabilityTable.Count == 0) { return false; }
            
            var pickupItems = m_ItemAmountProbabilityTable.GetRandomItemAmounts(m_MinAmount, m_MaxAmount, m_AmountProbabilityDistribution);
            
            var inventorySystemInventory = character.GetCachedComponent<Inventory>();
            for (int i = 0; i < pickupItems.Count; ++i) {
                var itemAmount = pickupItems[i];
                if (itemAmount.Item.IsMutable) {
                    inventorySystemInventory.MainItemCollection.AddItem(InventorySystemManager.CreateItem(itemAmount.Item, null), itemAmount.Amount);
                } else {
                    inventorySystemInventory.MainItemCollection.AddItem(itemAmount.Item, itemAmount.Amount);
                }
            }
            return pickupItems.Count > 0;
        }
    }
}