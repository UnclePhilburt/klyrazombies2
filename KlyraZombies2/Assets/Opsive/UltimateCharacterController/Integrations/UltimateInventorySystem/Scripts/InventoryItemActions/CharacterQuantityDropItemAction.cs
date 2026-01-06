/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem
{
    using Opsive.Shared.Events;
    using Opsive.Shared.Game;
    using Opsive.UltimateCharacterController.Character;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.ItemActions;
    using Opsive.UltimateInventorySystem.UI.Panels;

    /// <summary>
    /// Simple item action used to drop items.
    /// </summary>
    [System.Serializable]
    public class CharacterQuantityDropItemAction : ItemActionWithQuantityPickerPanel
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public CharacterQuantityDropItemAction()
        {
            m_Name = "Drop";
        }

        /// <summary>
        /// Check if the action can be invoked.
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
            if (characterLocomotion != null && !characterLocomotion.Alive) {
                return false;
            }

            var itemCollection = itemInfo.ItemCollection;
            return itemCollection?.HasItem(itemInfo.Item) ?? false;
        }

        /// <summary>
        /// Invoke the action.
        /// </summary>
        /// <param name="itemInfo">The item info.</param>
        /// <param name="itemUser">The item user (can be null).</param>
        protected override void InvokeActionInternal(ItemInfo itemInfo, ItemUser itemUser)
        {
            if (itemInfo.Amount == 1) {
                InvokeWithAwaitedValue(itemInfo, itemUser, 1);
                return;
            }

            base.InvokeActionInternal(itemInfo, itemUser);
        }

        /// <summary>
        /// Setup the quantity picker before it is used.
        /// </summary>
        /// <param name="itemInfo">The item info.</param>
        /// <param name="itemUser">The item user (can be null).</param>
        /// <param name="quantityPickerPanel">The quantity picker panel.</param>
        protected override void SetupQuantityPickerSettings(ItemInfo itemInfo, ItemUser itemUser, QuantityPickerPanel quantityPickerPanel)
        {
            quantityPickerPanel.QuantityPicker.MinQuantity = 1;
            quantityPickerPanel.QuantityPicker.MaxQuantity = itemInfo.Amount;
        }

        /// <summary>
        /// Invoke with the action with the awaited value. 
        /// </summary>
        /// <param name="itemInfo">The itemInfo.</param>
        /// <param name="itemUser">The item user.</param>
        /// <param name="awaitedValue">The value that was waited for.</param>
        protected override void InvokeWithAwaitedValue(ItemInfo itemInfo, ItemUser itemUser, int awaitedValue)
        {
            if (awaitedValue <= 0) { return; }

            itemInfo = (awaitedValue, itemInfo);
            
            var inventorySystemBridge = itemUser.gameObject.GetCachedComponent<CharacterInventoryBridge>();
            inventorySystemBridge.DropItem(itemInfo,false,true);
        }
    }
}