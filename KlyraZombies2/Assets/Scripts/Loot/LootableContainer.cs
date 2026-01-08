using UnityEngine;
using Opsive.UltimateInventorySystem.Core;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;
using Opsive.UltimateInventorySystem.Core.DataStructures;

/// <summary>
/// Populates an Opsive Inventory with random loot from a LootTable on game start.
/// Attach this to any container with an Inventory component.
/// </summary>
[RequireComponent(typeof(Inventory))]
public class LootableContainer : MonoBehaviour
{
    [Header("Loot Settings")]
    [Tooltip("The loot table to roll items from")]
    public LootTable lootTable;

    [Tooltip("If true, populate loot on Start. If false, call PopulateLoot() manually.")]
    public bool populateOnStart = true;

    [Tooltip("If true, use a seed based on position for consistent loot per playthrough")]
    public bool usePositionSeed = false;

    [Header("Debug")]
    [Tooltip("Log what loot was generated")]
    public bool debugLog = true;

    private Inventory inventory;
    private bool hasPopulated = false;

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
    }

    private void Start()
    {
        if (populateOnStart)
        {
            // Delay slightly to ensure InventorySystemManager is ready
            Invoke(nameof(PopulateLoot), 0.1f);
        }
    }

    /// <summary>
    /// Populates the container with random loot from the assigned loot table.
    /// </summary>
    public void PopulateLoot()
    {
        if (hasPopulated)
        {
            if (debugLog) Debug.Log($"[{gameObject.name}] Already populated, skipping.");
            return;
        }

        if (lootTable == null)
        {
            Debug.LogWarning($"[{gameObject.name}] No loot table assigned!");
            return;
        }

        if (debugLog) Debug.Log($"[{gameObject.name}] Using loot table: {lootTable.tableName} ({lootTable.possibleItems.Count} possible items)");

        if (inventory == null)
        {
            inventory = GetComponent<Inventory>();
            if (inventory == null)
            {
                Debug.LogError($"[{gameObject.name}] No Inventory component found!");
                return;
            }
        }

        // Set seed based on position if desired (same position = same loot)
        if (usePositionSeed)
        {
            int seed = Mathf.RoundToInt(transform.position.x * 1000 + transform.position.z * 100);
            Random.InitState(seed);
        }

        // Roll loot from table
        var loot = lootTable.RollLoot();

        if (debugLog)
        {
            if (loot.Count == 0)
            {
                Debug.Log($"[{gameObject.name}] Container is empty (rolled nothing)");
            }
            else
            {
                string items = "";
                foreach (var (item, amount) in loot)
                {
                    items += $"{item.name} x{amount}, ";
                }
                Debug.Log($"[{gameObject.name}] Populated with: {items}");
            }
        }

        // Add items to inventory
        var mainCollection = inventory.MainItemCollection;

        if (mainCollection == null)
        {
            Debug.LogError($"[{gameObject.name}] MainItemCollection is null! Make sure Inventory has a 'Main' collection.");
            return;
        }

        foreach (var (itemDef, amount) in loot)
        {
            if (itemDef == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Null item definition in loot roll!");
                continue;
            }

            // Cast to ItemDefinition (UIS type)
            if (itemDef is ItemDefinition definition)
            {
                try
                {
                    // Create item instance and add to collection
                    var item = InventorySystemManager.CreateItem(definition);
                    if (item == null)
                    {
                        Debug.LogError($"[{gameObject.name}] Failed to create item from {definition.name}");
                        continue;
                    }

                    var result = mainCollection.AddItem(item, amount);
                    if (debugLog) Debug.Log($"[{gameObject.name}] Added {amount}x {definition.name}, result: {result}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[{gameObject.name}] Error adding {definition.name}: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] {itemDef.name} is not a valid ItemDefinition (type: {itemDef.GetType()})!");
            }
        }

        hasPopulated = true;
        if (debugLog) Debug.Log($"[{gameObject.name}] Loot population complete. Items in inventory: {mainCollection.GetAllItemStacks().Count}");
    }

    /// <summary>
    /// Clears and re-populates loot (for testing or respawning)
    /// </summary>
    public void RerollLoot()
    {
        if (inventory != null)
        {
            inventory.MainItemCollection.RemoveAll();
        }
        hasPopulated = false;
        PopulateLoot();
    }
}
