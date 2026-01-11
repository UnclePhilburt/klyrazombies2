using UnityEngine;
using Opsive.UltimateCharacterController.Inventory;
using System.Collections;

/// <summary>
/// Auto-logs ItemSet state at game start.
/// Attach to any object in the scene, it will find the player automatically.
/// </summary>
public class RuntimeItemSetLogger : MonoBehaviour
{
    [SerializeField] private float m_DelaySeconds = 2f;

    private void Start()
    {
        StartCoroutine(LogAfterDelay());
    }

    private IEnumerator LogAfterDelay()
    {
        Debug.Log($"[RuntimeItemSetLogger] Waiting {m_DelaySeconds}s before logging...");
        yield return new WaitForSeconds(m_DelaySeconds);

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[RuntimeItemSetLogger] No player found!");
            yield break;
        }

        Debug.Log($"[RuntimeItemSetLogger] Found player: {player.name}");

        var itemSetManager = player.GetComponent<ItemSetManagerBase>();
        if (itemSetManager == null)
        {
            Debug.LogError("[RuntimeItemSetLogger] No ItemSetManager on player!");
            yield break;
        }

        LogItemSetState(itemSetManager);
    }

    private void LogItemSetState(ItemSetManagerBase itemSetManager)
    {
        Debug.Log($"[RuntimeItemSetLogger] ===== ItemSetManager State =====");
        Debug.Log($"[RuntimeItemSetLogger] Initialized: {itemSetManager.Initialized}");
        Debug.Log($"[RuntimeItemSetLogger] CategoryCount: {itemSetManager.CategoryCount}");

        try
        {
            Debug.Log($"[RuntimeItemSetLogger] SlotCount: {itemSetManager.SlotCount}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RuntimeItemSetLogger] Error getting SlotCount: {e.Message}");
        }

        Debug.Log($"[RuntimeItemSetLogger] CharacterInventory: {itemSetManager.CharacterInventory?.GetType().Name ?? "NULL"}");

        var groups = itemSetManager.ItemSetGroups;
        if (groups == null)
        {
            Debug.LogError("[RuntimeItemSetLogger] ItemSetGroups is null!");
            return;
        }

        Debug.Log($"[RuntimeItemSetLogger] ItemSetGroups.Length: {groups.Length}");

        for (int i = 0; i < groups.Length; i++)
        {
            var group = groups[i];
            Debug.Log($"[RuntimeItemSetLogger] --- Group {i} ---");
            Debug.Log($"[RuntimeItemSetLogger]   CategoryName: {group.CategoryName}");
            Debug.Log($"[RuntimeItemSetLogger]   CategoryID: {group.CategoryID}");
            Debug.Log($"[RuntimeItemSetLogger]   IsInitialized: {group.IsInitialized}");

            Debug.Log($"[RuntimeItemSetLogger]   StartingItemSetRules.Length: {group.StartingItemSetRules?.Length ?? -1}");
            if (group.StartingItemSetRules != null)
            {
                for (int j = 0; j < group.StartingItemSetRules.Length; j++)
                {
                    var rule = group.StartingItemSetRules[j];
                    Debug.Log($"[RuntimeItemSetLogger]   StartingRule[{j}]: {(rule != null ? $"{rule.GetType().Name} - {rule.name}" : "NULL")}");
                }
            }

            Debug.Log($"[RuntimeItemSetLogger]   ItemSetRules.Count (runtime): {group.ItemSetRules?.Count ?? -1}");
            if (group.ItemSetRules != null)
            {
                for (int j = 0; j < group.ItemSetRules.Count; j++)
                {
                    var rule = group.ItemSetRules[j];
                    Debug.Log($"[RuntimeItemSetLogger]   RuntimeRule[{j}]: {(rule != null ? rule.GetType().Name : "NULL")}");
                }
            }

            Debug.Log($"[RuntimeItemSetLogger]   ItemSetList.Count: {group.ItemSetList?.Count ?? -1}");
            if (group.ItemSetList != null)
            {
                for (int j = 0; j < group.ItemSetList.Count; j++)
                {
                    var itemSet = group.ItemSetList[j];
                    Debug.Log($"[RuntimeItemSetLogger]   ItemSet[{j}]: State='{itemSet.State}', IsValid={itemSet.IsValid}");
                    if (itemSet.ItemIdentifiers != null)
                    {
                        for (int k = 0; k < itemSet.ItemIdentifiers.Length; k++)
                        {
                            var item = itemSet.ItemIdentifiers[k];
                            string itemName = item != null ? item.ToString() : "empty";
                            Debug.Log($"[RuntimeItemSetLogger]     Slot[{k}]: {itemName}");
                        }
                    }
                }
            }

            Debug.Log($"[RuntimeItemSetLogger]   ActiveItemSetIndex: {group.ActiveItemSetIndex}");
            Debug.Log($"[RuntimeItemSetLogger]   EquipUnequip: {(group.EquipUnequip != null ? "found" : "NULL")}");
        }

        Debug.Log($"[RuntimeItemSetLogger] =============================");
    }
}
