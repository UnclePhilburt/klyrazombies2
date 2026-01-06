/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem
{
    using Opsive.Shared.Game;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using Opsive.UltimateInventorySystem.UI.Item.ItemViewModules;
    using Opsive.UltimateInventorySystem.UI.Views;
    using System;
    using Opsive.UltimateInventorySystem.Input;
    using UnityEngine;
    using UnityEngine.UI;

    public class CharacterEquippedSelectItemViewModule : ItemViewModuleSelectable
    {
        public enum EquippedState
        {
            Unequipped,
            SoftEquipped,
            ActiveEquipped
        }

        [Tooltip("The image that changes when equipped.")]
        [SerializeField] protected Image m_Image;
        [Tooltip("The default sprite.")]
        [SerializeField] protected Sprite m_Default;
        [Tooltip("The selected sprite.")]
        [SerializeField] protected Sprite m_Selected;
        [Tooltip("The equipped sprite.")]
        [SerializeField] protected Sprite m_SoftEquipped;
        [Tooltip("The equipped and selected sprite.")]
        [SerializeField] protected Sprite m_SoftEquippedSelected;
        [Tooltip("The equipped sprite.")]
        [SerializeField] protected Sprite m_ActiveEquipped;
        [Tooltip("The equipped and selected sprite.")]
        [SerializeField] protected Sprite m_ActiveEquippedSelected;

        protected EquippedState m_EquippedState;
        protected bool m_IsSelected = false;

        /// <summary>
        /// Change the background when selected.
        /// </summary>
        /// <param name="select">Should the image be selected?</param>
        public override void Select(bool select)
        {
            m_IsSelected = select;

            switch (m_EquippedState) {
                case EquippedState.Unequipped:
                    m_Image.sprite = m_IsSelected ? m_Selected : m_Default;
                    break;
                case EquippedState.SoftEquipped:
                    m_Image.sprite = m_IsSelected ? m_SoftEquippedSelected :m_SoftEquipped;
                    break;
                case EquippedState.ActiveEquipped:
                    m_Image.sprite = m_IsSelected ? m_ActiveEquippedSelected : m_ActiveEquipped;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Sets the item infos.
        /// </summary>
        /// <param name="info">Contains the info about the item.</param>
        public override void SetValue(ItemInfo info)
        {
            if (info.Item == null) {
                Clear();
                return;
            }

            var inventory = info.Inventory;

            if (info.ItemCollection == null) {
                Clear();
                return;
            }
            
            if (inventory == null) {
                Clear();
                return;
            }

            var inventoryBridge = inventory.gameObject.GetCachedComponent<CharacterInventoryBridge>();
            if (inventoryBridge == null) {
                Clear();
                return;
            }
            
            var characterItem = inventoryBridge.GetCharacterItem(info.Item);
            if (characterItem == null) {
                Clear();
                return;
            }

            m_EquippedState = inventoryBridge.IsItemActive(info.Item) ? EquippedState.ActiveEquipped
                : inventoryBridge.BridgeItemCollections.HasItem(info.Item, false) ? EquippedState.SoftEquipped 
                : EquippedState.Unequipped;

            Select(m_IsSelected);
        }

        /// <summary>
        /// Clear the Item View.
        /// </summary>
        public override void Clear()
        {
            m_EquippedState = EquippedState.Unequipped;
            base.Clear();
        }
    }
}