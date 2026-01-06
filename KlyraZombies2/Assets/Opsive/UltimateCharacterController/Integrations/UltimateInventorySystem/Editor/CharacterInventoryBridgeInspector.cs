/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem.Editor
{
    using Opsive.UltimateCharacterController.Editor.Inspectors.Inventory;
    using Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem;
    using UnityEditor;

    /// <summary>
    /// Custom inspector for the Inventory component.
    /// </summary>
    [CustomEditor(typeof(CharacterInventoryBridge))]
    public class CharacterInventoryBridgeInspector : InventoryBaseInspector
    {
        /// <summary>
        /// Draws the properties for the inventory subclass.
        /// </summary>
        protected override void DrawInventoryProperties()
        {
            if (Foldout("Categories")) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(PropertyFromName("m_EquippableCategory"));
                EditorGUI.indentLevel--;
            }
            if (Foldout("Attribute Names")) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(PropertyFromName("m_CharacterItemPrefabsAttributeName"));
                EditorGUI.indentLevel--;
            }
            
            if (Foldout("Item Collections")) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(PropertyFromName("m_DefaultItemCollectionName"));
                EditorGUILayout.PropertyField(PropertyFromName("m_BridgeItemCollectionNames"));
                EditorGUILayout.PropertyField(PropertyFromName("m_LoadoutItemCollectionNames"));
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.PropertyField(PropertyFromName("m_RemoveExceptions"));
            
            if (Foldout("Item Drop")) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(PropertyFromName("m_DropUsingCharacterItemWhenPossible"));
                EditorGUILayout.PropertyField(PropertyFromName("m_InventoryItemPickupPrefab"));
                EditorGUILayout.PropertyField(PropertyFromName("m_ItemDropPositionOffset"));
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.PropertyField(PropertyFromName("m_RemoveOnDeathOptions"));
        }
    }
}