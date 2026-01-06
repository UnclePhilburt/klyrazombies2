/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem.Editor
{
    using Opsive.Shared.Editor.Managers;
    using Opsive.UltimateCharacterController.Character;
    using Opsive.UltimateCharacterController.Character.Abilities.Items;
    using Opsive.UltimateCharacterController.Editor.Inspectors.Utility;
    using Opsive.UltimateCharacterController.Game;
    using Opsive.UltimateCharacterController.Objects;
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using Opsive.UltimateInventorySystem.Editor.Managers;
    using Opsive.UltimateInventorySystem.Editor.Styles;
    using Opsive.UltimateInventorySystem.Editor.VisualElements;
    using Opsive.UltimateInventorySystem.Exchange;
    using Opsive.UltimateInventorySystem.Interactions;
    using Opsive.UltimateInventorySystem.ItemActions;
    using Opsive.UltimateInventorySystem.Storage;
    using System;
    using System.Collections.Generic;
    using Opsive.UltimateCharacterController.Inventory;
    using Opsive.UltimateCharacterController.Items;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Inventory = Opsive.UltimateInventorySystem.Core.InventoryCollections.Inventory;
    using ItemCollection = Opsive.UltimateInventorySystem.Core.InventoryCollections.ItemCollection;
    using ManagerUtility = Opsive.UltimateCharacterController.Editor.Managers.ManagerUtility;
    using Object = UnityEngine.Object;
    using Opsive.Shared.Utility;

    /// <summary>
    /// Draws an inspector that can be used within the Inspector Manager.
    /// </summary>
    [OrderedEditorItem("Ultimate Character Controller", 1)]
    public class UltimateCharacterControllerIntegration : Opsive.UltimateInventorySystem.Editor.Managers.IntegrationInspector
    {
        public const string c_AmmoItemCategoryName = "Ammo";
        public const string c_EquippableItemCategoryName = "Equippable";
        
        private CharacterSetup m_CharacterSetup;
        protected ItemPickupSetup m_ItemPickupSetup;
        
        protected ObjectField m_BridgeComponentField;
        
        /// <summary>
        /// Draws the integration inspector.
        /// </summary>
        /// <param name="parent">The parent VisualElement.</param>
        public override void ShowInspector(VisualElement parent)
        {
            //parent.Add(new Label("The integration is located within the Ultimate Character Controller Manager."));
            var label = new Label("Integration Version 3.2.4\n" +
                                  "Requires:\n" +
                                  "Ultimate Character Controller v3.2.4+\n" +
                                  "Ultimate Inventory System v1.3.2+\n");
            label.AddToClassList(InventoryManagerStyles.SubMenuIconDescription);
            parent.Add(label);
            
            m_BridgeComponentField = new ObjectField("Character Inventory Bridge");
            m_BridgeComponentField.objectType = typeof(CharacterInventoryBridge);
            
#if UNITY_2023_1_OR_NEWER
            m_BridgeComponentField.value = Object.FindFirstObjectByType<CharacterInventoryBridge>();
#else
            m_BridgeComponentField.value = Object.FindObjectOfType<CharacterInventoryBridge>();
#endif
            m_BridgeComponentField.RegisterValueChangedCallback(evt =>
            {
                Refresh();
            });
            parent.Add(m_BridgeComponentField);
            
            var label2 = new Label("The Character and Character Items must be created using the Character Manager. " +
                                   "Once create they can be converted to work with the Inventory System using the fields below.");
            label2.AddToClassList(InventoryManagerStyles.SubMenuIconDescription);
            parent.Add(label2);
            
            var button = IntegrationControlBox.CreateButton("Open the Character Item Manager", 
                Opsive.UltimateCharacterController.Editor.Managers.CharacterMainWindow.ShowItemManagerWindow);
            parent.Add(button);
            
            m_CharacterSetup = new CharacterSetup();
            m_CharacterSetup.OnSetupCharacter += (character) =>
            {
                m_BridgeComponentField.value = character.GetComponent<CharacterInventoryBridge>();
            };
            parent.Add(m_CharacterSetup);
            
            m_ItemPickupSetup = new ItemPickupSetup();
            parent.Add(m_ItemPickupSetup);
            

            Refresh();
        }

        private void Refresh()
        {
            var inventoryBridge = m_BridgeComponentField.value as CharacterInventoryBridge;
            if (inventoryBridge == null) {
                
#if UNITY_2023_1_OR_NEWER
                inventoryBridge = Object.FindFirstObjectByType<CharacterInventoryBridge>();
#else
                inventoryBridge = Object.FindObjectOfType<CharacterInventoryBridge>();
#endif
                m_BridgeComponentField.SetValueWithoutNotify(inventoryBridge);
            }

            m_CharacterSetup.Refresh();
            m_ItemPickupSetup.Refresh();
        }
    }
    
    public class CharacterSetup : IntegrationControlBox
    {
        public event Action<GameObject> OnSetupCharacter;
        
        public override string Title => "Character Setup";
        public override string Description => "Sets up the character to work with the Ultimate Inventory System by replacing and adding required components.\n" +
                                              "The character must be first created using the Character Controller Manager and then converted here.";

        protected ObjectField m_CharacterField;
        protected Button m_SetupButton;
        
        public CharacterSetup()
        {
            m_CharacterField = new ObjectField("Character");
            m_CharacterField.objectType = typeof(GameObject);
            m_CharacterField.RegisterValueChangedCallback(evt=>
            {
                Refresh();
            });
            Add(m_CharacterField);
            
            m_SetupButton = CreateButton("Setup Character",SetupCharacter);
            Add(m_SetupButton);

            Refresh();
        }

        public void Refresh()
        {
            var canSetup = false;
            if (m_CharacterField.value is GameObject gameObject) {
                if (gameObject.GetComponent<UltimateCharacterLocomotion>() != null) {
                    canSetup = true;
                }
            }
            m_SetupButton.SetEnabled(canSetup);
        }

        private void SetupCharacter()
        {
            var character = m_CharacterField.value as GameObject;
            SetupCharacter(character);
            
            Debug.Log("Character was setup successfully.");
            m_CharacterField.value = null;
            
            OnSetupCharacter?.Invoke(character);
        }
        
        private void SetupCharacter(GameObject characterObject)
        {
            var locomotion = characterObject.GetComponent<UltimateCharacterLocomotion>();
            if (locomotion == null) {
                Debug.LogWarning("object specified is not a character, please assign a character object");
                return;
            }

            // Ensure the database is valid.
#if UNITY_2023_1_OR_NEWER
            var inventorySystemManager = Object.FindFirstObjectByType<Opsive.UltimateInventorySystem.Core.InventorySystemManager>();
#else
            var inventorySystemManager = Object.FindObjectOfType<Opsive.UltimateInventorySystem.Core.InventorySystemManager>();
#endif
            var database = inventorySystemManager != null ? inventorySystemManager.Database :
                    Opsive.UltimateInventorySystem.Editor.Managers.InventoryMainWindow.InventorySystemDatabase;
            if (database == null) {
                Debug.LogError("Error: Unable to find the database. Ensure the database has been created and is assigned to the Inventory System Manager or " +
                               "database field of the Inventory Manager.");
                return;
            }

            // The database needs to be in the correct format.
            ItemCategory ammoItemCategory = null, equippableItemCategory = null;
            database.Initialize(false);
            if (database.ItemCategories != null) {
                for (int i = 0; i < database.ItemCategories.Length; ++i) {
                    if (database.ItemCategories[i].name == UltimateCharacterControllerIntegration.c_AmmoItemCategoryName) {
                        ammoItemCategory = database.ItemCategories[i];
                    } else if (database.ItemCategories[i].name == UltimateCharacterControllerIntegration.c_EquippableItemCategoryName) {
                        equippableItemCategory = database.ItemCategories[i];
                    }
                }
            }
            if (ammoItemCategory == null || equippableItemCategory == null) {
                Debug.LogWarning("Warning: Unable to find the Ammo Item Category or the Equippable Item Category. " +
                                 "You will be required to add those manually in the Ultimate Inventory System Bridge component.");
            }

            var characterInventory = characterObject.GetComponent<UltimateCharacterController.Inventory.Inventory>();
            if (characterInventory != null) {
                Object.DestroyImmediate(characterInventory, true);
            }

            //Ensure at least one ItemSetGroup exists.
            var someItemSetCategoriesNotFound = false;
            var itemSetGroup = new ItemSetGroup(equippableItemCategory,
                new ItemSetRuleBase[1] { ManagerUtility.FindItemSetRule(null) });
            ItemSetGroup[] itemSetManagerItemSetRuleGroups = new ItemSetGroup[1] { itemSetGroup };
            var itemSetManager = characterObject.GetComponent<UltimateCharacterController.Inventory.ItemSetManager>();
            if (itemSetManager != null) {
                TypeUtility.ResizeIfNecessary(ref itemSetManagerItemSetRuleGroups, itemSetManager.ItemSetGroups.Length+1);
                for (int i = 0; i < itemSetManager.ItemSetGroups.Length; i++) {
                    var itemSetRuleGroup = itemSetManager.ItemSetGroups[i];
                    if (itemSetRuleGroup.ItemCategory == null) {
                        continue;
                    }
                    var foundMatch = false;
                    if (database.TryGet(itemSetRuleGroup.ItemCategory.ID, out ItemCategory newCategory)) {
                        itemSetRuleGroup.ItemCategory = newCategory;
                        foundMatch = true;
                    } else {
                        if (database.TryGet(itemSetRuleGroup.ItemCategory.name, out newCategory)) {
                            itemSetRuleGroup.ItemCategory = newCategory;
                            foundMatch = true;
                        }
                    }

                    if (foundMatch == false) {
                        someItemSetCategoriesNotFound = true;
                        Debug.LogWarning($"The ItemSetGroup Could not find a Category Matching the '{itemSetRuleGroup.ItemCategory.name}' Category within the Inventory Database. The Equippable Category will be used.");
                        itemSetRuleGroup.ItemCategory = equippableItemCategory;
                    }
                    
                    itemSetManagerItemSetRuleGroups[i + 1] = itemSetRuleGroup;
                }
                itemSetManagerItemSetRuleGroups = itemSetManager.ItemSetGroups;
                Object.DestroyImmediate(itemSetManager, true);
            }
            if (someItemSetCategoriesNotFound) {
                Debug.LogWarning("Action Required: Please review the ItemSetGroups ItemCategories within the Inventory Item Set Manager component, as some may be pointing to the wrong category.");
            }

            // Add the default ItemCollections to the character.
            var itemCollections = new List<ItemCollection>();
            var itemCollection = new ItemCollection();
            itemCollection.SetName("Default");
            InspectorUtility.SetFieldValue(itemCollection, "m_Purpose", ItemCollectionPurpose.Main);
            itemCollections.Add(itemCollection);
            var itemSlotCollection = new ItemSlotCollection(CreateSlotSet(database, equippableItemCategory));
            itemSlotCollection.SetName("Equippable Slots");
            InspectorUtility.SetFieldValue(itemSlotCollection, "m_Purpose", ItemCollectionPurpose.Equipped);
            itemCollections.Add(itemSlotCollection);
            itemCollection = new ItemCollection();
            itemCollection.SetName("Equippable");
            InspectorUtility.SetFieldValue(itemCollection, "m_Purpose", ItemCollectionPurpose.None);
            itemCollections.Add(itemCollection);
            itemCollection = new CharacterLoadoutItemCollection();
            itemCollection.SetName("Loadout");
            InspectorUtility.SetFieldValue(itemCollection, "m_Purpose", ItemCollectionPurpose.Loadout);
            itemCollections.Add(itemCollection);
            var inventory = Shared.Editor.Inspectors.Utility.InspectorUtility.AddComponent<Inventory>(characterObject);
            InspectorUtility.SetFieldValue(inventory, "m_ItemCollections", itemCollections);
            inventory.Serialize();

            // Add the Ultimate Inventory System integration components.
            var bridge = Shared.Editor.Inspectors.Utility.InspectorUtility.AddComponent<CharacterInventoryBridge>(characterObject);
            InspectorUtility.SetFieldValue(bridge, "m_EquippableCategory", new DynamicItemCategory(equippableItemCategory));
            EditorUtility.SetDirty(bridge);

            //Add the item set rules for the Inventory Item Set Manager
            var InventoryItemSetManager = Shared.Editor.Inspectors.Utility.InspectorUtility.AddComponent<InventoryItemSetManager>(characterObject);
            InventoryItemSetManager.ItemSetGroups = itemSetManagerItemSetRuleGroups;
            EditorUtility.SetDirty(InventoryItemSetManager);
            
            // Fix the categories on the item set abilities
            // Replace the ItemSetAbility categories.
            var abilities = locomotion.GetAbilities<ItemSetAbilityBase>();
            var someAbilityCategoriesNotFound = false;
            if (abilities != null) {
                for (int i = 0; i < abilities.Length; i++) {
                    var originalAbilityCategory = abilities[i].ItemCategory;
                    if (originalAbilityCategory == null) {
                        continue;
                    }

                    var foundMatch = false;
                    if (database.TryGet(abilities[i].ItemSetCategoryID, out ItemCategory newCategory)) {
                        abilities[i].ItemCategory = newCategory;
                        foundMatch = true;
                    } else {
                        if (database.TryGet(originalAbilityCategory.name, out newCategory)) {
                            abilities[i].ItemCategory = newCategory;
                            foundMatch = true;
                        }
                    }

                    if (foundMatch == false) {
                        someAbilityCategoriesNotFound = true;
                        Debug.LogWarning($"'{abilities[i]}' Could not find a Category Matching the '{originalAbilityCategory.name}' Category within the Inventory Database. The Equippable Category will be used.");
                        abilities[i].ItemCategory = equippableItemCategory;
                    }
                }
            }

            if (someAbilityCategoriesNotFound) {
                Debug.LogWarning("Action Required: Please review the Item Abilities ItemCategories on the Locomotion component, as some may be pointing to the wrong category.");
            }
            
            // Add the additional Inventory System components for characters
            Shared.Editor.Inspectors.Utility.InspectorUtility.AddComponent<InventoryIdentifier>(characterObject);
            Shared.Editor.Inspectors.Utility.InspectorUtility.AddComponent<ItemUser>(characterObject);
            var input = characterObject.GetComponentInChildren<Shared.Input.PlayerInput>();
            if (input == null) {

#if ENABLE_INPUT_SYSTEM
                input = Shared.Editor.Inspectors.Utility.InspectorUtility.AddComponent<Shared.Input.InputSystem.UnityInputSystem>(characterObject);
#else
                input = Shared.Editor.Inspectors.Utility.InspectorUtility.AddComponent<Shared.Input.InputManager.UnityInput>(characterObject);
#endif
            }

#if ENABLE_INPUT_SYSTEM
            if (input is Shared.Input.InputSystem.UnityInputSystem unityInputSystem) {
                unityInputSystem.DisableCursor = false;
                unityInputSystem.EnableCursorWithEscape = false;
            }
#endif
            if (input is Shared.Input.InputManager.UnityInput unityInput) {
                unityInput.DisableCursor = false;
                unityInput.EnableCursorWithEscape = false;
            }
            
            characterObject.GetComponent<ItemUser>().InventoryInput = input;
            Shared.Editor.Inspectors.Utility.InspectorUtility.AddComponent<CurrencyOwner>(characterObject);
            Shared.Editor.Inspectors.Utility.InspectorUtility.AddComponent<InventoryInteractor>(characterObject);
            
            // And finally add the saver
            Shared.Editor.Inspectors.Utility.InspectorUtility.AddComponent<InventoryBridgeSaver>(characterObject);
        }
        
        /// <summary>
        /// Creates the ItemSlotSet Scriptable Object at the same path as the database.
        /// </summary>
        /// <param name="database">The database that is being used.</param>
        /// <param name="equippableCategory">The equippable category.</param>
        /// <returns></returns>
        private static ItemSlotSet CreateSlotSet(InventorySystemDatabase database, Opsive.UltimateInventorySystem.Core.ItemCategory equippableCategory)
        {
            var directory = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(database));
            var name = database.name + "EquippedSlots";
            var index = 1;
            // Ensure the ItemSlotSet is unique.
            while (AssetDatabase.LoadAssetAtPath(directory + "/" + name + ".asset", typeof(ItemSlotSet)) != null) {
                name = database.name + "EquippedSlots" + index;
                index++;
            }

            var itemSlotSet = ScriptableObject.CreateInstance<ItemSlotSet>();
            var itemSlots = new ItemSlot[3];
            itemSlots[0] = new ItemSlot("Primary", equippableCategory);
            itemSlots[1] = new ItemSlot("Secondary", equippableCategory);
            itemSlots[2] = new ItemSlot("Tactical", equippableCategory);
            InspectorUtility.SetFieldValue(itemSlotSet, "m_ItemSlots", itemSlots);
            itemSlotSet.name = name;
            EditorUtility.SetDirty(itemSlotSet);

            Opsive.UltimateInventorySystem.Editor.Utility.AssetDatabaseUtility.CreateAsset(itemSlotSet, directory + "/" + itemSlotSet.name, null);

            return itemSlotSet;
        }
    }

    public class ItemPickupSetup : IntegrationControlBox
    {
        public override string Title => "Item Pickup Setup";
        public override string Description => "Set up an Inventory Item Pickup that was made to work with the Character Inventory Bridge.\n" +
                                              "Note that there are many different ways to setup and use item Pickups, refer to the documentation to learn more.";

        protected ObjectField m_ItemObjectField;
        protected ObjectField m_ModelObjectField;
        protected Button m_SetupButton;
        
        public ItemPickupSetup()
        {
            m_ItemObjectField = new ObjectField("Character Item");
            m_ItemObjectField.objectType = typeof(GameObject);
            m_ItemObjectField.RegisterValueChangedCallback(evt=>
            {
                Refresh();
            });
            Add(m_ItemObjectField);
            
            m_ModelObjectField = new ObjectField("Pickup Model Object");
            m_ModelObjectField.objectType = typeof(GameObject);
            m_ModelObjectField.RegisterValueChangedCallback(evt=>
            {
                Refresh();
            });
            Add(m_ModelObjectField);
            
            m_SetupButton = CreateButton("Setup Item Pickup",SetupItemPickup);
            Add(m_SetupButton);

            Refresh();
        }

        public void Refresh()
        { }

        private void SetupItemPickup()
        {
            var modelObject = m_ModelObjectField.value as GameObject;
            var characterItem = (m_ItemObjectField?.value as GameObject)?.GetComponent<CharacterItem>();
            SetupItemPickup(characterItem, modelObject);
            m_ModelObjectField.value = null;
            m_ItemObjectField.value = null;
            
            Debug.Log($"Success: the item pickup was setup correctly!");
        }
        
        public static GameObject SetupItemPickup(CharacterItem characterItem, GameObject ModelObject)
        {
            
            var pickupName = characterItem?.name ?? ModelObject?.name ?? "InventoryItem";
            pickupName += "Pickup";
            
            var path = EditorUtility.SaveFilePanel("Save Object", "Assets", pickupName + ".prefab", "prefab");
            if (path.Length == 0 || Application.dataPath.Length > path.Length) {
                return null;
            }

            var createdObject = ModelObject;
            if (createdObject == null) {
                createdObject = new GameObject();
            } else if (EditorUtility.IsPersistent(createdObject)) {
                createdObject = GameObject.Instantiate(createdObject) as GameObject;
            }
            createdObject.name = pickupName;
            
            createdObject.layer = LayerManager.VisualEffect;
            Shared.Editor.Inspectors.Utility.InspectorUtility.AddComponent<BoxCollider>(createdObject);
            var sphereCollider = Shared.Editor.Inspectors.Utility.InspectorUtility.AddComponent<SphereCollider>(createdObject);
            sphereCollider.isTrigger = true;
            Shared.Editor.Inspectors.Utility.InspectorUtility.AddComponent<Inventory>(createdObject);
            Shared.Editor.Inspectors.Utility.InspectorUtility.AddComponent<InventoryItemPickup>(createdObject);
            var trajectoryObject = Shared.Editor.Inspectors.Utility.InspectorUtility.AddComponent<TrajectoryObject>(createdObject);
            trajectoryObject.ImpactLayers =
                ~(1 << LayerManager.IgnoreRaycast | 1 << LayerManager.Water | 1 << LayerManager.SubCharacter |
                  1 << LayerManager.Overlay |
                  1 << LayerManager.VisualEffect | 1 << LayerManager.SubCharacter |
                  1 << LayerManager.Character);

            //Replace/Create prefab
            var relativePath = path.Replace(Application.dataPath, "");
            PrefabUtility.SaveAsPrefabAsset(createdObject, "Assets" + relativePath);
            var newPickup = AssetDatabase.LoadAssetAtPath("Assets" + relativePath, typeof(GameObject)) as GameObject;
            Selection.activeGameObject = newPickup;
            Object.DestroyImmediate(createdObject, true);
            
            
            //Add the item to the inventory pickup
            if (characterItem != null) {

                characterItem.DropPrefab = newPickup;

                if (characterItem.ItemDefinition is ItemDefinition itemDefinition) {
                    var inventory = newPickup.GetComponent<Inventory>();
                    inventory.Initialize(true);

                    var itemCollection = inventory.MainItemCollection;
                    var itemAmounts = new ItemAmount[1];

                    itemAmounts[0] = new ItemAmount(ItemField.CreateItem(itemDefinition),1);
                    itemCollection.DefaultLoadout = itemAmounts;

                    inventory.Serialize();
                    Shared.Editor.Utility.EditorUtility.SetDirty(inventory);
                    Shared.Editor.Utility.EditorUtility.SetDirty(inventory.gameObject);
                }
                
                Shared.Editor.Utility.EditorUtility.SetDirty(characterItem);
                Shared.Editor.Utility.EditorUtility.SetDirty(characterItem.gameObject);
            }

            return newPickup;
        }
    }
}