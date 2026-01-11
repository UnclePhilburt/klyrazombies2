using UnityEngine;
using Opsive.UltimateCharacterController.Inventory;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities.Items;
using Opsive.Shared.Events;

/// <summary>
/// Debug script to trace what happens when you press 1 to equip.
/// Add to player or any object in scene.
/// </summary>
public class EquipInputDebugger : MonoBehaviour
{
    private ItemSetManagerBase m_ItemSetManager;
    private UltimateCharacterLocomotion m_Locomotion;
    private EquipUnequip[] m_EquipUnequipAbilities;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[EquipInputDebugger] No player found!");
            return;
        }

        m_ItemSetManager = player.GetComponent<ItemSetManagerBase>();
        m_Locomotion = player.GetComponent<UltimateCharacterLocomotion>();

        if (m_Locomotion != null)
        {
            m_EquipUnequipAbilities = m_Locomotion.GetAbilities<EquipUnequip>();
            Debug.Log($"[EquipInputDebugger] Found {m_EquipUnequipAbilities?.Length ?? 0} EquipUnequip abilities");
        }

        // Register for equip events
        EventHandler.RegisterEvent<int, int>(player, "OnAbilityWillEquipItem", OnWillEquip);
        EventHandler.RegisterEvent<int, int>(player, "OnAbilityEquipItemComplete", OnEquipComplete);
        EventHandler.RegisterEvent<int, int>(player, "OnAbilityUnequipItemComplete", OnUnequipComplete);
        EventHandler.RegisterEvent<int, int>(player, "OnItemSetIndexChange", OnItemSetIndexChange);
    }

    private void Update()
    {
        // When user presses 1, log what SHOULD happen
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("[EquipInputDebugger] ========== KEY 1 PRESSED ==========");

            if (m_ItemSetManager == null)
            {
                Debug.LogError("[EquipInputDebugger] No ItemSetManager!");
                return;
            }

            var groups = m_ItemSetManager.ItemSetGroups;
            if (groups == null || groups.Length == 0)
            {
                Debug.LogError("[EquipInputDebugger] No ItemSetGroups!");
                return;
            }

            Debug.Log($"[EquipInputDebugger] ItemSetGroups: {groups.Length}");

            // For pressing 1, we expect it to equip ItemSet index 0 from the first group
            var firstGroup = groups[0];
            Debug.Log($"[EquipInputDebugger] First Group: {firstGroup.CategoryName}");
            Debug.Log($"[EquipInputDebugger]   IsInitialized: {firstGroup.IsInitialized}");
            Debug.Log($"[EquipInputDebugger]   ItemSetList.Count: {firstGroup.ItemSetList?.Count ?? -1}");
            Debug.Log($"[EquipInputDebugger]   ActiveItemSetIndex: {firstGroup.ActiveItemSetIndex}");
            Debug.Log($"[EquipInputDebugger]   EquipUnequip ability: {(firstGroup.EquipUnequip != null ? "found" : "NULL")}");

            if (firstGroup.ItemSetList != null && firstGroup.ItemSetList.Count > 0)
            {
                var targetItemSet = firstGroup.ItemSetList[0];
                Debug.Log($"[EquipInputDebugger]   Target ItemSet[0]: State='{targetItemSet.State}', IsValid={targetItemSet.IsValid}");
            }
            else
            {
                Debug.LogWarning("[EquipInputDebugger]   NO ITEMSETS IN LIST - cannot equip anything!");
            }

            // Check EquipUnequip abilities
            if (m_EquipUnequipAbilities != null)
            {
                foreach (var ability in m_EquipUnequipAbilities)
                {
                    Debug.Log($"[EquipInputDebugger] EquipUnequip ability: Enabled={ability.Enabled}, CanStartAbility={ability.CanStartAbility()}");
                }
            }

            Debug.Log("[EquipInputDebugger] ====================================");
        }
    }

    private void OnWillEquip(int itemSetIndex, int slotID)
    {
        Debug.Log($"[EquipInputDebugger] OnWillEquip: itemSetIndex={itemSetIndex}, slotID={slotID}");
    }

    private void OnEquipComplete(int itemSetIndex, int slotID)
    {
        Debug.Log($"[EquipInputDebugger] OnEquipComplete: itemSetIndex={itemSetIndex}, slotID={slotID}");
    }

    private void OnUnequipComplete(int itemSetIndex, int slotID)
    {
        Debug.Log($"[EquipInputDebugger] OnUnequipComplete: itemSetIndex={itemSetIndex}, slotID={slotID}");
    }

    private void OnItemSetIndexChange(int groupIndex, int itemSetIndex)
    {
        Debug.Log($"[EquipInputDebugger] OnItemSetIndexChange: groupIndex={groupIndex}, itemSetIndex={itemSetIndex}");
    }

    private void OnDestroy()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            EventHandler.UnregisterEvent<int, int>(player, "OnAbilityWillEquipItem", OnWillEquip);
            EventHandler.UnregisterEvent<int, int>(player, "OnAbilityEquipItemComplete", OnEquipComplete);
            EventHandler.UnregisterEvent<int, int>(player, "OnAbilityUnequipItemComplete", OnUnequipComplete);
            EventHandler.UnregisterEvent<int, int>(player, "OnItemSetIndexChange", OnItemSetIndexChange);
        }
    }
}
