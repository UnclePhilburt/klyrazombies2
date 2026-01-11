using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool to quickly set up the SimpleInventoryUI
/// </summary>
public class SimpleInventorySetup : EditorWindow
{
    [MenuItem("Project Klyra/Inventory/Setup Simple Inventory UI")]
    public static void SetupInventoryUI()
    {
        // Check if SimpleInventoryUI already exists
        var existing = Object.FindFirstObjectByType<SimpleInventoryUI>();
        if (existing != null)
        {
            EditorUtility.DisplayDialog("Already Exists",
                "SimpleInventoryUI already exists in the scene on: " + existing.gameObject.name, "OK");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // Create new GameObject with SimpleInventoryUI
        var go = new GameObject("SimpleInventoryUI");
        go.AddComponent<SimpleInventoryUI>();

        Undo.RegisterCreatedObjectUndo(go, "Create Simple Inventory UI");
        Selection.activeGameObject = go;

        EditorUtility.DisplayDialog("Success",
            "SimpleInventoryUI created!\n\n" +
            "It will automatically:\n" +
            "- Create the UI when you play\n" +
            "- Find the player's inventory\n" +
            "- Open with Tab key\n\n" +
            "You can adjust grid size, colors, etc. in the Inspector.", "OK");
    }

    [MenuItem("Project Klyra/Inventory/Disable Old Inventory Panels")]
    public static void DisableOldPanels()
    {
        int count = 0;

        // Find and disable RPG Schema Full Layout panels
        var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            if (obj.name.Contains("RPG Schema") ||
                obj.name.Contains("Main Menu") ||
                obj.name.Contains("Inventory Panel"))
            {
                // Check if it has DisplayPanelManager or similar
                var panelManager = obj.GetComponent("DisplayPanelManager");
                var inventoryPanel = obj.GetComponent("InventoryPanelBinding");

                if (panelManager != null || inventoryPanel != null)
                {
                    Undo.RecordObject(obj, "Disable Old Inventory Panel");
                    obj.SetActive(false);
                    count++;
                    Debug.Log($"Disabled: {obj.name}");
                }
            }
        }

        if (count > 0)
        {
            EditorUtility.DisplayDialog("Done", $"Disabled {count} old inventory panel(s).\n\nYou can re-enable them later if needed.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Nothing Found", "No old inventory panels found to disable.", "OK");
        }
    }
}
