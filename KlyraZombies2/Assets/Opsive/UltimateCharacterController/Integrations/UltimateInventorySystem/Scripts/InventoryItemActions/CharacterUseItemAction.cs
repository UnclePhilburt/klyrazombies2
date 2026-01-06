/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem
{
    using System.Collections;
    using Opsive.Shared.Events;
    using Opsive.Shared.Game;
    using Opsive.Shared.Inventory;
    using Opsive.UltimateCharacterController.Character;
    using Opsive.UltimateCharacterController.Character.Abilities.Items;
    using Opsive.UltimateCharacterController.Inventory;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using Opsive.UltimateInventorySystem.ItemActions;
    using UnityEngine;
    using UnityEngine.Serialization;

    /// <summary>
    /// Item action which will instantly use the specified item.
    /// </summary>
    [System.Serializable]
    public class CharacterUseItemAction : ItemAction
    {
        [SerializeField] protected int m_EquippableItemCollectionIndex = -1;
        [FormerlySerializedAs("m_SlotID")] [SerializeField] protected int m_EquippableSlotID = -1;

        public int EquippableItemCollectionIndex
        {
            get => m_EquippableItemCollectionIndex;
            set => m_EquippableItemCollectionIndex = value;
        }

        public int SlotID
        {
            get => m_EquippableSlotID;
            set => m_EquippableSlotID = value;
        }

        private Opsive.UltimateInventorySystem.Core.Item m_Item;
        private ItemUser m_ItemUser;
        
        /// <summary>
        /// Default constructor.
        /// </summary>
        public CharacterUseItemAction()
        {
            m_Name = "UseItem";
        }

        /// <summary>
        /// Returns true if the item action be invoked.
        /// </summary>
        /// <param name="itemInfo">The item.</param>
        /// <param name="itemUser">The inventory user.</param>
        /// <returns>True if it can be invoked.</returns>
        protected override bool CanInvokeInternal(ItemInfo itemInfo, ItemUser itemUser)
        {
            var characterLocomotion = itemUser.gameObject.GetCachedComponent<UltimateCharacterLocomotion>();
            if (characterLocomotion == null || !characterLocomotion.Alive) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Instantly uses the item.
        /// </summary>
        /// <param name="itemInfo">The item info.</param>
        /// <param name="itemUser">The user that has the item.</param>
        protected override void InvokeActionInternal(ItemInfo itemInfo, ItemUser itemUser)
        {
            var inventoryBridge = itemUser.gameObject.GetCachedComponent<CharacterInventoryBridge>();
            if (inventoryBridge == null) {
                return;
            }

            var itemSetManager = inventoryBridge.InventoryItemSetManager;

            // If the item is already equipped it can be used immediately.
            if (inventoryBridge.BridgeItemCollections.HasItem(itemInfo)) {
                UseItem(itemInfo, itemUser);
                return;
            }

            // The item is not equipped. Equip the item and use the item as soon as it is equipped.
            m_Item = itemInfo.Item;
            m_ItemUser = itemUser;

            EventHandler.RegisterEvent<ItemAbility, bool>(itemUser.gameObject, "OnCharacterItemAbilityActive", OnItemAbilityActive);
            inventoryBridge.MoveEquip(itemInfo, m_EquippableItemCollectionIndex, m_EquippableSlotID, true);
        }

        /// <summary>
        /// The character's item ability has been started or stopped.
        /// </summary>
        /// <param name="itemAbility">The item ability which was started or stopped.</param>
        /// <param name="active">True if the ability was started, false if it was stopped.</param>
        private void OnItemAbilityActive(ItemAbility itemAbility, bool active)
        {
            if (active || !((itemAbility is EquipUnequip) || (itemAbility is Use))) {
                return;
            }
            
            var inventory = m_ItemUser.gameObject.GetCachedComponent<Opsive.UltimateInventorySystem.Core.InventoryCollections.Inventory>();
            var equippedItemInfo = inventory.GetItemInfo(m_Item);

            if (equippedItemInfo.HasValue == false) {
                Debug.LogWarning("The item failed to equip.");
                return;
            }
            
            var inventoryBridge = m_ItemUser.gameObject.GetCachedComponent<CharacterInventoryBridge>();

            if (itemAbility is EquipUnequip) {
                // The item has been equipped. Use the item.
                UseItemDelayed(equippedItemInfo.Value, m_ItemUser);
            } else {
                EventHandler.UnregisterEvent<ItemAbility, bool>(m_ItemUser.gameObject, "OnCharacterItemAbilityActive", OnItemAbilityActive);
                // Unequip the item after use.
                inventoryBridge.MoveEquip(equippedItemInfo.Value, m_EquippableItemCollectionIndex, m_EquippableSlotID, false);
            }
        }

        /// <summary>
        /// Uses the specified item after a frame.
        /// </summary>
        /// <param name="itemInfo">The item info.</param>
        /// <param name="itemUser">The user that has the item.</param>
        private void UseItemDelayed(ItemInfo itemInfo, ItemUser itemUser)
        {
            itemUser.StartCoroutine(UseItemIE(itemInfo, itemUser));
        }

        /// <summary>
        /// Uses the specified item after a frame.
        /// </summary>
        /// <param name="itemInfo">The item info.</param>
        /// <param name="itemUser">The user that has the item.</param>
        protected IEnumerator UseItemIE(ItemInfo itemInfo, ItemUser itemUser)
        {
            // Wait a frame to initialize the awake and start functions of the item.
            yield return null;

            UseItem(itemInfo, itemUser);
        }

        /// <summary>
        /// Uses the specified item.
        /// </summary>
        /// <param name="itemInfo">The item info.</param>
        /// <param name="itemUser">The user that has the item.</param>
        protected virtual void UseItem(ItemInfo itemInfo, ItemUser itemUser)
        {
            var characterLocomotion = itemUser.gameObject.GetCachedComponent<UltimateCharacterLocomotion>();
            var useAbilities = characterLocomotion.GetAbilities<Use>();
            if (useAbilities == null) {
                return;
            }

            var inventoryBridge = itemUser.gameObject.GetCachedComponent<CharacterInventoryBridge>();

            var characterItem = inventoryBridge.GetCharacterItem(itemInfo.Item);
            if (characterItem == null) {
                Debug.LogWarning("The character items is null");
                return;
            }

            var itemSlotID = characterItem.SlotID;

            // Find the correct Use ability.
            Use useAbility = null;
            for (int i = 0; i < useAbilities.Length; ++i) {
                if (useAbilities[i].SlotID == itemSlotID) {
                    useAbility = useAbilities[i];
                    break;
                }
            }

            if (useAbility == null) {
                return;
            }

            // Start the Use ability.
            characterLocomotion.TryStartAbility(useAbility);
        }
    }
}