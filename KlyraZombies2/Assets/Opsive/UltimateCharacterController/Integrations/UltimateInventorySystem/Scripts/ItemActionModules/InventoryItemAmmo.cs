/// ---------------------------------------------
/// Ultimate Character Controller Integration
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem
{
    using System;
    using System.Collections.Generic;
    using Opsive.Shared.Game;
    using Opsive.Shared.Inventory;
    using Opsive.UltimateCharacterController.Inventory;
    using Opsive.UltimateCharacterController.Items.Actions;
    using Opsive.UltimateCharacterController.Items.Actions.Modules;
    using Opsive.UltimateCharacterController.Items.Actions.Modules.Shootable;
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.AttributeSystem;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using UnityEngine;
    using ItemCollection = Opsive.UltimateInventorySystem.Core.InventoryCollections.ItemCollection;
    using Inventory = Opsive.UltimateInventorySystem.Core.InventoryCollections.Inventory;

    /// <summary>
    /// Uses Items inside the Inventory to get ammo for the shootable action.
    /// </summary>
    [Serializable]
    public class InventoryItemAmmo : ShootableAmmoModule, IModuleGetItemIdentifiersToDrop
    {
        [Tooltip("Use AmmoData or use the Ammo ItemDefinition directly?")]
        [SerializeField] protected bool m_UseAmmoData = false;
        [Tooltip("Ammo Data Attribute Name (Optional).")]
        [SerializeField] protected string m_AmmoDataAttributeName = "AmmoData";
        [Tooltip("The name of the item Collection where the ammo resides.")]
        [SerializeField] protected string[] m_AmmoItemCollectionNames;
        [Tooltip("The ItemIdentifier that is consumed by the item.")]
        [SerializeField] protected DynamicItemDefinition m_AmmoItemDefinition;
        [Tooltip("Choose how what quantity of ammo item gets dropped when the character item is dropped.")]
        [SerializeField] protected ItemAmmo.DropOptions m_DropOption = ItemAmmo.DropOptions.AmmoLeft;
        [Tooltip("Is the ammo shared with other items?")]
        [SerializeField] protected bool m_SharedAmmoItemIdentifier = false;

        public bool UseAmmoData { get { return m_UseAmmoData; } set { m_UseAmmoData = value; } }
        public string AmmoDataAttributeName { get { return m_AmmoDataAttributeName; } set { m_AmmoDataAttributeName = value; } }
        public bool SharedAmmoItemIdentifier { get { return m_SharedAmmoItemIdentifier; } set { m_SharedAmmoItemIdentifier = value; } }
        public override ItemDefinitionBase AmmoItemDefinition { get { return m_AmmoItemDefinition; } set { m_AmmoItemDefinition = value as ItemDefinition; } }
        public ItemAmmo.DropOptions DropOption { get => m_DropOption; set => m_DropOption = value; }
    
        protected ItemCollectionGroup m_AmmoItemCollections;
        protected Item m_AmmoItem;
        protected Inventory m_Inventory;
        
        protected AmmoData m_AmmoData;
        protected ItemObject m_ItemObject;
        protected Item m_WeaponItem;
        protected Attribute<AmmoData> m_AmmoDataAttribute;

        public ItemObject ItemObject => m_ItemObject;
        public Item WeaponItem => m_WeaponItem;

        /// <summary>
        /// Initialize the module.
        /// </summary>
        /// <param name="itemAction">The parent Item Action.</param>
        protected override void Initialize(CharacterItemAction itemAction)
        {
            base.Initialize(itemAction);
            m_Inventory = Character.GetCachedComponent<Inventory>();
        
            m_AmmoItemCollections = new ItemCollectionGroup();
            m_AmmoItemCollections.ItemCollections = new List<ItemCollection>();
            if (m_AmmoItemCollectionNames == null || m_AmmoItemCollectionNames.Length == 0) {
                // Use all the Main ItemCollection if there is no specified ItemCollection
                m_AmmoItemCollections.AddItemCollection(m_Inventory.MainItemCollection);
            } else {
                for (int i = 0; i < m_AmmoItemCollectionNames.Length; i++) {
                    var ammoItemCollection = GetItemCollection(m_AmmoItemCollectionNames[i], true);
                    if(ammoItemCollection == null){continue;}
                    m_AmmoItemCollections.AddItemCollection(ammoItemCollection);
                }
            }
            
            //TODO get AmmoData?
            
            
            var ammoDefinition = m_AmmoItemDefinition.Value;
            if (ammoDefinition != null) { m_AmmoItem = ammoDefinition.CreateItemIdentifier() as Item; }
        }
    
        /// <summary>
        /// Returns the ItemCollection with the specified name.
        /// </summary>
        /// <param name="collectionName">The name of the ItemCollection.</param>
        /// <param name="errorIfNull">Should an error be logged if the ItemCollection cannot be found?</param>
        /// <returns>The ItemCollection with the specified name.</returns>
        private ItemCollection GetItemCollection(string collectionName, bool errorIfNull)
        {
            var itemCollection = m_Inventory.GetItemCollection(collectionName);
            if (errorIfNull && itemCollection == null) {
                Debug.LogWarning($"Error: Unable to find the Item Collection with name {collectionName} within the Character Inventory.");
            }
            return itemCollection;
        }

        /// <summary>
        /// Updates the registered events when the item is equipped and the module is enabled.
        /// </summary>
        protected override void UpdateRegisteredEventsInternal(bool register)
        {
            base.UpdateRegisteredEventsInternal(register);
            Opsive.Shared.Events.EventHandler.RegisterUnregisterEvent<ItemInfo, ItemStack>(register,
                Character, EventNames.c_Inventory_OnAdd_ItemInfo_ItemStack,
                OnAddItem);
            Opsive.Shared.Events.EventHandler.RegisterUnregisterEvent<ItemInfo>(register,
                Character, EventNames.c_Inventory_OnRemove_ItemInfo,
                OnRemoveItem);
            
            if (register) {
                var itemObject = ShootableAction.gameObject.GetCachedComponent<ItemObject>();
                SetItemObject(itemObject);
            }
        }
        
        /// <summary>
        /// Set the item object.
        /// </summary>
        /// <param name="itemObject">The item object.</param>
        public virtual void SetItemObject(ItemObject itemObject)
        {
            if (m_ItemObject != null) {
                Opsive.Shared.Events.EventHandler.UnregisterEvent(m_ItemObject, EventNames.c_ItemObject_OnItemChanged, SetItem);
            }

            m_ItemObject = itemObject;
            if (m_ItemObject == null) { return; }

            Opsive.Shared.Events.EventHandler.RegisterEvent(m_ItemObject, EventNames.c_ItemObject_OnItemChanged, SetItem);
            SetItem();
        }
        
        /// <summary>
        /// Set the item from the ItemObject.
        /// </summary>
        protected void SetItem()
        {
            if (m_ItemObject.isActiveAndEnabled) {
                SetItem(m_ItemObject.Item);
            } else {
                SetItem(null);
            }
        }
        
        
        /// <summary>
        /// Set the item.
        /// </summary>
        /// <param name="item">The item.</param>
        private void SetItem(Item item)
        {
            
            
            if (m_WeaponItem == item && m_WeaponItem == null) {
                m_AmmoDataAttribute = null;
                return;
            }
            
            m_AmmoData = AmmoData.None;

            m_WeaponItem = item;
            // Don't continue if the ammo does not need to be set.
            if(m_UseAmmoData == false){ return; }
            
            if (m_WeaponItem == null || m_WeaponItem.IsInitialized == false) {
                m_WeaponItem = null;
                m_AmmoDataAttribute = null;
                return;
            }

            m_AmmoDataAttribute = m_WeaponItem.GetAttribute<Attribute<AmmoData>>(m_AmmoDataAttributeName);
            if (m_AmmoDataAttribute == null) {
                Debug.LogWarning($"The AmmoData Attribute with name {m_AmmoDataAttributeName} was not found on item {m_WeaponItem}");
                return;
            }

            m_AmmoData = m_AmmoDataAttribute.GetValue();
            SetConsumableItemIdentifier(m_AmmoData.ItemDefinition?.CreateItemIdentifier());
        }

        private void OnAddItem(ItemInfo itemInfo, ItemStack itemStack)
        {
            if(itemStack == null){ return; }
            if(itemStack.Item != m_AmmoItem){ return; }
            NotifyAmmoChange();
        }

        private void OnRemoveItem(ItemInfo itemInfo)
        {
            if(itemInfo == null){ return; }
            if(itemInfo.Item != m_AmmoItem){ return; }
            NotifyAmmoChange();
        }

        /// <summary>
        /// Set the ItemIdentifier which can be consumed by the item.
        /// </summary>
        /// <param name="itemIdentifier">The new ItemIdentifier which can be consumed by the item.</param>
        public virtual void SetConsumableItemIdentifier(IItemIdentifier itemIdentifier)
        {
            if (m_AmmoItem == itemIdentifier) { return; }

            var previousAmmoIdentifier = m_AmmoItem;
            m_AmmoItem = itemIdentifier as Item;
            m_AmmoItemDefinition = new DynamicItemDefinition(m_AmmoItem?.ItemDefinition);

            // Add back the previous consumable item to the inventory.
            var itemInfo = new ItemInfo(previousAmmoIdentifier, ShootableAction.ClipRemainingCount);
            m_AmmoItemCollections.AdjustItem(itemInfo);
            // Set the ClipRemaining to 0 so the new consumable item can be loaded from the inventory.
            ShootableAction.MainClipModule.EmptyClip(false);

            NotifyAmmoChange();

            ShootableAction.ReloadItem(false);
        }

        /// <summary>
        /// Is there any ammo left to be used?
        /// </summary>
        /// <returns>True if there is still ammo left.</returns>
        public override bool HasAmmoRemaining()
        {
            if (m_AmmoItemCollections.GetItemAmount(m_AmmoItem) == 0) { return false; }

            return true;
        }

        /// <summary>
        /// Is the ammo shared between multiple items.
        /// </summary>
        /// <returns>True if the ammo is shared with other items.</returns>
        public override bool IsAmmoShared()
        {
            return m_SharedAmmoItemIdentifier;
        }

        /// <summary>
        /// Check if the same ammo is shared between the two modules.
        /// </summary>
        /// <param name="otherAmmoModule">The other Ammo Module to compare if the ammo match.</param>
        /// <returns>True if both modules share the same ammo.</returns>
        public override bool DoesAmmoSharedMatch(ShootableAmmoModule otherAmmoModule)
        {
            if (m_SharedAmmoItemIdentifier == false || otherAmmoModule.IsAmmoShared() == false) { return false; }

            if (!(otherAmmoModule is InventoryItemAmmo itemAmmo)) { return false; }

            return m_AmmoItemDefinition == itemAmmo.AmmoItemDefinition;
        }

        /// <summary>
        /// Get the number of unloaded ammo left.
        /// </summary>
        /// <returns></returns>
        public override int GetAmmoRemainingCount()
        {
            return m_AmmoItemCollections.GetItemAmount(m_AmmoItem);
        }

        /// <summary>
        /// Create an ammo data which can be cached.
        /// </summary>
        /// <returns>The new shootable ammo data to cache.</returns>
        public override ShootableAmmoData CreateAmmoData()
        {
            return new ShootableAmmoData(this,
                -1, -1, m_AmmoItem, null);
        }

        /// <summary>
        /// Load the ammo within the clip ammo list.
        /// </summary>
        /// <param name="clipRemaining">The clip remaining list of ammo.</param>
        /// <param name="reloadAmount">The number of ammo to reload inside the clip list.</param>
        /// <param name="removeAmmoWhenLoaded">Remove the ammo when it is loaded in the clip list?</param>
        public override void LoadAmmoIntoList(List<ShootableAmmoData> clipRemaining, int reloadAmount,
            bool removeAmmoWhenLoaded)
        {
            if (reloadAmount <= 0) { return; }

            for (int i = 0; i < reloadAmount; i++) {
                var ammoData = CreateAmmoData();
                clipRemaining.Add(ammoData);
            }

            if (removeAmmoWhenLoaded) {
                var removeItemInfo = new ItemInfo(m_AmmoItem, -reloadAmount);
                m_AmmoItemCollections.AdjustItem(removeItemInfo);
                NotifyAmmoChange();
            }
        }

        /// <summary>
        /// Adjust the ammo amount by adding the amount (negative to remove ammo).
        /// </summary>
        /// <param name="amount">The amount to adjust the ammo by.</param>
        public override void AdjustAmmoAmount(int amount)
        {
            var addItemInfo = new ItemInfo(m_AmmoItem, amount);
            m_AmmoItemCollections.AdjustItem(addItemInfo);
            Inventory.AdjustItemIdentifierAmount(m_AmmoItem, amount);
            NotifyAmmoChange();
        }

        /// <summary>
        /// Get the items to drop by adding it to the list.
        /// </summary>
        /// <param name="itemsToDrop">The list of items to drop, the item to drop will be added to this list.</param>
        public void GetItemIdentifiersToDrop(List<ItemIdentifierAmount> itemsToDrop)
        {
            var amount = -1;
            switch (m_DropOption) {
                case ItemAmmo.DropOptions.Nothing:
                    return;
                case ItemAmmo.DropOptions.All:
                    amount = GetAmmoRemainingCount() + ShootableAction.ClipRemainingCount;
                    break;
                case ItemAmmo.DropOptions.Clip:
                    amount = ShootableAction.ClipRemainingCount;
                    break;
                case ItemAmmo.DropOptions.AmmoLeft:
                    amount = GetAmmoRemainingCount();
                    break;
            }

            if (amount == -1) { return; }

            itemsToDrop.Add(new ItemIdentifierAmount(m_AmmoItem, amount));
        }
    }
}