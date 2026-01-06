/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem
{
    using Opsive.Shared.Game;
    using Opsive.UltimateCharacterController.Inventory;
    using Opsive.UltimateCharacterController.Objects.CharacterAssist;
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.Storage;
    using UnityEngine;

    using Inventory = Opsive.UltimateInventorySystem.Core.InventoryCollections.Inventory;

    /// <summary>
    /// Allows an object with an Ultimate Inventory System inventory to pickup items when a character enters the trigger.
    /// </summary>
    public class InventoryItemPickup : ItemPickupBase
    {
        [Tooltip("Equip the item that was picked up?")]
        [SerializeField] protected bool m_EquipOnPickup;
        [Tooltip("Copy the items before they are picked up?")]
        [SerializeField] protected bool m_PickupItemCopies;
        [Tooltip("Remove the items that were picked up from the inventory?")]
        [SerializeField] protected bool m_RemoveItemsOnPickup;

        protected Inventory m_Inventory;
        protected ItemIdentifierAmount[] m_ItemDefinitionAmounts;

        /// <summary>
        /// Initialize the default values.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            m_Inventory = gameObject.GetCachedComponent<Inventory>();

            // Convert from ItemAmounts to ItemIdentifierAmounts.
            var pickupItems = m_Inventory.MainItemCollection.GetAllItemStacks();
            m_ItemDefinitionAmounts = new ItemIdentifierAmount[pickupItems.Count];
            for (int i = 0; i < pickupItems.Count; ++i) {
                m_ItemDefinitionAmounts[i] = new ItemIdentifierAmount(pickupItems[i].Item.ItemDefinition, pickupItems[i].Amount);
            }
        }

        /// <summary>
        /// Returns the ItemDefinitionAmount that the ItemPickup contains.
        /// </summary>
        /// <returns>The ItemDefinitionAmount that the ItemPickup contains.</returns>
        public override ItemIdentifierAmount[] GetItemDefinitionAmounts()
        {
            return m_ItemDefinitionAmounts;
        }

        /// <summary>
        /// Sets the ItemPickup ItemDefinitionAmount value.
        /// </summary>
        /// <param name="itemDefinitionAmounts">The ItemDefinitionAmount that should be set.</param>
        public override void SetItemDefinitionAmounts(ItemIdentifierAmount[] itemDefinitionAmounts)
        {
            m_ItemDefinitionAmounts = itemDefinitionAmounts;
            m_Inventory.MainItemCollection.RemoveAll();
            for (int i = 0; i < m_ItemDefinitionAmounts.Length; ++i) {
                if (!(m_ItemDefinitionAmounts[i].ItemDefinition is ItemDefinition)) {
                    Debug.LogWarning($"Warning: Unable to drop {m_ItemDefinitionAmounts[i].ItemDefinition} because it isn't an Ultimate Inventory System Item Definition.");
                    continue;
                }
                m_Inventory.MainItemCollection.AddItem(InventorySystemManager.CreateItem(m_ItemDefinitionAmounts[i].ItemDefinition as ItemDefinition, null), m_ItemDefinitionAmounts[i].Amount);
            }
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
            // The item can't be equipped if the use or reload abilities are active.
            var characterLocomotion = inventory.gameObject.GetCachedComponent<Character.UltimateCharacterLocomotion>();
            if (characterLocomotion.IsAbilityTypeActive<Character.Abilities.Items.Use>() || characterLocomotion.IsAbilityTypeActive<Character.Abilities.Items.Reload>()) {
                return false;
            }
            
            var pickupItems = m_Inventory.AllItemInfos;
            if (pickupItems.Count == 0) { return false;}
            
            var bridgeInventory = character.GetCachedComponent<CharacterInventoryBridge>();
            var inventorySystemInventory = character.GetCachedComponent<Inventory>();
            for (int i = pickupItems.Count - 1; i >= 0; --i) {
                var itemInfo = pickupItems[i];

                if (m_RemoveItemsOnPickup) {
                    itemInfo = m_Inventory.RemoveItem(itemInfo);
                }
                
                if (m_PickupItemCopies) {
                    itemInfo = new ItemInfo((InventorySystemManager.CreateItem(itemInfo.Item), itemInfo.Amount), itemInfo.ItemCollection);
                } else if(!m_RemoveItemsOnPickup){
                    //Remove the item and replace it by a copy.
                    itemInfo = m_Inventory.RemoveItem(itemInfo);
                    m_Inventory.AddItem((ItemInfo) (InventorySystemManager.CreateItem(itemInfo.Item), itemInfo.Amount));
                }
                
                itemInfo = inventorySystemInventory.AddItem(itemInfo);
                if (m_EquipOnPickup && bridgeInventory.EquippableCategory.InherentlyContains(itemInfo.Item)) {
                    bridgeInventory.MoveEquip(itemInfo,true);
                }
            }
            return true;
        }
    }
}