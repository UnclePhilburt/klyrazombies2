/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

#if !ULTIMATE_CHARACTER_CONTROLLER_EXTENSION_DEBUG

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem.Demo.Editor.References
{
#if !FIRST_PERSON_CONTROLLER || !THIRD_PERSON_CONTROLLER
    using Opsive.Shared.Utility;
    using Opsive.UltimateCharacterController.Camera.ViewTypes;
    using Opsive.UltimateCharacterController.Character.MovementTypes;
    using Opsive.UltimateCharacterController.Character.Abilities;
    using Opsive.UltimateCharacterController.Character.Abilities.Items;
    using Opsive.UltimateCharacterController.Editor.Inspectors.Utility;
    using Opsive.UltimateCharacterController.Editor.Managers;
    using Opsive.UltimateCharacterController.StateSystem;
#endif
    using Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem.Demo.References;
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    [InitializeOnLoad]
    public class InventoryItemRemover
    {
        private static Scene s_ActiveScene;

        /// <summary>
        /// Registers for the scene change callback.
        /// </summary>
        static InventoryItemRemover()
        {
            EditorApplication.update += Update;
        }

        /// <summary>
        /// The scene has been changed.
        /// </summary>
        private static void Update()
        {
            var scene = SceneManager.GetActiveScene();

            if (scene == s_ActiveScene || Application.isPlaying) {
                return;
            }
            s_ActiveScene = scene;

            // Only the add-ons and integrations demo scene should be affected.
            var scenePath = s_ActiveScene.path.Replace("\\", "/"); 
            if (!scenePath.Contains("UltimateCharacterController/Add-Ons") && !scenePath.Contains("UltimateCharacterController/Integrations")) {
                return;
            }

            // Find the object which contains the objects that should be removed.
           
#if UNITY_2023_1_OR_NEWER
                var inventoryItemReferences = GameObject.FindFirstObjectByType<InventoryItemReferences>();
#else
                 var inventoryItemReferences = GameObject.FindObjectOfType<InventoryItemReferences>();
#endif
            ProcessObjectReferences(inventoryItemReferences, true);
        }

        /// <summary>
        /// Removes the objects specified by the object references object.
        /// </summary>
        private static void ProcessObjectReferences(InventoryItemReferences objectReferences, bool fromScene)
        {
            if (objectReferences == null) { return; }

#if UNITY_2023_1_OR_NEWER
                var inventorySystemManager = GameObject.FindFirstObjectByType<InventorySystemManager>();
#else
                 var inventorySystemManager = GameObject.FindObjectOfType<InventorySystemManager>();
#endif
            
            if (inventorySystemManager == null) { return; }

            inventorySystemManager.Database.Initialize(false);

#if !FIRST_PERSON_CONTROLLER
            RemoveObjects(objectReferences.Inventories, objectReferences.FirstPersonObjects);
            objectReferences.FirstPersonObjects = null;
#endif
#if !THIRD_PERSON_CONTROLLER
            RemoveObjects(objectReferences.Inventories, objectReferences.ThirdPersonObjects);
            objectReferences.ThirdPersonObjects = null;
#endif
#if !ULTIMATE_CHARACTER_CONTROLLER_SHOOTER
            RemoveObjects(objectReferences.Inventories, objectReferences.ShooterObjects);
            objectReferences.ShooterObjects = null;
#endif
#if !ULTIMATE_CHARACTER_CONTROLLER_MELEE
            RemoveObjects(objectReferences.Inventories, objectReferences.MeleeObjects);
            objectReferences.MeleeObjects = null;
#endif

        }

        /// <summary>
        /// Removes the specified objects.
        /// </summary>
        private static void RemoveObjects(Inventory[] inventories, InventoryItemReferenceObjects referenceObjects)
        {
            if (referenceObjects == null || (referenceObjects.Definitions == null && referenceObjects.Categories == null) || inventories == null) {
                return;
            }

            for (int i = 0; i < inventories.Length; i++) {
                var inventory = inventories[i];
                if(inventory == null){continue;}

                inventory.Initialize(false);

                var save = false;
                
                var itemCollectionCount = inventory.GetItemCollectionCount();
                for (int j = 0; j < itemCollectionCount; j++) {
                    var itemCollection = inventory.GetItemCollection(j);
                    if (RemoveObjects(itemCollection, referenceObjects)) {
                        //Something was removed this inventory needs to be serialized and saved.
                        save = true;
                    }
                }

                if (save) {
                    inventory.Serialize();
                    Shared.Editor.Utility.EditorUtility.SetDirty(inventory);
                }
            }
        }

        /// <summary>
        /// Removes the specified objects.
        /// </summary>
        private static bool RemoveObjects(ItemCollection itemCollection, InventoryItemReferenceObjects referenceObjects)
        {
            var defaultItems = itemCollection.DefaultLoadout;

            var atLeastOnRemove = false;
            
            for (int i = 0; i < defaultItems.Count; i++) {
                var item = defaultItems[i].Item;

                var removeItem = CheckItemForMatch(referenceObjects, item);

                if (removeItem) {
                    defaultItems[i] = ItemAmount.None;
                    atLeastOnRemove = true;
                }
            }

            return atLeastOnRemove;

        }
        
        /// <summary>
        /// Finds if the item matches the reference objects.
        /// </summary>
        private static bool CheckItemForMatch(InventoryItemReferenceObjects referenceObjects, Item item)
        {
            var matchFound = false;

            for (int k = 0; k < referenceObjects.Categories.Length; k++) {
                var category = referenceObjects.Categories[k];
                if (category != null && category.InherentlyContains(item)) {
                    matchFound = true;
                    break;
                }
            }

            for (int k = 0; k < referenceObjects.Definitions.Length; k++) {
                var definition = referenceObjects.Definitions[k];
                if (definition != null && definition.InherentlyContains(item)) {
                    matchFound = true;
                    break;
                }
            }

            return matchFound;
        }

        /// <summary>
        /// Unpacks the prefab root.
        /// </summary>
        /// <param name="obj">The object that should be unpacked.</param>
        private static void UnpackPrefab(Object obj)
        {
            if (obj != null && PrefabUtility.IsPartOfAnyPrefab(obj)) {
                var root = PrefabUtility.GetNearestPrefabInstanceRoot(obj);
                if (root != null) {
                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }
            }
        }
    }
}
#endif