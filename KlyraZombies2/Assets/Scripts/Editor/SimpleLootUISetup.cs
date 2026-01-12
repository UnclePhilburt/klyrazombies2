using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool to create UI GameObjects in the scene for easy editing.
/// Menu: Project Klyra > UI
/// </summary>
public class SimpleLootUISetup : Editor
{
    [MenuItem("Project Klyra/UI/Create Simple Loot UI")]
    public static void CreateSimpleLootUI()
    {
        // Check if one already exists
        var existing = FindFirstObjectByType<SimpleLootUI>();
        if (existing != null)
        {
            Debug.LogWarning("[UISetup] SimpleLootUI already exists in scene. Selecting it.");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // Create the GameObject
        GameObject lootUIObj = new GameObject("SimpleLootUI");

        // Add the component
        SimpleLootUI lootUI = lootUIObj.AddComponent<SimpleLootUI>();

        // Register undo
        Undo.RegisterCreatedObjectUndo(lootUIObj, "Create Simple Loot UI");

        // Select it
        Selection.activeGameObject = lootUIObj;

        Debug.Log("[UISetup] Created SimpleLootUI GameObject. Edit settings in the Inspector, then enter Play mode to test.");
    }

    [MenuItem("Project Klyra/UI/Create Simple Inventory UI")]
    public static void CreateSimpleInventoryUI()
    {
        // Check if one already exists
        var existing = FindFirstObjectByType<SimpleInventoryUI>();
        if (existing != null)
        {
            Debug.LogWarning("[UISetup] SimpleInventoryUI already exists in scene. Selecting it.");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // Create the GameObject
        GameObject invUIObj = new GameObject("SimpleInventoryUI");

        // Add the component
        SimpleInventoryUI invUI = invUIObj.AddComponent<SimpleInventoryUI>();

        // Register undo
        Undo.RegisterCreatedObjectUndo(invUIObj, "Create Simple Inventory UI");

        // Select it
        Selection.activeGameObject = invUIObj;

        Debug.Log("[UISetup] Created SimpleInventoryUI GameObject. Edit settings in the Inspector, then enter Play mode to test.");
    }

    [MenuItem("Project Klyra/UI/Create Character Info Popup")]
    public static void CreateCharacterInfoPopup()
    {
        // Check if one already exists
        var existing = FindFirstObjectByType<CharacterInfoPopup>();
        if (existing != null)
        {
            Debug.LogWarning("[UISetup] CharacterInfoPopup already exists in scene. Selecting it.");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // Create the GameObject
        GameObject popupObj = new GameObject("CharacterInfoPopup");

        // Add the component
        CharacterInfoPopup popup = popupObj.AddComponent<CharacterInfoPopup>();

        // Register undo
        Undo.RegisterCreatedObjectUndo(popupObj, "Create Character Info Popup");

        // Select it
        Selection.activeGameObject = popupObj;

        Debug.Log("[UISetup] Created CharacterInfoPopup GameObject. Edit settings in the Inspector, then enter Play mode to test.");
    }
}
