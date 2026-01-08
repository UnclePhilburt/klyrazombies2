using UnityEngine;
using UnityEditor;
using Opsive.UltimateInventorySystem.UI.Item;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;

/// <summary>
/// Editor tool to set up inventory UI for the backpack system.
/// Configures DynamicInventorySizeInventoryGridBinding on inventory grids.
/// </summary>
public class InventoryBackpackSetup : EditorWindow
{
    private InventoryGrid targetGrid;
    private int dynamicSizeID = 0;
    private bool setMaxElementCount = true;
    private bool showResults = false;
    private string resultMessage = "";

    [MenuItem("Tools/Inventory Backpack Setup")]
    public static void ShowWindow()
    {
        GetWindow<InventoryBackpackSetup>("Backpack Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Inventory Backpack Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This tool configures your inventory grid to work with the backpack system.\n\n" +
            "It adds DynamicInventorySizeInventoryGridBinding which automatically:\n" +
            "• Limits visible slots based on equipped backpack\n" +
            "• Updates slot count when backpack changes\n" +
            "• Grays out unavailable slots",
            MessageType.Info);

        EditorGUILayout.Space();

        // Target grid selection
        targetGrid = (InventoryGrid)EditorGUILayout.ObjectField(
            "Target Inventory Grid",
            targetGrid,
            typeof(InventoryGrid),
            true);

        EditorGUILayout.Space();

        // Settings
        GUILayout.Label("Settings", EditorStyles.boldLabel);
        dynamicSizeID = EditorGUILayout.IntField(
            new GUIContent("Dynamic Size ID", "Must match the ID on the player's DynamicInventorySize component"),
            dynamicSizeID);
        setMaxElementCount = EditorGUILayout.Toggle(
            new GUIContent("Set Max Element Count", "Automatically limit grid slots based on inventory size"),
            setMaxElementCount);

        EditorGUILayout.Space();

        // Buttons
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Find Inventory Grids in Scene"))
        {
            FindInventoryGrids();
        }

        if (GUILayout.Button("Setup Selected Grid"))
        {
            if (targetGrid != null)
            {
                SetupGrid(targetGrid);
            }
            else
            {
                resultMessage = "Please select a target grid first!";
                showResults = true;
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (GUILayout.Button("Setup All Grids in Scene"))
        {
            SetupAllGrids();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Check Player DynamicInventorySize"))
        {
            CheckPlayerDynamicInventorySize();
        }

        // Results
        if (showResults)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(resultMessage, MessageType.Info);
        }
    }

    private void FindInventoryGrids()
    {
        var grids = FindObjectsByType<InventoryGrid>(FindObjectsSortMode.None);
        resultMessage = $"Found {grids.Length} InventoryGrid(s) in scene:\n\n";

        foreach (var grid in grids)
        {
            var hasBinding = grid.GetComponent<DynamicInventorySizeInventoryGridBinding>() != null;
            var bindingStatus = hasBinding ? " [Has DynamicSizeBinding]" : " [No DynamicSizeBinding]";
            resultMessage += $"• {grid.gameObject.name}{bindingStatus}\n";
        }

        if (grids.Length > 0)
        {
            targetGrid = grids[0];
            resultMessage += $"\nSelected first grid: {targetGrid.gameObject.name}";
        }

        showResults = true;
    }

    private void SetupGrid(InventoryGrid grid)
    {
        if (grid == null) return;

        Undo.RecordObject(grid.gameObject, "Setup Inventory Grid for Backpack");

        // Check for existing binding
        var existingBinding = grid.GetComponent<DynamicInventorySizeInventoryGridBinding>();

        if (existingBinding != null)
        {
            // Update existing binding
            existingBinding.DynamicInventorySizeID = dynamicSizeID;
            EditorUtility.SetDirty(existingBinding);
            resultMessage = $"Updated existing DynamicInventorySizeInventoryGridBinding on {grid.gameObject.name}\n" +
                           $"• Dynamic Size ID: {dynamicSizeID}";
        }
        else
        {
            // Add new binding
            var binding = Undo.AddComponent<DynamicInventorySizeInventoryGridBinding>(grid.gameObject);
            binding.DynamicInventorySizeID = dynamicSizeID;
            EditorUtility.SetDirty(binding);
            resultMessage = $"Added DynamicInventorySizeInventoryGridBinding to {grid.gameObject.name}\n" +
                           $"• Dynamic Size ID: {dynamicSizeID}";
        }

        // Remove any IndexedInventoryGrid if present (legacy)
        var indexedGrid = grid.GetComponent("IndexedInventoryGrid");
        if (indexedGrid != null)
        {
            Undo.DestroyObjectImmediate(indexedGrid);
            resultMessage += "\n• Removed legacy IndexedInventoryGrid component";
        }

        // Remove any BackpackSlotsDisplay if present (legacy)
        var backpackDisplay = grid.GetComponent("BackpackSlotsDisplay");
        if (backpackDisplay != null)
        {
            Undo.DestroyObjectImmediate(backpackDisplay);
            resultMessage += "\n• Removed legacy BackpackSlotsDisplay component";
        }

        showResults = true;
        EditorUtility.SetDirty(grid.gameObject);
    }

    private void SetupAllGrids()
    {
        var grids = FindObjectsByType<InventoryGrid>(FindObjectsSortMode.None);
        int setupCount = 0;

        foreach (var grid in grids)
        {
            // Skip grids that are part of storage/shop menus (they don't need dynamic sizing)
            if (grid.gameObject.name.Contains("Storage") ||
                grid.gameObject.name.Contains("Shop") ||
                grid.gameObject.name.Contains("Chest"))
            {
                continue;
            }

            SetupGrid(grid);
            setupCount++;
        }

        resultMessage = $"Set up {setupCount} inventory grid(s) for backpack system.";
        showResults = true;
    }

    private void CheckPlayerDynamicInventorySize()
    {
        // Find player inventory
        var inventories = FindObjectsByType<Inventory>(FindObjectsSortMode.None);
        resultMessage = "DynamicInventorySize components found:\n\n";
        bool foundAny = false;

        foreach (var inv in inventories)
        {
            var dynamicSizes = inv.GetComponents<DynamicInventorySize>();
            if (dynamicSizes.Length > 0)
            {
                resultMessage += $"On '{inv.gameObject.name}':\n";
                foreach (var ds in dynamicSizes)
                {
                    resultMessage += $"  • ID: {ds.ID}, Base Max: {ds.BaseMaxStackAmount}, " +
                                    $"Current Max: {ds.MaxStackAmount}\n";

                    // Check configuration
                    var bagsCollection = ds.BagsItemCollectionNames;
                    if (bagsCollection != null && bagsCollection.Length > 0)
                    {
                        resultMessage += $"    Bags Collection: [{string.Join(", ", bagsCollection)}]\n";
                    }
                    else
                    {
                        resultMessage += $"    WARNING: No Bags Collection configured!\n";
                    }
                }
                foundAny = true;
            }
        }

        if (!foundAny)
        {
            resultMessage = "No DynamicInventorySize components found in scene.\n\n" +
                           "Add a DynamicInventorySize component to your player's Inventory GameObject with:\n" +
                           "• Base Max Stack Amount: Your base inventory size (e.g., 4)\n" +
                           "• Use Bag Items: true\n" +
                           "• Bag Item Category: Backpack\n" +
                           "• Bag Size Item Attribute Name: BagSize\n" +
                           "• Bags Item Collection Names: [\"Equippable\"]";
        }

        showResults = true;
    }
}
