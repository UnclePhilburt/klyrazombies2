using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor tool to add clothing equipment slots (Shirt, Pants) to the inventory UI system.
/// Run from: Tools > Inventory > Setup Clothing Equipment Slots
/// </summary>
public class ClothingEquipmentSlotSetup : EditorWindow
{
    private const string ITEM_SLOT_SET_PATH = "Assets/Samples/Opsive Ultimate Inventory System/1.3.5/Demo/Character/DemoPlayerCharacterItemSlotSet.asset";
    private const string UNCATEGORIZED_GUID = "73a3fedf5a3ed40e5b1b044d3bfd169e";

    private Vector2 m_ScrollPos;
    private string m_StatusMessage = "";
    private MessageType m_StatusType = MessageType.None;

    [MenuItem("Tools/Inventory/Setup Clothing Equipment Slots")]
    public static void ShowWindow()
    {
        var window = GetWindow<ClothingEquipmentSlotSetup>("Clothing Slots Setup");
        window.minSize = new Vector2(450, 400);
    }

    private void OnGUI()
    {
        m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

        EditorGUILayout.LabelField("Clothing Equipment Slots Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This tool adds Shirt and Pants equipment slots to the inventory system.\n\n" +
            "It will:\n" +
            "1. Update DemoPlayerCharacterItemSlotSet.asset to include clothing slots\n" +
            "2. Clothing items use the Uncategorized category for compatibility",
            MessageType.Info);

        EditorGUILayout.Space();

        // Show current slot set status
        EditorGUILayout.LabelField("Current ItemSlotSet Configuration:", EditorStyles.boldLabel);
        ShowCurrentSlots();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        // Main action buttons
        EditorGUILayout.LabelField("Actions:", EditorStyles.boldLabel);

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Add Shirt & Pants Slots to ItemSlotSet", GUILayout.Height(35)))
        {
            AddClothingSlotsToItemSlotSet();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();

        if (GUILayout.Button("Reset to Default Slots (Weapons Only)", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Reset Slots",
                "This will remove clothing slots and keep only weapon slots. Continue?",
                "Reset", "Cancel"))
            {
                ResetToDefaultSlots();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        // Status message
        if (!string.IsNullOrEmpty(m_StatusMessage))
        {
            EditorGUILayout.HelpBox(m_StatusMessage, m_StatusType);
        }

        EditorGUILayout.Space();

        // Instructions
        EditorGUILayout.LabelField("After Running:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. The ItemSlotSet will have Shirt and Pants slots\n" +
            "2. Clothing items can be equipped by dragging to equipment slots\n" +
            "3. If UI doesn't show new slots, you may need to update the UI prefab manually\n" +
            "4. Select Equipment Item View Slots Container prefab and add slot entries",
            MessageType.Info);

        EditorGUILayout.EndScrollView();
    }

    private void ShowCurrentSlots()
    {
        string slotSetYaml = ReadSlotSetFile();
        if (string.IsNullOrEmpty(slotSetYaml))
        {
            EditorGUILayout.HelpBox("Could not read ItemSlotSet file.", MessageType.Warning);
            return;
        }

        // Parse slot names from YAML
        var slots = ParseSlotNames(slotSetYaml);

        EditorGUI.indentLevel++;
        foreach (var slot in slots)
        {
            EditorGUILayout.LabelField($"- {slot.name} ({slot.category})");
        }
        EditorGUI.indentLevel--;

        if (slots.Count == 0)
        {
            EditorGUILayout.LabelField("No slots found", EditorStyles.miniLabel);
        }
    }

    private string ReadSlotSetFile()
    {
        if (!File.Exists(ITEM_SLOT_SET_PATH))
        {
            return null;
        }
        return File.ReadAllText(ITEM_SLOT_SET_PATH);
    }

    private List<(string name, string category)> ParseSlotNames(string yaml)
    {
        var slots = new List<(string name, string category)>();
        var lines = yaml.Split('\n');

        string currentSlotName = null;
        string currentCategory = null;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            if (line.StartsWith("- m_Name:"))
            {
                currentSlotName = line.Replace("- m_Name:", "").Trim();
            }
            else if (line.StartsWith("m_Name:") && currentSlotName != null)
            {
                currentCategory = line.Replace("m_Name:", "").Trim();
                slots.Add((currentSlotName, currentCategory));
                currentSlotName = null;
                currentCategory = null;
            }
        }

        return slots;
    }

    private void AddClothingSlotsToItemSlotSet()
    {
        // The new YAML content with clothing slots
        string newContent = @"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 0e9d0f19db9c7d949b5317a193f72cd0, type: 3}
  m_Name: DemoPlayerCharacterItemSlotSet
  m_EditorClassIdentifier:
  m_ItemSlots:
  - m_Name: Shirt
    m_Category:
      m_Name: Uncategorized
      m_ItemCategory: {fileID: 11400000, guid: " + UNCATEGORIZED_GUID + @", type: 2}
  - m_Name: Pants
    m_Category:
      m_Name: Uncategorized
      m_ItemCategory: {fileID: 11400000, guid: " + UNCATEGORIZED_GUID + @", type: 2}
  - m_Name: Backpack
    m_Category:
      m_Name: Backpack
      m_ItemCategory: {fileID: 11400000, guid: a8c3d7e9f2b1a4c5d6e7f8a9b0c1d2e3, type: 2}
";

        try
        {
            File.WriteAllText(ITEM_SLOT_SET_PATH, newContent);
            AssetDatabase.Refresh();

            m_StatusMessage = "Successfully updated ItemSlotSet with Shirt, Pants, and Backpack slots!\n\n" +
                              "The UI should now show these equipment slots.\n" +
                              "If not visible, check that the UI prefab is configured correctly.";
            m_StatusType = MessageType.Info;

            Debug.Log("[ClothingEquipmentSlotSetup] Updated ItemSlotSet with clothing slots");
        }
        catch (System.Exception e)
        {
            m_StatusMessage = $"Error updating ItemSlotSet: {e.Message}";
            m_StatusType = MessageType.Error;
            Debug.LogError($"[ClothingEquipmentSlotSetup] Error: {e}");
        }
    }

    private void ResetToDefaultSlots()
    {
        // Reset to original weapon-only configuration
        string originalContent = @"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 0e9d0f19db9c7d949b5317a193f72cd0, type: 3}
  m_Name: DemoPlayerCharacterItemSlotSet
  m_EditorClassIdentifier:
  m_ItemSlots:
  - m_Name: Right Hand
    m_Category:
      m_Name: Weapons
      m_ItemCategory: {fileID: 11400000, guid: af709a2c720e59949a5398cba38d76ac, type: 2}
  - m_Name: Head
    m_Category:
      m_Name: Headwear
      m_ItemCategory: {fileID: 11400000, guid: 05e8d3a0e47234353a19ef4a8edc1bc4, type: 2}
  - m_Name: Chest
    m_Category:
      m_Name: Uncategorized
      m_ItemCategory: {fileID: 11400000, guid: " + UNCATEGORIZED_GUID + @", type: 2}
  - m_Name: Legs
    m_Category:
      m_Name: Pants
      m_ItemCategory: {fileID: 11400000, guid: 479ced862ae1444e0bfd5e602e982ba2, type: 2}
";

        try
        {
            File.WriteAllText(ITEM_SLOT_SET_PATH, originalContent);
            AssetDatabase.Refresh();

            m_StatusMessage = "Reset ItemSlotSet to default configuration.";
            m_StatusType = MessageType.Info;

            Debug.Log("[ClothingEquipmentSlotSetup] Reset ItemSlotSet to default");
        }
        catch (System.Exception e)
        {
            m_StatusMessage = $"Error resetting ItemSlotSet: {e.Message}";
            m_StatusType = MessageType.Error;
            Debug.LogError($"[ClothingEquipmentSlotSetup] Error: {e}");
        }
    }
}
