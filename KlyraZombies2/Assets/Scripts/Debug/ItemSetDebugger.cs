using UnityEngine;
using Opsive.UltimateCharacterController.Inventory;

/// <summary>
/// Debug script to diagnose ItemSet issues.
/// Press F10 to log the current state of ItemSets.
/// </summary>
public class ItemSetDebugger : MonoBehaviour
{
    private ItemSetManagerBase m_ItemSetManager;

    private void Start()
    {
        m_ItemSetManager = GetComponent<ItemSetManagerBase>();

        Debug.Log($"[ItemSetDebugger] Start - ItemSetManager found: {m_ItemSetManager != null}");

        if (m_ItemSetManager != null)
        {
            LogItemSetState();
        }
    }

    private void Update()
    {
        // Press F10 to log ItemSet state
        if (Input.GetKeyDown(KeyCode.F10))
        {
            LogItemSetState();
        }
    }

    private void LogItemSetState()
    {
        if (m_ItemSetManager == null)
        {
            Debug.LogError("[ItemSetDebugger] ItemSetManager is null!");
            return;
        }

        Debug.Log($"[ItemSetDebugger] ===== ItemSetManager State =====");
        Debug.Log($"[ItemSetDebugger] Initialized: {m_ItemSetManager.Initialized}");
        Debug.Log($"[ItemSetDebugger] CategoryCount: {m_ItemSetManager.CategoryCount}");
        Debug.Log($"[ItemSetDebugger] SlotCount: {m_ItemSetManager.SlotCount}");
        Debug.Log($"[ItemSetDebugger] CharacterInventory: {m_ItemSetManager.CharacterInventory}");

        var groups = m_ItemSetManager.ItemSetGroups;
        if (groups == null)
        {
            Debug.LogError("[ItemSetDebugger] ItemSetGroups is null!");
            return;
        }

        Debug.Log($"[ItemSetDebugger] ItemSetGroups.Length: {groups.Length}");

        for (int i = 0; i < groups.Length; i++)
        {
            var group = groups[i];
            Debug.Log($"[ItemSetDebugger] --- Group {i} ---");
            Debug.Log($"[ItemSetDebugger]   CategoryName: {group.CategoryName}");
            Debug.Log($"[ItemSetDebugger]   CategoryID: {group.CategoryID}");
            Debug.Log($"[ItemSetDebugger]   IsInitialized: {group.IsInitialized}");
            Debug.Log($"[ItemSetDebugger]   StartingItemSetRules: {group.StartingItemSetRules?.Length ?? -1}");
            Debug.Log($"[ItemSetDebugger]   ItemSetRules (runtime): {group.ItemSetRules?.Count ?? -1}");
            Debug.Log($"[ItemSetDebugger]   ItemSetList: {group.ItemSetList?.Count ?? -1}");
            Debug.Log($"[ItemSetDebugger]   ActiveItemSetIndex: {group.ActiveItemSetIndex}");
            Debug.Log($"[ItemSetDebugger]   DefaultItemSetIndex: {group.DefaultItemSetIndex}");
            Debug.Log($"[ItemSetDebugger]   EquipUnequip: {group.EquipUnequip != null}");

            if (group.StartingItemSetRules != null)
            {
                for (int j = 0; j < group.StartingItemSetRules.Length; j++)
                {
                    var rule = group.StartingItemSetRules[j];
                    Debug.Log($"[ItemSetDebugger]   StartingRule[{j}]: {rule?.GetType().Name ?? "NULL"} - {rule?.name ?? "null"}");
                }
            }

            if (group.ItemSetRules != null)
            {
                for (int j = 0; j < group.ItemSetRules.Count; j++)
                {
                    var rule = group.ItemSetRules[j];
                    Debug.Log($"[ItemSetDebugger]   RuntimeRule[{j}]: {rule?.GetType().Name ?? "NULL"}");
                }
            }

            if (group.ItemSetList != null)
            {
                for (int j = 0; j < group.ItemSetList.Count; j++)
                {
                    var itemSet = group.ItemSetList[j];
                    Debug.Log($"[ItemSetDebugger]   ItemSet[{j}]: State='{itemSet.State}', IsValid={itemSet.IsValid}, Index={itemSet.Index}");
                    if (itemSet.ItemIdentifiers != null)
                    {
                        for (int k = 0; k < itemSet.ItemIdentifiers.Length; k++)
                        {
                            var itemId = itemSet.ItemIdentifiers[k];
                            string itemName = itemId != null ? itemId.ToString() : "empty";
                            Debug.Log($"[ItemSetDebugger]     Slot[{k}]: {itemName}");
                        }
                    }
                }
            }
        }
        Debug.Log($"[ItemSetDebugger] =============================");
    }
}
