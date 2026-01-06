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
    using Opsive.UltimateCharacterController.Items.Actions.Modules.Shootable;
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.AttributeSystem;
    using UnityEngine;

    /// <summary>
    /// Use an AmmoData Attribute on the Inventory Item to store the ammo itemDefinition and clip remaining.
    /// </summary>
    [Serializable]
    public class InventoryAmmoDataClip : ShootableClipModule
    {
        [Tooltip("The ammoData.")]
        [SerializeField] protected string m_AmmoDataAttributeName = "AmmoData";
        
        protected AmmoData m_AmmoData;
        protected List<ShootableAmmoData> m_ClipRemaining;
        
        protected int m_TotalReloadAmount;
        protected int m_TotalClipAmount;
        protected int m_TotalAmmoAmount;
        
        protected ItemObject m_ItemObject;
        protected Item m_Item;
        protected Attribute<AmmoData> m_AmmoDataAttribute;

        public ItemObject ItemObject => m_ItemObject;
        public Item Item => m_Item;

        public string AmmoDataAttributeName
        {
            get => m_AmmoDataAttributeName;
            set => m_AmmoDataAttributeName = value;
        }
        
        public override ItemDefinitionBase ItemDefinition
        {
            get {
                if (m_ClipRemaining != null && m_ClipRemaining.Count > 0) {
                    return m_ClipRemaining[0].AmmoModule.AmmoItemDefinition;
                }
                if (ShootableAction.MainAmmoModule != null && ShootableAction.MainAmmoModule.AmmoItemDefinition != null) {
                    return ShootableAction.MainAmmoModule.AmmoItemDefinition;
                }
                for (int i = 0; i < ShootableAction.AmmoModuleGroup.ModuleCount; ++i) {
                    var module = ShootableAction.AmmoModuleGroup.GetBaseModuleAt(i) as ShootableAmmoModule;
                    if (module != null && module.AmmoItemDefinition != null) {
                        return module.AmmoItemDefinition;
                    }
                }
                return null;
            }
            set {
                Debug.LogError("Error: The InventoryAmmoDataClip module cannot set the ItemDefinition.");
            }
        }

        /// <summary>
        /// The ammo data is used by the bound attribute to update the shootable weapon or attribute values.
        /// </summary>
        public AmmoData AmmoData {
            get { return m_AmmoData; }
            set
            {
                m_AmmoData = value;

                SetAmmoData(m_AmmoData, true);
            }
        }

        public List<ShootableAmmoData> ClipRemaining
        {
            get { return m_ClipRemaining; }
            private set
            {
                m_ClipRemaining = value;
                NotifyClipChange();
            }
        }

        public override int ClipSize 
        { 
            get { return m_AmmoData.ClipSize; }
            set { AmmoData = new AmmoData(m_AmmoData.ItemDefinition, value, m_AmmoData.ClipRemaining); }
        }

        public override int ClipRemainingCount => m_AmmoData.ClipRemaining;

        /// <summary>
        /// Initialize the module.
        /// </summary>
        /// <param name="itemAction">The parent Item Action.</param>
        protected override void Initialize(CharacterItemAction itemAction)
        {
            base.Initialize(itemAction);

            m_ClipRemaining = new List<ShootableAmmoData>();
        }

        /// <summary>
        /// Updates the registered events when the item is equipped and the module is enabled.
        /// </summary>
        protected override void UpdateRegisteredEventsInternal(bool register)
        {
            base.UpdateRegisteredEventsInternal(register);
            if (register) {
                var itemObject = ShootableAction.gameObject.GetCachedComponent<ItemObject>();
                SetItemObject(itemObject);
            }
        }

        /// <summary>
        /// Set the AmmoData.
        /// </summary>
        /// <param name="ammoData">The new AmmoData</param>
        /// <param name="refreshAmmoList">Refresh the list to match the clip remaining?</param>
        protected virtual void SetAmmoData(AmmoData ammoData, bool refreshAmmoList)
        {
            m_AmmoData = ammoData;

            if (refreshAmmoList && m_ClipRemaining != null) {
                m_ClipRemaining.Clear();
                for (int i = 0; i < ammoData.ClipRemaining; i++) {
                    m_ClipRemaining.Add(new ShootableAmmoData(ShootableAction.MainAmmoModule, i,0,
                        ammoData.ItemDefinition.CreateItemIdentifier(),null));
                }
            }

            if(m_AmmoDataAttribute == null){ return; }
            
            // only assign the value if it is different, otherwise it creates an infinite loop.
            var attributeAmmoData = m_AmmoDataAttribute.GetValue();
            if (attributeAmmoData.Equals(m_AmmoData) == false) {
                m_AmmoDataAttribute.SetOverrideValue(m_AmmoData);
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
            if (m_Item == item && m_Item == null) {
                m_AmmoDataAttribute = null;
                return;
            }
            
            m_AmmoData = AmmoData.None;

            m_Item = item;
            if (m_Item == null || m_Item.IsInitialized == false) {
                m_Item = null;
                m_AmmoDataAttribute = null;
                return;
            }

            m_AmmoDataAttribute = m_Item.GetAttribute<Attribute<AmmoData>>(m_AmmoDataAttributeName);
            if (m_AmmoDataAttribute == null) {
                Debug.LogWarning($"The AmmoData Attribute with name {m_AmmoDataAttributeName} was not found on item {m_Item}");
                return;
            }

            SetAmmoData(m_AmmoDataAttribute.GetValue(), true);
        }

        /// <summary>
        /// Get the ammo within the clip at the specified index.
        /// </summary>
        /// <param name="index">The ammo at the specific index.</param>
        /// <returns>The shootable ammo data at the index specified.</returns>
        public override ShootableAmmoData GetAmmoDataInClip(int index)
        {
            if (index < 0 || index >= m_ClipRemaining.Count) { return ShootableAmmoData.None; }

            if (m_ClipRemaining[index].Valid == false) { return ShootableAmmoData.None; }

            m_ClipRemaining[index] = m_ClipRemaining[index].CopyWithIndex(index);

            return m_ClipRemaining[index];
        }

        /// <summary>
        /// Notify that the ammo was used.
        /// </summary>
        /// <param name="amountUsed">The number of ammo that was used.</param>
        /// <param name="startIndex">The ammo index at which the ammo started to be used.</param>
        public override void AmmoUsed(int amountUsed, int startIndex = 0)
        {
            if (startIndex < 0 || startIndex >= m_ClipRemaining.Count) { return; }
            
            m_ClipRemaining.RemoveRange(startIndex, amountUsed);
            NotifyClipChange();
        }

        /// <summary>
        /// Notify that the ammo was removed.
        /// </summary>
        /// <param name="amountToRemove">The number of ammo that was removed.</param>
        /// <param name="startIndex">The ammo index at which the ammo started to be removed.</param>
        public override void RemoveAmmo(int amountToRemove, int startIndex = 0)
        {
            if (startIndex < 0 || startIndex >= m_ClipRemaining.Count) { return; }

            m_ClipRemaining.RemoveRange(startIndex, amountToRemove);
            NotifyClipChange();
        }

        /// <summary>
        /// Empty the clip completely.
        /// </summary>
        /// <param name="notify">Notify that the clip changed.</param>
        public override void EmptyClip(bool notify)
        {
            if(m_ClipRemaining.Count == 0){ return; }
            m_ClipRemaining.Clear();
            if (notify) {
                NotifyClipChange();
            }
        }

        /// <summary>
        /// Notify that the ammo within the clip has changed.
        /// </summary>
        public override void NotifyClipChange()
        {
            var newAmmoData = new AmmoData(m_AmmoData.ItemDefinition, m_AmmoData.ClipSize, m_ClipRemaining.Count);
            SetAmmoData(newAmmoData, false);
            
            base.NotifyClipChange();
        }

        /// <summary>
        /// Set the clip remaining manually.
        /// </summary>
        /// <param name="targetClipRemainingCount">The clip remaining count to set.</param>
        public override void SetClipRemaining(int targetClipRemainingCount)
        {
            var currentRemainingCount = ClipRemaining.Count;
            if (currentRemainingCount == targetClipRemainingCount) {
                return;
            }

            if (currentRemainingCount > targetClipRemainingCount) {
                ClipRemaining.RemoveRange(targetClipRemainingCount, currentRemainingCount - targetClipRemainingCount);
                NotifyClipChange();
                return;
            }
            
            // Add more.
            var reloadAmount = targetClipRemainingCount - currentRemainingCount;
            var ammoModule = ShootableAction.MainAmmoModule;
            if (ammoModule == null) {
                for (int i = 0; i < reloadAmount; i++) {
                    ClipRemaining.Add(new ShootableAmmoData(null, i +currentRemainingCount,0,null,null));
                }
            } else {
                ShootableAction.MainAmmoModule.LoadAmmoIntoList(ClipRemaining, reloadAmount, false);
            }
            
            NotifyClipChange();
        }

        /// <summary>
        /// Reload the clip.
        /// </summary>
        /// <param name="fullClip">Reload the clip completely?</param>
        public override void ReloadClip(bool fullClip)
        {
            var ammoLeft = ShootableAction.MainAmmoModule.GetAmmoRemainingCount();

            int reloadAmount;
            var clipRemainingCount = ClipRemainingCount;

            if (ShootableAction.MainAmmoModule.IsAmmoShared()) {
                DetermineTotalReloadAmount();

                if (m_TotalReloadAmount > m_TotalAmmoAmount) {
                    var totalAmount = m_TotalAmmoAmount + m_TotalClipAmount;

                    var activeSharedInstances = GetNumberOfSharedInstances();

                    // If there are multiple active consumable ItemIdentifiers then the reloaded count is shared across all of the ItemIdentifiers.
                    var targetAmount = fullClip
                        ? Mathf.CeilToInt(totalAmount / (float)activeSharedInstances)
                        : clipRemainingCount + 1;
                    reloadAmount = Mathf.Min(ammoLeft,
                        Mathf.Min(ClipSize - clipRemainingCount, targetAmount - clipRemainingCount));
                    
                } else {
                    // The Consumable ItemIdentifier doesn't need to be shared if there is plenty of ammo for all weapons.
                    reloadAmount = Mathf.Min(m_TotalAmmoAmount,
                        (fullClip ? (ClipSize - clipRemainingCount) : 1));
                }
            } else {
                // The consumable ItemIdentifier doesn't share with any other objects.
                reloadAmount = Mathf.Min(ammoLeft,
                    (fullClip ? (ClipSize - clipRemainingCount) : 1));
            }

            if (reloadAmount <= 0) {
                // Reloading negative amounts of ammo is not possible
                return;
            }
            
            ShootableAction.MainAmmoModule.LoadAmmoIntoList(ClipRemaining, reloadAmount, true);
            NotifyClipChange();
        }

        /// <summary>
        /// Determine the total amount of ammo to reload.
        /// </summary>
        protected virtual void DetermineTotalReloadAmount()
        {
            m_TotalReloadAmount = ClipSize - ClipRemainingCount;
            m_TotalClipAmount = ClipRemainingCount;
            if (ShootableAction.MainAmmoModule.IsAmmoShared()) {
                for (int i = 0; i < Inventory.SlotCount; ++i) {
                    var item = Inventory.GetActiveCharacterItem(i);
                    if (item != null) {
                        var shootableWeapons = item.gameObject.GetCachedComponents<ShootableAction>();
                        for (int j = 0; j < shootableWeapons.Length; ++j) {
                            if (shootableWeapons[j] == ShootableAction) { continue; }
                            
                            // Ignore if it was already reloaded.
                            if(shootableWeapons[j].HasReloaded()){ continue; }

                            m_TotalReloadAmount +=
                                shootableWeapons[j].ClipSize - shootableWeapons[j].ClipRemainingCount;
                            m_TotalClipAmount += shootableWeapons[j].ClipRemainingCount;
                        }
                    }
                }
            }

            m_TotalAmmoAmount = ShootableAction.MainAmmoModule.GetAmmoRemainingCount();
        }
        
        /// <summary>
        /// Get the number of items or modules sharing the ammo.
        /// </summary>
        /// <returns>The number of items or modules sharing the ammo.</returns>
        public int GetNumberOfSharedInstances()
        {
            var ammoModule = ShootableAction.MainAmmoModule;
            var isAmmoShared = ammoModule.IsAmmoShared();
            if (isAmmoShared == false) { return 1; }
            
            var inventory = Inventory;
            var sharedCount = 0;

            // Find other modules/item that use the same ammo.
            for (int i = 0; i < inventory.SlotCount; ++i) {
                var item = inventory.GetActiveCharacterItem(i);
                if (item == null) { 
                    continue;
                }

                // Find any ShootableWeapons that may be sharing the same Consumable ItemIdentifier.
                var itemActions = item.ItemActions;
                for (int j = 0; j < itemActions.Length; ++j) {
                    var otherShootableAction = itemActions[j] as ShootableAction;
                    if (otherShootableAction == null || ammoModule.DoesAmmoSharedMatch(otherShootableAction.MainAmmoModule) == false) {
                        continue;
                    }

                    // The Consumable ItemIdentifier doesn't need to be shared if there is plenty of ammo for all weapons.
                    var totalInventoryAmount = ammoModule.GetAmmoRemainingCount();
                    if (otherShootableAction.ClipSize + ShootableAction.ClipSize < totalInventoryAmount) {
                        continue;
                    }

                    sharedCount++;
                }
            }

            return sharedCount;
        }

        /// <summary>
        /// Clean up the module when it is destroyed.
        /// </summary>
        public override void OnDestroy()
        {
            base.OnDestroy();
            if (m_ItemObject != null) {
                Opsive.Shared.Events.EventHandler.UnregisterEvent(m_ItemObject, EventNames.c_ItemObject_OnItemChanged, SetItem);
            }
        }
    }
    
}
    