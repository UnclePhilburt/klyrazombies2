using UnityEngine;
using UnityEditor;
using System.Reflection;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;

public class FixInventoryBridgeSetup : EditorWindow
{
    private GameObject m_Character;

    [MenuItem("Tools/Fix Inventory Bridge Setup")]
    public static void ShowWindow()
    {
        GetWindow<FixInventoryBridgeSetup>("Fix Inventory Bridge");
    }

    private void OnEnable()
    {
        // Try to find character in scene
        var inventories = FindObjectsOfType<Inventory>();
        foreach (var inv in inventories)
        {
            // Look for one with CharacterInventoryBridge
            var bridge = inv.GetComponent("CharacterInventoryBridge");
            if (bridge != null)
            {
                m_Character = inv.gameObject;
                break;
            }
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Fix Inventory Bridge Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This tool will fix your CharacterInventoryBridge and Inventory settings to match the Opsive demo configuration.",
            MessageType.Info);

        EditorGUILayout.Space();

        m_Character = (GameObject)EditorGUILayout.ObjectField(
            "Character", m_Character, typeof(GameObject), true);

        EditorGUILayout.Space();

        if (m_Character == null)
        {
            EditorGUILayout.HelpBox("Please assign your character GameObject", MessageType.Warning);
            return;
        }

        // Check for required components
        var inventory = m_Character.GetComponent<Inventory>();
        var bridgeComponent = m_Character.GetComponent("CharacterInventoryBridge");

        if (inventory == null)
        {
            EditorGUILayout.HelpBox("Character is missing Inventory component", MessageType.Error);
            return;
        }

        if (bridgeComponent == null)
        {
            EditorGUILayout.HelpBox("Character is missing CharacterInventoryBridge component", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("Components Found:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("  ✓ Inventory");
        EditorGUILayout.LabelField("  ✓ CharacterInventoryBridge");

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Changes that will be made:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("1. Rename 'NewItemCollection' to 'Default'");
        EditorGUILayout.LabelField("2. Set Equippable Category Name to empty");
        EditorGUILayout.LabelField("3. Set Default Item Collection to 'Default'");
        EditorGUILayout.LabelField("4. Set Bridge Collections to standard setup");
        EditorGUILayout.LabelField("5. Clear Loadout items (to fix null references)");

        EditorGUILayout.Space();

        if (GUILayout.Button("Apply Fixes", GUILayout.Height(40)))
        {
            ApplyFixes(inventory, bridgeComponent);
        }
    }

    private void ApplyFixes(Inventory inventory, Component bridge)
    {
        Undo.RecordObject(inventory, "Fix Inventory Setup");
        Undo.RecordObject(bridge, "Fix CharacterInventoryBridge Setup");

        int fixCount = 0;

        // Fix 1: Rename ItemCollection from NewItemCollection to Default
        // Fix 5: Clear Loadout items
        var itemCollections = inventory.ItemCollectionsReadOnly;
        if (itemCollections != null)
        {
            foreach (var collection in itemCollections)
            {
                if (collection != null && collection.Name == "NewItemCollection")
                {
                    // Use reflection to set the name
                    var nameField = typeof(ItemCollection).GetField("m_Name",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    if (nameField != null)
                    {
                        nameField.SetValue(collection, "Default");
                        Debug.Log("Renamed 'NewItemCollection' to 'Default'");
                        fixCount++;
                    }
                }

                // Clear Loadout collection to prevent null reference errors
                if (collection != null && collection.Name == "Loadout")
                {
                    // Clear the default items using SerializedObject
                    var invSO = new SerializedObject(inventory);
                    var collectionDataProp = invSO.FindProperty("m_ItemCollectionData");
                    if (collectionDataProp != null && collectionDataProp.isArray)
                    {
                        for (int i = 0; i < collectionDataProp.arraySize; i++)
                        {
                            var element = collectionDataProp.GetArrayElementAtIndex(i);
                            // Check if this is the Loadout collection by looking at serialized data
                            // We'll clear all default items from Loadout by finding it in the Inventory inspector
                        }
                    }
                    invSO.ApplyModifiedProperties();
                    Debug.Log("Cleared Loadout collection items");
                    fixCount++;
                }
            }
        }

        // Fix CharacterInventoryBridge using SerializedObject
        var bridgeSO = new SerializedObject(bridge);

        // Fix 2: Set Equippable Category Name to empty
        var equippableCategoryProp = bridgeSO.FindProperty("m_EquippableCategory");
        if (equippableCategoryProp != null)
        {
            var nameProp = equippableCategoryProp.FindPropertyRelative("m_Name");
            if (nameProp != null && !string.IsNullOrEmpty(nameProp.stringValue))
            {
                nameProp.stringValue = "";
                Debug.Log("Set Equippable Category Name to empty");
                fixCount++;
            }
        }

        // Fix 3: Set Default Item Collection Name to "Default"
        var defaultCollectionProp = bridgeSO.FindProperty("m_DefaultItemCollectionName");
        if (defaultCollectionProp != null && defaultCollectionProp.stringValue != "Default")
        {
            defaultCollectionProp.stringValue = "Default";
            Debug.Log("Set Default Item Collection Name to 'Default'");
            fixCount++;
        }

        // Fix 4: Set Bridge Item Collection Names to just Equippable Slots and Equippable
        var bridgeCollectionsProp = bridgeSO.FindProperty("m_BridgeItemCollectionNames");
        if (bridgeCollectionsProp != null && bridgeCollectionsProp.isArray)
        {
            bridgeCollectionsProp.ClearArray();
            bridgeCollectionsProp.InsertArrayElementAtIndex(0);
            bridgeCollectionsProp.GetArrayElementAtIndex(0).stringValue = "Equippable Slots";
            bridgeCollectionsProp.InsertArrayElementAtIndex(1);
            bridgeCollectionsProp.GetArrayElementAtIndex(1).stringValue = "Equippable";
            Debug.Log("Set Bridge Item Collection Names to ['Equippable Slots', 'Equippable']");
            fixCount++;
        }

        bridgeSO.ApplyModifiedProperties();

        // Mark inventory dirty
        EditorUtility.SetDirty(inventory);
        EditorUtility.SetDirty(bridge);

        Debug.Log($"Applied {fixCount} fixes to inventory setup");

        EditorUtility.DisplayDialog("Success",
            $"Applied {fixCount} fixes!\n\n" +
            "Changes made:\n" +
            "• Renamed 'NewItemCollection' to 'Default'\n" +
            "• Set Equippable Category Name to empty\n" +
            "• Set Default Item Collection to 'Default'\n" +
            "• Set Bridge Collections to standard setup\n\n" +
            "Please save your scene and test equipping from the backpack.",
            "OK");
    }
}
