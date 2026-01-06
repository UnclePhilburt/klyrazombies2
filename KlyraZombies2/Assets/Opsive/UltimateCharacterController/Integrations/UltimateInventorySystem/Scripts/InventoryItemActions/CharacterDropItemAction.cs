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
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.ItemActions;

    /// <summary>
    /// Simple item action used to drop items.
    /// </summary>
    [System.Serializable]
    public class CharacterDropItemAction : ItemAction
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public CharacterDropItemAction()
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
        /// <param name="itemInfo">The item.</param>
        protected override void InvokeActionInternal(ItemInfo itemInfo, ItemUser itemUser)
        {
            var inventoryBridge = itemUser.gameObject.GetCachedComponent<CharacterInventoryBridge>();
            inventoryBridge.DropItem(itemInfo, false, true);
        }
    }
}