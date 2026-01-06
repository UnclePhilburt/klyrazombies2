/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

//#define DEBUG_BRIDGE

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem
{
    using System;
    using Opsive.Shared.Game;
    using Opsive.Shared.Inventory;
    using Opsive.Shared.Utility;
    using Opsive.UltimateCharacterController.Inventory;
    using Opsive.UltimateCharacterController.Items;
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.AttributeSystem;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using Opsive.UltimateInventorySystem.Storage;
    using System.Collections.Generic;
    using UnityEngine;

    using EventHandler = Opsive.Shared.Events.EventHandler;
    using Inventory = Opsive.UltimateInventorySystem.Core.InventoryCollections.Inventory;
    using ItemCollection = Opsive.UltimateInventorySystem.Core.InventoryCollections.ItemCollection;

    /// <summary>
    /// Provides an integration between the Ultimate Character Controller and Ultimate Inventory System.
    /// </summary>
    public class CharacterInventoryBridge : InventoryBase, IDatabaseSwitcher, IItemRestriction
    {

        [Serializable]
        public class RemoveOnDeathOptions
        {
            [Tooltip("The ItemCollections where the items should be removed from on death.")]
            [SerializeField] protected string[] m_ItemCollections;

            [Tooltip("The item categories that can be removed.")]
            [SerializeField] protected DynamicItemCategoryArray m_RemoveInclude;
            [Tooltip("The item categories that cannot be removed.")]
            [SerializeField] protected DynamicItemCategoryArray m_RemoveExceptions;
            [Tooltip("The item categories that can be dropped.")]
            [SerializeField] protected DynamicItemCategoryArray m_DropInclude;
            [Tooltip("The items that cannot be dropped.")]
            [SerializeField] protected DynamicItemCategoryArray m_DropExceptions;

            protected ItemCollectionGroup m_ItemCollectionGroup;
            protected CharacterInventoryBridge m_CharacterInventoryBridge;

            protected List<CharacterItem> m_CachedCharacterItems;


            /// <summary>
            /// Initialize the class.
            /// </summary>
            /// <param name="characterInventoryBridge">The characterInventory Bridge.</param>
            public void Initialize(CharacterInventoryBridge characterInventoryBridge)
            {
                m_CharacterInventoryBridge = characterInventoryBridge;
                m_CachedCharacterItems = new List<CharacterItem>();
                m_ItemCollectionGroup = new ItemCollectionGroup();
                m_ItemCollectionGroup.SetItemCollections(m_CharacterInventoryBridge.Inventory, m_ItemCollections, false, false);

            }

            /// <summary>
            /// Called when the character dies.
            /// </summary>
            public void OnDeath()
            {
                var itemInfos = m_ItemCollectionGroup.GetAllItemInfos();

                for (int i = itemInfos.Count - 1; i >= 0; i--) {
                    var itemInfo = itemInfos[i];
                    var item = itemInfo.Item;
                    

                    // Multiple items may be dropped at the same time.
                    if (itemInfo.ItemStack == null || itemInfo.ItemStack.Amount == 0 ||
                        itemInfo.ItemStack.Item == null) {
                        continue;
                    }

                    var amount = itemInfo.ItemStack.Amount;
                    itemInfo = new ItemInfo(amount, itemInfo);


                    if (m_DropInclude.InherentlyContains(item, false)
                        && !m_DropExceptions.InherentlyContains(item, false)) {
                        // drop item

                        RemoveOrDrop(itemInfo, true);

                        continue;
                    }
                    
                    if (m_RemoveInclude.InherentlyContains(item, false)
                        && !m_RemoveExceptions.InherentlyContains(item, false)) {
                        // remove item

                        RemoveOrDrop(itemInfo, false);
                        
                        continue;
                    }

                }
            }

            /// <summary>
            /// Remove or drop the item.
            /// </summary>
            /// <param name="itemInfo">The item info to remove or drop.</param>
            /// <param name="drop">Should the item be dropped?</param>
            private void RemoveOrDrop(ItemInfo itemInfo, bool drop)
            {
                var item = itemInfo.Item;
                var amount = itemInfo.Amount;
                
                m_CachedCharacterItems.Clear();
                var characterItems = m_CharacterInventoryBridge.GetCharacterItems(item, m_CachedCharacterItems);

                // if it is not a character item.
                if (characterItems.Count == 0) {

                    if (drop) {
                        m_CharacterInventoryBridge.DropItem(itemInfo, true, true);
                    } else {
                        m_CharacterInventoryBridge.Inventory.RemoveItem(itemInfo);
                    }

                    return;
                }
                
                // The item is a character item.
                for (int i = 0; i < characterItems.Count; i++) {
                    var slotID = characterItems[i].SlotID;
                    
                    while (m_CharacterInventoryBridge.GetItemIdentifierAmount(item) > 0 &&
                           m_CharacterInventoryBridge.CanRemoveItemIdentifier(item)) {
                        m_CharacterInventoryBridge.RemoveItemIdentifier(item, slotID, 1, drop);
                    }
                }
                
            }
        }

        struct AddingData
        {
            public bool Initialized;
            public IItemIdentifier ItemIdentifier;
            public int Amount;
        }
        
        struct RemovingData
        {
            public bool Initialized;
            public IItemIdentifier ItemIdentifier;
            public int Amount;
            public int SlotID;
            public bool Drop;
            public bool RemoveCharacterItem;
            public bool DestroyCharacterItem;
            public GameObject DropInstance;

            public static RemovingData None => new RemovingData();
            
            /// <summary>
            /// Remove an item amount from the inventory..
            /// </summary>
            /// <param name="itemIdentifier">The ItemIdentifier to remove.</param>
            /// <param name="slotID">The slot id in which to remove the item from.</param>
            /// <param name="amount">The amount of ItemIdentifier to adjust.</param>
            /// <param name="drop">Should the item be dropped?</param>
            /// <param name="removeCharacterItem">Should the character item be removed?</param>
            /// <param name="destroyCharacterItem">Should the character item be destroyed?</param>
            public RemovingData(IItemIdentifier itemIdentifier, int slotID, int amount, bool drop, bool removeCharacterItem,
                bool destroyCharacterItem)
            {
                Initialized = true;
                ItemIdentifier = itemIdentifier;
                Amount= amount;
                SlotID= slotID;
                Drop= drop;
                RemoveCharacterItem= removeCharacterItem;
                DestroyCharacterItem= destroyCharacterItem;
                DropInstance = null;
            }
        }
        
        [Tooltip("The ItemCategory that items that can be equipped are placed in.")]
        [SerializeField] protected DynamicItemCategory m_EquippableCategory;
        [Tooltip("The name of attribute containing the item prefabs for multi items.")]
        [SerializeField] protected string m_CharacterItemPrefabsAttributeName = "Prefabs";
        [Tooltip("The name of the ItemCollection that contains the items that are not equippable.")]
        [SerializeField] protected string m_DefaultItemCollectionName = "Default";
        [Tooltip("The name of the ItemCollection that contains the items that are equipped.")]
        [SerializeField] protected string[] m_BridgeItemCollectionNames = new []{"Equippable Slots", "Equippable"};
        [Tooltip("The name of the ItemCollection that specifies the items that should be loaded when the character spawns.")]
        [SerializeField] protected string[] m_LoadoutItemCollectionNames =  new []{ "Loadout" };
        [Tooltip("Specifies the prefab that should be used when an item is dropped, requires a Inventory Item Pickup and cannot have children.")]
        [SerializeField] protected bool m_DropUsingCharacterItemWhenPossible;
        [Tooltip("Specifies the prefab that should be used when an item is dropped, requires a Inventory Item Pickup and cannot have children.")]
        [SerializeField] protected GameObject m_InventoryItemPickupPrefab;
        [Tooltip("Specifies the offset compared to the character position as to where the drop item will spawn.")]
        [SerializeField] protected Vector3 m_ItemDropPositionOffset;
        [Tooltip("Remove and or drop items On Death options")]
        [SerializeField] protected RemoveOnDeathOptions m_RemoveOnDeathOptions;
        [Tooltip("The items that cannot be dropped.")]
        [SerializeField] protected DynamicItemCategoryArray m_RemoveExceptions;
        
        
        
        private ItemCollection m_DefaultItemCollection;
        private ItemCollectionGroup m_BridgeItemCollections;
        private ItemCollectionGroup m_LoadoutItemCollections;

        private Transform m_Transform;
        private Inventory m_Inventory;
#if UNITY_EDITOR
        private InventorySystemManager m_InventorySystemManager;
#endif
        private Character.UltimateCharacterLocomotion m_CharacterLocomotion;
        private InventoryItemSetManager m_InventoryItemSetManager;
        //private BridgeEquippableProcessing m_BridgeEquippableProcessing;
        private BridgeDropProcessing m_BridgeDropProcessing;
        private Dictionary<IItemIdentifier, CharacterItem>[] m_ItemToCharacterItemMap;
        private Opsive.UltimateInventorySystem.Core.Item[] m_ActiveItem;
        private RemovingData m_RemovingData;

        protected bool m_Initialized;

        public ItemCollection DefaultItemCollection => m_DefaultItemCollection;
        public ItemCollectionGroup BridgeItemCollections => m_BridgeItemCollections;
        public ItemCollectionGroup LoadoutItemCollections => m_LoadoutItemCollections;
        public InventoryItemSetManager InventoryItemSetManager => m_InventoryItemSetManager;

        public string CharacterItemPrefabsAttributeName => m_CharacterItemPrefabsAttributeName;

        public ItemCategory EquippableCategory => m_EquippableCategory;

        public Inventory Inventory => m_Inventory;
        public Character.UltimateCharacterLocomotion CharacterLocomotion => m_CharacterLocomotion;

        public bool DropUsingCharacterItemWhenPossible { get => m_DropUsingCharacterItemWhenPossible; set => m_DropUsingCharacterItemWhenPossible = value; }
        public Vector3 ItemDropPositionOffset { get => m_ItemDropPositionOffset; set => m_ItemDropPositionOffset = value; }
        public GameObject InventoryItemPickupPrefab { get => m_InventoryItemPickupPrefab; set => m_InventoryItemPickupPrefab = value; }
        public BridgeDropProcessing BridgeDropProcessing { get => m_BridgeDropProcessing; set => m_BridgeDropProcessing = value; }
        
        /// <summary>
        /// Initialize the default values.
        /// </summary>
        protected override void AwakeInternal()
        {
            // The InventoryBridge inherits IItemRestriction, meaning it can be initialized before awake.
            if(m_Initialized){ return; }
            Initialize(
                m_GameObject.GetCachedComponent<Opsive.UltimateInventorySystem.Core.InventoryCollections.Inventory>(),
                false);
        }
        
        /// <summary>
        /// Initialize the item restriction.
        /// </summary>
        /// <param name="inventory">The inventory.</param>
        /// <param name="force">Force initialize?</param>
        public void Initialize(IInventory inventory, bool force) {

            if (m_Initialized && !force) {
                return;
            }
            m_Inventory = inventory as Inventory;
            if (m_Inventory == null) { return; }

            m_Initialized = true;
            
            base.AwakeInternal();
            
            m_Transform = transform;
#if UNITY_EDITOR
            
#if UNITY_2023_1_OR_NEWER
            m_InventorySystemManager = FindFirstObjectByType<InventorySystemManager>();
#else
            m_InventorySystemManager = FindObjectOfType<InventorySystemManager>();
#endif
#endif
            m_CharacterLocomotion = m_GameObject.GetCachedComponent<Character.UltimateCharacterLocomotion>();
            m_InventoryItemSetManager = m_GameObject.GetCachedComponent<InventoryItemSetManager>();

            m_ItemToCharacterItemMap = new Dictionary<IItemIdentifier, CharacterItem>[m_SlotCount];
            for (int i = 0; i < m_SlotCount; ++i) {
                m_ItemToCharacterItemMap[i] = new Dictionary<IItemIdentifier, CharacterItem>();
            }
            m_ActiveItem = new Opsive.UltimateInventorySystem.Core.Item[m_SlotCount];

            if (m_EquippableCategory.HasValue == false) {
                Debug.LogError("Error: An equippable category must be specified.");
            }

            m_DefaultItemCollection = GetItemCollection(m_DefaultItemCollectionName, true);

            m_BridgeItemCollections = new ItemCollectionGroup();
            m_BridgeItemCollections.ItemCollections = new List<ItemCollection>();
            for (int i = 0; i < m_BridgeItemCollectionNames.Length; i++) {
                var equippableItemCollection = GetItemCollection(m_BridgeItemCollectionNames[i], true);
                if(equippableItemCollection == null){continue;}
                m_BridgeItemCollections.AddItemCollection(equippableItemCollection);
            }

            m_LoadoutItemCollections = new ItemCollectionGroup();
            m_LoadoutItemCollections.ItemCollections = new List<ItemCollection>();
            for (int i = 0; i < m_LoadoutItemCollectionNames.Length; i++) {
                var loadoutItemCollection = GetItemCollection(m_LoadoutItemCollectionNames[i], true);
                if(loadoutItemCollection == null){continue;}
                m_LoadoutItemCollections.AddItemCollection(loadoutItemCollection);
            }
            
            m_Inventory.AddRestriction(this);
            
            m_BridgeDropProcessing = new BridgeDropProcessing(this);
            m_RemoveOnDeathOptions.Initialize(this);

            EventHandler.RegisterEvent<ItemInfo, ItemStack>(m_Inventory, EventNames.c_Inventory_OnAdd_ItemInfo_ItemStack, OnAddItemToInventory);
            EventHandler.RegisterEvent<ItemInfo>(m_Inventory, EventNames.c_Inventory_OnRemove_ItemInfo, OnRemoveItemFromInventory);
            
            EventHandler.RegisterEvent<CharacterItem,int>(m_GameObject, "OnInventoryEquipItem", NotifyOnInventoryEquipItem);
            EventHandler.RegisterEvent<CharacterItem,int>(m_GameObject, "OnInventoryUnequipItem", NotifyOnInventoryUnequipItem);
        }

        /// <summary>
        /// Notify the Ultimate Inventory system that an Item was unequipped.
        /// </summary>
        /// <param name="characterItem">The character item that was unequipped.</param>
        /// <param name="slotID">The slot ID where the item was unequipped.</param>
        protected virtual void NotifyOnInventoryUnequipItem(CharacterItem characterItem, int slotID)
        {
            if (m_Inventory != null) {
                m_Inventory.UpdateInventory();
            }
        }

        /// <summary>
        /// Notify the Ultimate Inventory system that an Item was equipped.
        /// </summary>
        /// <param name="characterItem">The character item that was equipped.</param>
        /// <param name="slotID">The slot ID where the item was equipped.</param>
        protected virtual void NotifyOnInventoryEquipItem(CharacterItem characterItem, int slotID)
        {
            if (m_Inventory != null) {
                m_Inventory.UpdateInventory();
            }
        }

        //TODO what about the difference between add and pickup? Use AddingData?
        /// <summary>
        /// Adjusts the amount of the specified ItemIdentifier.
        /// </summary>
        /// <param name="itemIdentifier">The ItemIdentifier to adjust.</param>
        /// <param name="amount">The amount of ItemIdentifier to adjust.</param>
        public override int AddItemIdentifierAmount(IItemIdentifier itemIdentifier, int amount)
        {
            // Intercept the call and instead add the item to the Inventory System Inventory
            // base.AddItemIdentifierAmount will be called in OnAddItemToInventory
            if (itemIdentifier is Item item) {
                var addedItemInfo = m_Inventory.AddItem(item, amount);
                return addedItemInfo.Amount;
            } else {
                Debug.LogWarning("The Inventory Bridge does not allow none Item ItemIdentifiers", gameObject);
            }

            return 0;
        }
        
        /// <summary>
        /// An Item has been added to the Inventory.
        /// </summary>
        /// <param name="itemInfo">The info that describes hte item.</param>
        /// <param name="addedItemStack">The ItemCollection that the item was added to.</param>
        private void OnAddItemToInventory(ItemInfo itemInfo, ItemStack addedItemStack)
        {
            if(addedItemStack == null){ return; }
            
#if DEBUG_BRIDGE
            Debug.Log("Item Added or Moved in Inventory, origin: " + itemInfo + " | added stack: " + addedItemStack ,gameObject);
#endif

            var originItemCollection = itemInfo.ItemCollection;
            var destinationItemCollection = addedItemStack.ItemCollection;

            // If the item is not part of the Character collections ignore it.
            if (!m_BridgeItemCollections.Contains(destinationItemCollection)) { return; }
#if DEBUG_BRIDGE
                    Debug.Log("Adding Inventory Item to Character Inventory " + itemInfo + " " + itemInfo.Item.ItemDefinition + " " +
                              m_GameObject);
#endif
                
            base.AddItemIdentifierAmount(itemInfo.Item, itemInfo.Amount);
        }

        /// <summary>
        /// Remove an item amount from the inventory..
        /// </summary>
        /// <param name="itemIdentifier">The ItemIdentifier to remove.</param>
        /// <param name="slotID">The slot id in which to remove the item from.</param>
        /// <param name="amount">The amount of ItemIdentifier to adjust.</param>
        /// <param name="drop">Should the item be dropped?</param>
        /// <param name="removeCharacterItem">Should the character item be removed?</param>
        /// <param name="destroyCharacterItem">Should the character item be destroyed?</param>
        /// <param name="removeAssociatedItemIdentifiers">(This parameter is forced to false in the bridge)Should the associated item identifiers also be removed.</param>
        /// <returns>Returns a tuple of the actual amount removed and the dropped item instance.</returns>
        protected override (int amountRemoved, GameObject dropInstance) RemoveItemIdentifierAmountInternal(
            IItemIdentifier itemIdentifier, int slotID, int amount, bool drop, bool removeCharacterItem, bool destroyCharacterItem, bool removeAssociatedItemIdentifiers = true)
        {
            // Intercept the call and instead add the item to the Inventory System Inventory
            // base.RemoveItemIdentifierAmountInternal will be called in OnRemoveItemFromInventory

            m_RemovingData = new RemovingData(itemIdentifier, slotID, amount, drop, removeCharacterItem,
                destroyCharacterItem);
            
            if (itemIdentifier is Item item) {
                // The Item might be in one of the CharacterItem Collections (For example throwable items)
                var bridgeItem = m_BridgeItemCollections.GetItemInfo(item);
                ItemInfo removeItemInfo;
                if (bridgeItem.HasValue) {
                    removeItemInfo = m_BridgeItemCollections.RemoveItem(new ItemInfo(bridgeItem.Value, amount));
                } else {
                    removeItemInfo = m_Inventory.RemoveItem(item, amount);
                }

                var dropInstance = m_RemovingData.DropInstance;
                // The removing data is reset once the item is removed.
                m_RemovingData = RemovingData.None;
                return (removeItemInfo.Amount, dropInstance);
            } else {
                Debug.LogWarning("The Inventory Bridge does not allow none Item ItemIdentifiers", gameObject);
            }

            return (0, null);
        }

        /// <summary>
        /// An Item has been removed from the Inventory.
        /// </summary>
        /// <param name="itemInfo">The info that describes the item.</param>
        private void OnRemoveItemFromInventory(ItemInfo itemInfo)
        {
            // If the item is not part of the Character collections ignore it.
            if (!m_BridgeItemCollections.Contains(itemInfo.ItemCollection)) {
                return;
            }
            
#if DEBUG_BRIDGE
            Debug.Log("Item Removed or Moved in Inventory: " + itemInfo,gameObject);
#endif

            // The call was intercepted if the removing data Is initialized.
            if (m_RemovingData.Initialized) {
                var result = base.RemoveItemIdentifierAmountInternal(
                    itemInfo.Item,
                    m_RemovingData.SlotID,
                    itemInfo.Amount,
                    m_RemovingData.Drop,
                    m_RemovingData.RemoveCharacterItem,
                    m_RemovingData.DestroyCharacterItem,
                    false
                );
                // The removing data will reset was the item is fully removed.
                m_RemovingData.DropInstance = result.dropInstance;
            } else {
                base.RemoveItemIdentifierAmountInternal(
                    itemInfo.Item,
                    -1, 
                    itemInfo.Amount,
                    false,
                    m_AutoRemoveCharacterItems,
                    m_AutoSpawnDestroyRuntimeCharacterItems,
                    false);
                // The removing data is reset now since the call was not intercepted.
                m_RemovingData = RemovingData.None;
            }
        }

#region Inventory Base overrides

        /// <summary>
        /// Try to drop an item.
        /// </summary>
        /// <param name="itemIdentifier">The item identifier to drop.</param>
        /// <param name="slotID">The slot id of the item to drop.</param>
        /// <param name="amount">The amount to drop.</param>
        /// <returns>The dropped instance.</returns>
        protected override GameObject TryDropItemInstance(IItemIdentifier itemIdentifier, int slotID, int amount)
        {
            var itemInfo = new ItemInfo(itemIdentifier as Item, amount);
            var inventory = DropItem(itemInfo,false, false);
            return inventory?.gameObject;
        }

        //TODO this function is the same for both Inventories, perhaps use it in the base?
        /// <summary>
        /// Internal method which determines if the character has the specified item.
        /// </summary>
        /// <param name="characterItem">The item to check against.</param>
        /// <returns>True if the character has the item.</returns>
        protected override bool HasCharacterItemInternal(CharacterItem characterItem)
        {
            if (characterItem == null) {
                return false;
            }
            return GetCharacterItem(characterItem.ItemDefinition as ItemType, characterItem.SlotID) != null;
        }

        /// <summary>
        /// Add the content of the default loadout item collection to the main item collection.
        /// </summary>
        public override void LoadDefaultLoadoutInternal()
        {
            if (m_LoadoutItemCollections == null) {
                return;
            }
            
            // The item set manager must be initialized if it isn't already.
            m_InventoryItemSetManager.Initialize(false);
            
            // Make the inventory monitor stop listening while loading the loadout.
            EventHandler.ExecuteEvent(gameObject, EventNames.c_InventoryGameObject_InventoryMonitorListen_Bool, false);

            // Equip the default items.
            var categorySetCount = m_InventoryItemSetManager.ItemSetGroups.Length;
            for (int i = 0; i < categorySetCount; i++) {
                var targetItemSetIndex = m_InventoryItemSetManager.GetTargetItemSetIndex(i, -1);
                if(targetItemSetIndex == -1){continue;}
                Equip(i,targetItemSetIndex, true, true);
            }

            // Add the items from the loadouts
            for (int i = 0; i < m_LoadoutItemCollections.ItemCollections.Count; i++) {
                var loadoutItemCollection = m_LoadoutItemCollections.ItemCollections[i];

                // Use the custom character loadout function to choose whether to equip the items or not.
                if (loadoutItemCollection is CharacterLoadoutItemCollection characterLoadoutItemCollection) {
                    characterLoadoutItemCollection.LoadCharacterLoadout(this);
                    continue;
                }
                
                // Not all items can belong to multiple collections. Create a new item and add that item to the default The loadout should remain the same
                m_DefaultItemCollection.UpdateEventDisabled = true;
                var items = loadoutItemCollection.GetAllItemStacks();
                for (int j = 0; j < items.Count; ++j) {
                    var duplicateItem = InventorySystemManager.CreateItem(items[j].Item);
                    m_DefaultItemCollection.AddItem((ItemInfo)(duplicateItem, items[j].Amount));
                }
                m_DefaultItemCollection.UpdateEventDisabled = false;
                m_DefaultItemCollection.UpdateCollection();
            }

            EventHandler.ExecuteEvent(gameObject, EventNames.c_InventoryGameObject_InventoryMonitorListen_Bool, true);
        }
        
        /// <summary>
        /// Get the Character Item Prefabs for the item identifier.
        /// </summary>
        /// <param name="itemIdentifier">The item identifier from which the character item prefabs should be retrieved.</param>
        /// <returns>A list slice of character item prefabs as GameObjects.</returns>
        public override ListSlice<GameObject> GetItemIdentifierCharacterItemPrefabs(IItemIdentifier itemIdentifier)
        {
            if (itemIdentifier is not Item item) {
                return ListSlice<GameObject>.Empty;
            }

            if (m_EquippableCategory.Value.InherentlyContains(item) == false) {
                return ListSlice<GameObject>.Empty;
            }
            
            // The prefabs can be of type GameObject or GameObject[]
            var itemGameObjectPrefabsAttribute = item.GetAttribute<Attribute<GameObject[]>>(m_CharacterItemPrefabsAttributeName);
            if (itemGameObjectPrefabsAttribute == null) {
                var itemGameObjectPrefabAttribute = item.GetAttribute<Attribute<GameObject>>(m_CharacterItemPrefabsAttributeName);
                if (itemGameObjectPrefabAttribute == null) {
                    Debug.LogError($"Error: The item {item} does not have the \"{m_CharacterItemPrefabsAttributeName}\" attribute of type GameObject or GameObject[].");
                    return ListSlice<GameObject>.Empty;
                }
                
                return itemGameObjectPrefabAttribute.GetValue();
            }

            return itemGameObjectPrefabsAttribute.GetValue();
        }

        /// <summary>
        /// Internal method which returns the active item in the specified slot.
        /// </summary>
        /// <param name="slotID">The ID of the slot which the item belongs to.</param>
        /// <returns>The active item which occupies the specified slot. Can be null.</returns>
        protected override CharacterItem GetActiveCharacterItemInternal(int slotID)
        {
            if (slotID < 0 || slotID >= m_ActiveItem.Length) { return null; }
            
            var item = m_ActiveItem[slotID];
            if (item == null) { return null; }
            
            return GetCharacterItem(item, slotID);
        }
        
        protected override bool TryGetCharacterItemInternal(IItemIdentifier itemIdentifier, int slotID, out CharacterItem characterItem)
        {
            characterItem = null;
            if (itemIdentifier == null || slotID < 0 || slotID >= m_ItemToCharacterItemMap.Length) {
                return false;
            }

            if (m_ItemToCharacterItemMap[slotID].TryGetValue(itemIdentifier, out var item)) {
                characterItem = item;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Internal method which equips the ItemIdentifier in the specified slot.
        /// </summary>
        /// <param name="itemIdentifier">The ItemIdentifier to equip.</param>
        /// <param name="slotID">The ID of the slot.</param>
        /// <returns>The item which corresponds to the ItemIdentifier. Can be null.</returns>
        protected override CharacterItem EquipItemInternal(IItemIdentifier itemIdentifier, int slotID)
        {
            if (itemIdentifier == null || slotID < -1 || slotID >= SlotCount) {
                return null;
            }
            
            var item = itemIdentifier as Opsive.UltimateInventorySystem.Core.Item;
#if DEBUG_BRIDGE
            Debug.Log("Active Equipped for slot "+slotID+" Item:"+item);
#endif
            
            var characterItem = GetCharacterItem(item, slotID);
            if (characterItem == null) {
                return null;
            }

            m_ActiveItem[slotID] = item;
            return characterItem;
        }

        /// <summary>
        /// Internal method which unequips the item in the specified slot.
        /// </summary>
        /// <param name="slotID">The ID of the slot.</param>
        /// <returns>The item that was unequipped.</returns>
        protected override CharacterItem UnequipItemInternal(int slotID)
        {
            if (slotID < -1 || slotID >= m_ActiveItem.Length) {
                return null;
            }
            
            var prevItem = GetActiveCharacterItem(slotID);
#if DEBUG_BRIDGE
            Debug.Log("Active Unequipped for slot "+slotID+" item "+prevItem);
#endif
            m_ActiveItem[slotID] = null;
            return prevItem;
        }
        
        /// <summary>
        /// When a character item is spawned send events to notify objects outside the inventory.
        /// </summary>
        /// <param name="characterItem">The character Item that was added.</param>
        public override void OnCharacterItemSpawned(CharacterItem characterItem)
        {
            m_ItemToCharacterItemMap[characterItem.SlotID][characterItem.ItemIdentifier] = characterItem;
            base.OnCharacterItemSpawned(characterItem);
        }

        /// <summary>
        /// On the Character start Initializing.
        /// </summary>
        /// <param name="characterItem">The character item that is starting to initialize.</param>
        public override void OnCharacterItemStartInitializing(CharacterItem characterItem)
        {
            base.OnCharacterItemStartInitializing(characterItem);
            // Ensure an ItemIdentifier is set.
            var itemObject = characterItem.GetComponent<ItemObject>();
            if (itemObject == null) {
                itemObject = characterItem.gameObject.AddComponent<ItemObject>();
            }

            var item = characterItem.ItemIdentifier as Item;
            itemObject.SetItem(item);
        }

        /// <summary>
        /// A Character item will be destroyed, remove it from the dictionaries and lists.
        /// </summary>
        /// <param name="characterItem">The character item that will be destroyed.</param>
        protected override void OnCharacterItemWillBeDestroyed(CharacterItem characterItem)
        {
            base.OnCharacterItemWillBeDestroyed(characterItem);
            m_ItemToCharacterItemMap[characterItem.SlotID].Remove(characterItem.ItemIdentifier);
        }

        /// <summary>
        /// Internal method which returns the amount of the specified ItemIdentifier.
        /// </summary>
        /// <param name="itemIdentifier">The ItemIdentifier to get the amount of.</param>
        /// <param name="includeExternalItems"></param>
        /// <returns>The amount of the specified ItemIdentifier.</returns>
        protected override int GetItemIdentifierAmountInternal(IItemIdentifier itemIdentifier, bool includeExternalItems)
        {
            if (itemIdentifier is not Item inventoryItem) {
                Debug.LogWarning($"The ItemIdentifier '{itemIdentifier}' is an ItemType. The identifier must be created by the Ultimate Inventory System.");

                var itemTypeItemDefinition = itemIdentifier.GetItemDefinition();
                if (itemTypeItemDefinition == null) {
                    return 0;
                }

                var itemItemDefinition = InventorySystemManager.GetItemDefinition(itemTypeItemDefinition.name);
                if (itemItemDefinition == null) {
                    return 0;
                }
                
                // Found a match, continue with that item.
                inventoryItem = InventorySystemManager.CreateItem(itemItemDefinition);
            }

#if UNITY_EDITOR
            if (!m_InventorySystemManager.Database.Contains(inventoryItem)) {
                Debug.LogError(
                    $"Error: The Item ({itemIdentifier}) does not exist within the active database ({m_InventorySystemManager.Database.name}).");
                return 0;
            }
#endif

            var amount = BridgeItemCollections.GetItemAmount(inventoryItem);
            if (includeExternalItems) {
                amount = Inventory.GetItemAmount(inventoryItem);
            }

            return amount;
        }

        /// <summary>
        /// Add an item identifier amount.
        /// </summary>
        /// <param name="itemIdentifier">The item identifier to add.</param>
        /// <param name="amount">The amount to add.</param>
        protected override void AddItemIdentifierAmountInternal(IItemIdentifier itemIdentifier, int amount)
        {
            // DO nothing. Items are always added to the Inventory system Inventory first.
        }

        /// <summary>
        /// Remove the amount of the specified ItemIdentifier.
        /// </summary>
        /// <param name="itemIdentifier">The ItemIdentifier to remove.</param>
        /// <param name="amount">The amount of ItemIdentifier to remove.</param>
        protected override void RemoveItemIdentifierAmountInternal(IItemIdentifier itemIdentifier, int amount)
        {
            // DO nothing. Items are always removed to the Inventory system Inventory first.
        }

        /// <summary>
        /// Can the item be removed?
        /// </summary>
        /// <param name="itemIdentifier">The item to remove.</param>
        /// <returns>True if it can be removed.</returns>
        protected override bool CanRemoveItemIdentifier(IItemIdentifier itemIdentifier)
        {
            // The Item Restriction will be used in addition to this.
            if (m_RemoveExceptions.InherentlyContains(itemIdentifier, false)) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// The character has died.
        /// </summary>
        /// <param name="position">The position of the force.</param>
        /// <param name="force">The amount of force which killed the character.</param>
        /// <param name="attacker">The GameObject that killed the character.</param>
        protected override void OnDeath(Vector3 position, Vector3 force, GameObject attacker)
        {
            base.OnDeath(position, force, attacker);
            // The item's drop method will call RemoveItem within the inventory.
            m_RemoveOnDeathOptions.OnDeath();
        }
        
    #endregion
    
        
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
        /// Is the Item Active.
        /// </summary>
        /// <param name="item">The item to check if active.</param>
        /// <returns>True if active</returns>
        public bool IsItemActive(Item item)
        {
            for (int i = 0; i < m_SlotCount; i++) {
                if (TryGetCharacterItem(item, i, out var characterItem)) {
                    if (IsItemActive(characterItem)) {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Is the Item Active.
        /// </summary>
        /// <param name="item">The item to check if active.</param>
        /// <returns>True if active</returns>
        public virtual bool IsItemActive(CharacterItem characterItem)
        {
            if (characterItem == null) { return false; }

            var slotID = characterItem.SlotID;

            var active = characterItem.IsActive();
            var activeCharacterItem = GetActiveCharacterItem(slotID);
            return active && activeCharacterItem == characterItem;
        }

        /// <summary>
        /// The Drop ItemAction has been triggered.
        /// </summary>
        /// <param name="item">The Item that should be dropped.</param>
        /// <param name="itemInfo"></param>
        public virtual Inventory DropItem(ItemInfo itemInfo, bool forceDrop, bool removeItemOnDrop)
        {
            return m_BridgeDropProcessing.DropInventoryPickup(itemInfo, forceDrop, removeItemOnDrop);
        }

        /// <summary>
        /// Returns the Inventory with the specified Slot ID.
        /// </summary>
        /// <param name="slotID">The ID of the slot for the ItemObject that should be retrieved.</param>
        /// <returns></returns>
        public Item GetActiveInventoryItem(int slotID)
        {
            if (slotID < 0 || slotID >= m_ActiveItem.Length) {
                return null;
            }
            return m_ActiveItem[slotID];
        }

        /// <summary>
        /// Returns the ItemObject with the specified Item and Slot ID.
        /// </summary>
        /// <param name="item">The Item for the ItemObject that should be retrieved.</param>
        /// <param name="slotID">The ID of the slot for the ItemObject that should be retrieved.</param>
        /// <returns></returns>
        public ItemObject GetItemObjectAtSlot(Item item, int slotID)
        {

            var characterItem = GetCharacterItem(item, slotID);
            if (characterItem == null) {
                return null;
            }

            return characterItem.gameObject.GetCachedComponent<ItemObject>();
        }

        /// <summary>
        /// Get the inventory item from the character item.
        /// </summary>
        /// <param name="characterItem">The character Item.</param>
        /// <returns>The inventory Item.</returns>
        public Item GetInventoryItem(CharacterItem characterItem)
        {
            if (characterItem == null) {
                return null;
            }

            return characterItem.ItemIdentifier as Item;
        }
        
        /// <summary>
        /// Move the Item from the Default Item Collection to one of the equippable item collections.
        /// </summary>
        /// <param name="itemInfo">The item info to move.</param>
        /// <param name="equippableCollectionIndex">The index of the equippable item collection.</param>
        /// <param name="slotIndex">The slot Index if the equippable is an item slot collection.</param>
        /// <returns>The item info moved.</returns>
        public virtual ItemInfo MoveItemToEquippable(ItemInfo itemInfo, int equippableCollectionIndex = 0, int slotIndex = -1)
        {
            var equippableCollection = m_BridgeItemCollections.GetItemCollection(equippableCollectionIndex);

            if (equippableCollection == null) {
                Debug.LogWarning($"The equippable item collection at index {equippableCollectionIndex} could not be found",gameObject);
                return ItemInfo.None;
            }

            var sourceItemCollection = itemInfo.ItemCollection;
            if (sourceItemCollection == null) {
                sourceItemCollection = DefaultItemCollection;
            }

            if(sourceItemCollection.HasItem(itemInfo) == false) {
                Debug.LogWarning($"{itemInfo} cannot be moved to equippable because it was not in source collection '{sourceItemCollection.Name}' first.",gameObject);
                return ItemInfo.None;
            }
            
            //Remove the item from Default collection.
            itemInfo = sourceItemCollection.RemoveItem(itemInfo);

            ItemInfo movedItemInfo;
            
            if (equippableCollection is ItemSlotCollection equippableSlotCollection) {

                if (slotIndex == -1) {
                    slotIndex = equippableSlotCollection.GetTargetSlotIndex(itemInfo.Item);
                }
                
                var previousItemInSlot = equippableSlotCollection.GetItemInfoAtSlot(slotIndex);

                if (previousItemInSlot.Item != null) {
                    //If the previous item is stackable don't remove it.
                    if (previousItemInSlot.Item.StackableEquivalentTo(itemInfo.Item)) {
                        previousItemInSlot = ItemInfo.None;
                    } else {
                        previousItemInSlot = equippableSlotCollection.RemoveItem(slotIndex); 
                    }
                }
            
                movedItemInfo = equippableSlotCollection.AddItem(itemInfo, slotIndex);

                if (previousItemInSlot.Item != null) {
                    sourceItemCollection.AddItem(previousItemInSlot);
                }
                
            } else {
                movedItemInfo = equippableCollection.AddItem(itemInfo);
            }
            
            //Not all the item was added, return the items to the default collection.
            if (movedItemInfo.Amount != itemInfo.Amount) {
                var amountToReturn = itemInfo.Amount - movedItemInfo.Amount;
                sourceItemCollection.AddItem((ItemInfo) (amountToReturn, itemInfo));
            }

            return movedItemInfo;

        }

        /// <summary>
        /// Move the item from one of th equippable item collection to the default item collection.
        /// </summary>
        /// <param name="itemInfo">The item info.</param>
        /// <param name="equippableCollectionIndex">The index of the equippable item collection.</param>
        /// <param name="slotIndex">The slot Index if the equippable is an item slot collection.</param>
        /// <returns>The item info moved.</returns>
        public ItemInfo MoveItemToDefault(ItemInfo itemInfo, int equippableCollectionIndex = 0, int slotIndex = -1)
        {
            var equippableCollection = m_BridgeItemCollections.GetItemCollection(equippableCollectionIndex);

            if (equippableCollection == null) {
                Debug.LogWarning($"The equippable item collection at index {equippableCollectionIndex} could not be found",gameObject);
                return ItemInfo.None;
            }
            
            ItemInfo movedItemInfo;
            
            if (equippableCollection.HasItem(itemInfo) == false) {
                Debug.LogWarning($"{itemInfo} cannot be moved to default because it was not found in the equippable collection '{equippableCollection.Name}'.",gameObject);
                return ItemInfo.None;
            }

            if (equippableCollection is ItemSlotCollection equippableSlotCollection) {

                if (slotIndex == -1) {
                    slotIndex = equippableSlotCollection.GetItemSlotIndex(itemInfo.Item);
                }
                
                movedItemInfo = equippableSlotCollection.RemoveItem(slotIndex, itemInfo.Amount);
                
            } else {
                movedItemInfo = equippableCollection.RemoveItem(itemInfo);
            }
            
            m_DefaultItemCollection.AddItem(movedItemInfo);

            return movedItemInfo;
        }

        /// <summary>
        /// Move the item from collection and equip or unequip the item.
        /// </summary>
        /// <param name="itemInfo">The item info to equip/unequip.</param>
        /// <param name="equip">Equip or Unequip?</param>
        public void MoveEquip(ItemInfo itemInfo, bool equip)
        {
            MoveEquip(itemInfo, 0, -1, equip);
        }
        
        /// <summary>
        /// Move the item from collection and equip or unequip the item.
        /// </summary>
        /// <param name="itemInfo">The item info to equip/unequip.</param>
        /// <param name="equippableCollectionIndex">The index of the equippable item collection.</param>
        /// <param name="slotIndex">The slot Index if the equippable is an item slot collection.</param>
        /// <param name="equip">Equip or Unequip?</param>
        public void MoveEquip(ItemInfo itemInfo, int equippableItemCollectionSet, int slotID, bool equip)
        {
            if (equip) {
                MoveItemToEquippable(itemInfo, equippableItemCollectionSet, slotID);
                Equip(itemInfo,true);
            } else {
                MoveItemToDefault(itemInfo, equippableItemCollectionSet, slotID);
            }
        }

        /// <summary>
        /// Equip or Unequip an item, the item must be equippable.
        /// </summary>
        /// <param name="itemInfo">The item info to equip/Unequip.</param>
        /// <param name="equip">Equip or Unequip?</param>
        public void Equip(ItemInfo itemInfo, bool equip, bool forceEquipUnequip = false, bool immediateEquipUnequip = false)
        {
            InventoryItemSetManager.EquipUnequipItem(itemInfo.Item, equip,-1, forceEquipUnequip, immediateEquipUnequip);
        }

        /// <summary>
        /// Equip a specific Item Set.
        /// </summary>
        /// <param name="groupIndex">The group index.</param>
        /// <param name="itemSetIndex">The item set index.</param>
        /// <param name="forceEquipUnequip">Force equip/unequip?</param>
        /// <param name="immediateEquipUnequip">immediate equip/unequip?</param>
        public void Equip(int groupIndex, int itemSetIndex, bool forceEquipUnequip = false, bool immediateEquipUnequip = false)
        {
            m_InventoryItemSetManager.TryEquipItemSet(itemSetIndex, groupIndex, forceEquipUnequip, immediateEquipUnequip);
        }
        
        /// <summary>
        /// Called when the bridge will be loaded, disable equipping or state changes.
        /// </summary>
        public virtual void OnWillLoadSaveData()
        {
            //TODO what do we do here?
            //BridgeEquippableProcessing.EquipOnItemSetUpdate = false;
            //BridgeEquippableProcessing.StateChangeOnItemSetUpdate = false;
        }

        /// <summary>
        /// Called when the bridge finished loading the save data, re-enable the equipping and state changes.
        /// </summary>
        public virtual void OnSaveDataLoaded()
        {
            //TODO what do we do here?
            //BridgeEquippableProcessing.EquipOnItemSetUpdate = true;
            //BridgeEquippableProcessing.StateChangeOnItemSetUpdate = true;
        }

        /// <summary>
        /// Condition when adding an item?
        /// </summary>
        /// <param name="itemInfo">The item info.</param>
        /// <param name="receivingCollection">The receiving collection.</param>
        /// <returns>The item info to add.</returns>
        public virtual ItemInfo? CanAddItem(ItemInfo itemInfo, ItemCollection receivingCollection)
        {
            if (!BridgeItemCollections.Contains(receivingCollection)) {
                return itemInfo;
            }

            return itemInfo;
        }

        /// <summary>
        /// Remove condition.
        /// </summary>
        /// <param name="itemInfo">The item info.</param>
        /// <returns>The item to remove.</returns>
        public virtual ItemInfo? CanRemoveItem(ItemInfo itemInfo)
        {
            return itemInfo;
        }

        /// <summary>
        /// Check if the object contained by this component are part of the database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <returns>True if all the objects in the component are part of that database.</returns>
        bool IDatabaseSwitcher.IsComponentValidForDatabase(InventorySystemDatabase database)
        {
            if (database == null) { return false; }

            database.Initialize(false);

            return database.Contains(m_EquippableCategory);
        }

        /// <summary>
        /// Replace any object that is not in the database by an equivalent object in the specified database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <returns>The objects that have been changed.</returns>
        ModifiedObjectWithDatabaseObjects IDatabaseSwitcher.ReplaceInventoryObjectsBySelectedDatabaseEquivalents(InventorySystemDatabase database)
        {
            if (database == null) { return null; }

            database.Initialize(false);

            m_EquippableCategory = database.FindSimilar(m_EquippableCategory);
            return null;
        }

        
        /// <summary>
        /// The object has been destroyed.
        /// </summary>
        protected override void OnDestroy()
        {
            if(m_Initialized && m_Inventory != null) {
                EventHandler.UnregisterEvent<ItemInfo, ItemStack>(m_Inventory, EventNames.c_Inventory_OnAdd_ItemInfo_ItemStack, OnAddItemToInventory);
                EventHandler.UnregisterEvent<ItemInfo>(m_Inventory, EventNames.c_Inventory_OnRemove_ItemInfo, OnRemoveItemFromInventory);
                
                EventHandler.UnregisterEvent<CharacterItem,int>(m_GameObject, "OnInventoryEquipItem", NotifyOnInventoryEquipItem);
                EventHandler.UnregisterEvent<CharacterItem,int>(m_GameObject, "OnInventoryUnequipItem", NotifyOnInventoryUnequipItem);
            }
            
            base.OnDestroy();
        }
    }
}