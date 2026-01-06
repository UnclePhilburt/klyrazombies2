using UnityEngine;

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem
{
    using Opsive.Shared.Game;
    using Opsive.Shared.Inventory;
    using Opsive.UltimateCharacterController.Items.Actions;
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.AttributeSystem;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using Opsive.UltimateInventorySystem.UI;
    using Opsive.UltimateInventorySystem.UI.CompoundElements;
    using Opsive.UltimateInventorySystem.UI.Item.ItemViewModules;
    using System;
    using Opsive.UltimateCharacterController.Items;
    using Opsive.UltimateCharacterController.Items.Actions.Modules.Shootable;
    using UnityEngine.Serialization;
    using UnityEngine.UI;
    using Text = Opsive.Shared.UI.Text;

    public class WeaponAmmoItemViewModule : ItemViewModule
    {
        [Tooltip("The Image for the Ammo Item Icon.")]
        [SerializeField] protected Image m_AmmoItemImage;
        [Tooltip("A reference to the text used for the usable item loaded count.")]
        [SerializeField] protected Text m_LoadedCount;
        [FormerlySerializedAs("m_MaxCount")]
        [Tooltip("A reference to the text used for the usable item unloaded count.")]
        [SerializeField] protected Text m_ClipSize;
        [Tooltip("A reference to the text used for the usable item total ammo left in the inventory.")]
        [SerializeField] protected Text m_UnloadedCount;
        [Tooltip("The action ID that the UI represents.")]
        [SerializeField] protected string m_AmmoDataAttributeName = "AmmoData";
        [Tooltip("The action ID that the UI represents.")]
        [SerializeField] protected bool m_DrawOnAmmoChange = true;
        [Tooltip("Hide on clear or when the attribute cannot be found.")]
        [SerializeField] protected GameObject m_HideOnClear;

        [NonSerialized] protected Item m_CachedItem;
        [NonSerialized] protected Inventory m_CachedInventory;
        
        public override void SetValue(ItemInfo info)
        {
            if (info.Item == null) {
                Clear();
                return;
            }
            
            if (m_DrawOnAmmoChange) { RegisterToItemAttributeChangeEvent(info); }
            DrawAmmo(info);
        }

        protected virtual void DrawAmmo(ItemInfo info)
        {
            if (m_HideOnClear != null) {
                m_HideOnClear.SetActive(true);
            }
            
            var inventory = info.Inventory;

            if (inventory == null) {
                UseAmmoAttribute(info);
                return;
            }

            var inventoryBridge = inventory.gameObject.GetCachedComponent<CharacterInventoryBridge>();
            if (inventoryBridge == null) {
                UseAmmoAttribute(info);
                return;
            }
            
            var characterItem = inventoryBridge.GetCharacterItem(info.Item);
            if (characterItem == null) {
                UseAmmoAttribute(info);
                return;
            }

            // Multiple item actions can be attached to the same item.
            var foundShootableWeapon = false;
            for (int i = 0; i < characterItem.ItemActions.Length; ++i) {
                var shootableWeapon = characterItem.ItemActions[i] as ShootableAction;
                if (shootableWeapon == null) { continue; }

                var consumableItemIdentifierAmount = shootableWeapon.ClipRemainingCount;
                // If the count is -1 then only the loaded should be shown.
                if (consumableItemIdentifierAmount == -1) {
                    Clear();
                    return;
                }

                foundShootableWeapon = true;

                if (m_AmmoItemImage != null) {
                    var itemAmmoModule = shootableWeapon.MainAmmoModule;
                    var ammoItem = itemAmmoModule?.AmmoItemDefinition as ItemDefinition;
                    if (ammoItem != null && ammoItem.TryGetAttributeValue("Icon", out Sprite icon)) {
                        m_AmmoItemImage.sprite = icon;
                    } else {
                        m_AmmoItemImage.sprite = null;
                    }
                }
                m_LoadedCount.text = shootableWeapon.ClipRemainingCount.ToString();
                m_ClipSize.text = shootableWeapon.ClipSize.ToString();
                m_UnloadedCount.text = shootableWeapon.MainAmmoModule?.GetAmmoRemainingCount().ToString();
                break;
            }

            if (foundShootableWeapon == false) {
                Clear();
                return;
            }
        }

        private void UseAmmoAttribute(ItemInfo info)
        {
            var inventoryItem = info.Item;
            var ammoDataAttribute = inventoryItem?.GetAttribute<Attribute<AmmoData>>(m_AmmoDataAttributeName);
            if (ammoDataAttribute == null) {
                Clear();
                return;
            }
            
            AmmoData ammoData = ammoDataAttribute.GetValue();

            if (m_AmmoItemImage != null) {
                var ammoItem = ammoData.ItemDefinition.DefaultItem;
                if (ammoItem != null && ammoItem.TryGetAttributeValue("Icon", out Sprite icon)) {
                    m_AmmoItemImage.sprite = icon;
                } else {
                    m_AmmoItemImage.sprite = null;
                }
            }
            m_LoadedCount.text = ammoData.ClipRemaining.ToString();
            m_ClipSize.text = ammoData.ClipSize.ToString();
            m_UnloadedCount.text = (info.Inventory as Inventory).GetItemAmount(ammoData.ItemDefinition).ToString();
        }

        private void RegisterToItemAttributeChangeEvent(ItemInfo itemInfo)
        {
            if (m_CachedItem == itemInfo.Item) { return; }
            
            if (m_CachedItem != null) {
                UnregisterToItemAttributeChangeEvent();
            }
            
            m_CachedItem = itemInfo.Item;
            m_CachedInventory = itemInfo.Inventory as Inventory;
            
            if(itemInfo.Item == null){return;}

            if (m_CachedInventory != null && m_CachedInventory.gameObject != null) {
                Shared.Events.EventHandler.RegisterEvent<CharacterItem, IItemIdentifier, int>(m_CachedInventory.gameObject, "OnItemUseConsumableItemIdentifier", OnUseConsumableItemIdentifier);
                Shared.Events.EventHandler.RegisterEvent<IItemIdentifier, int, int>(m_CachedInventory.gameObject, "OnInventoryAdjustItemIdentifierAmount", OnAdjustItemIdentifierAmount);
            }

            var ammoDataAttribute = itemInfo.Item.GetAttribute<Attribute<AmmoData>>(m_AmmoDataAttributeName);
            if (ammoDataAttribute == null) { return; }

            ammoDataAttribute.OnAttributeChanged += HandleAttributeValueChange;
        }

        private void OnAdjustItemIdentifierAmount(IItemIdentifier item, int previousAmount, int newAmount)
        {
            DrawAmmo(ItemInfo);
        }

        private void OnUseConsumableItemIdentifier(CharacterItem characterItem, IItemIdentifier item, int consumableAmount)
        {
            DrawAmmo(ItemInfo);
        }

        private void HandleAttributeValueChange(AttributeBase attribute)
        {
           DrawAmmo(ItemInfo);
        }

        private void UnregisterToItemAttributeChangeEvent()
        {
            if (m_CachedItem == null) { return; }

            var ammoDataAttribute = m_CachedItem.GetAttribute<Attribute<AmmoData>>(m_AmmoDataAttributeName);
            if (ammoDataAttribute == null) { return; }

            ammoDataAttribute.OnAttributeChanged -= HandleAttributeValueChange;

            if (m_CachedInventory != null && m_CachedInventory.gameObject != null) {
                Shared.Events.EventHandler.UnregisterEvent<CharacterItem, IItemIdentifier, int>(m_CachedInventory.gameObject, "OnItemUseConsumableItemIdentifier", OnUseConsumableItemIdentifier);
                Shared.Events.EventHandler.UnregisterEvent<IItemIdentifier, int, int>(m_CachedInventory.gameObject, "OnInventoryAdjustItemIdentifierAmount", OnAdjustItemIdentifierAmount);
            }

            m_CachedItem = null;
        }

        private void OnEnable()
        {
            if (m_DrawOnAmmoChange) { RegisterToItemAttributeChangeEvent(ItemInfo); }
        }

        private void OnDisable()
        {
            if (m_DrawOnAmmoChange) { UnregisterToItemAttributeChangeEvent(); }
        }

        public override void Clear()
        {
            if (m_DrawOnAmmoChange) { UnregisterToItemAttributeChangeEvent(); }

            if (m_AmmoItemImage != null) {
                m_AmmoItemImage.sprite = null;
            }

            m_LoadedCount.text = "???";
            m_ClipSize.text = "???";
            m_UnloadedCount.text = "???";
            
            if (m_HideOnClear != null) {
                m_HideOnClear.SetActive(false);
            }
        }
    }
}
