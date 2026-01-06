/// ---------------------------------------------
/// Ultimate Inventory System.
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem
{
    using Opsive.Shared.Events;
    using Opsive.Shared.Game;
    using Opsive.UltimateCharacterController.Character;
    using Opsive.UltimateInventorySystem.Core.AttributeSystem;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.ItemActions;
    using UnityEngine;

    /// <summary>
    /// Item action used to Move an item from one collection to another. It can be used to equip/unequip items too.
    /// </summary>
    [System.Serializable]
    public class CharacterEquipUnequipItemAction : ItemAction
    {
        [Tooltip("The name that should be displayed when the item can be equipped.")]
        [SerializeField] protected string m_EquipActionName = "Equip";
        [Tooltip("The name that should be displayed when the item can be unequipped.")]
        [SerializeField] protected string m_UnequipActionName = "Unequip";
        [Tooltip("Actively Equip the item if it is soft Equipped but not part of the active item set.")]
        [SerializeField] protected bool m_NotMoveEquip = false;

        private bool m_Equip;
        protected bool m_MoveEquip;
        
        /// <summary>
        /// Default constructor.
        /// </summary>
        public CharacterEquipUnequipItemAction()
        {
            m_Name = "Equip";
        }

        /// <summary>
        /// Returns true if the item action be invoked.
        /// </summary>
        /// <param name="itemInfo">The item.</param>
        /// <param name="itemUser">The inventory user.</param>
        /// <returns>True if it can be invoked.</returns>
        protected override bool CanInvokeInternal(ItemInfo itemInfo, ItemUser itemUser)
        {
            if (itemUser == null) {
                return false;
            }
            
            var characterLocomotion = itemUser.gameObject.GetCachedComponent<UltimateCharacterLocomotion>();
            if (characterLocomotion == null || !characterLocomotion.Alive) {
                return false;
            }

            var inventorySystemBridge = itemUser.gameObject.GetCachedComponent<CharacterInventoryBridge>();
            if (inventorySystemBridge == null) {
                return false;
            }
            
            var item = itemInfo.Item;
            
            if (m_NotMoveEquip) {
                m_MoveEquip = false;
                m_Equip = !inventorySystemBridge.IsItemActive(item);
            } else {
                m_MoveEquip = true;
                m_Equip = !inventorySystemBridge.BridgeItemCollections.Contains(itemInfo.ItemCollection);
            }

            if (m_Equip) {
                m_Name = m_EquipActionName;
            } else {
                m_Name = m_UnequipActionName;
            }

            return true;
        }

        /// <summary>
        /// Move an item from one collection to another.
        /// </summary>
        /// <param name="itemInfo">The item info.</param>
        protected override void InvokeActionInternal(ItemInfo itemInfo, ItemUser itemUser)
        {
            var inventorySystemBridge = itemUser.gameObject.GetCachedComponent<CharacterInventoryBridge>();

            if (m_MoveEquip) {
                inventorySystemBridge.MoveEquip(itemInfo,m_Equip);
            } else {
                inventorySystemBridge.Equip(itemInfo,m_Equip);
            }
            
        }
    }
}
