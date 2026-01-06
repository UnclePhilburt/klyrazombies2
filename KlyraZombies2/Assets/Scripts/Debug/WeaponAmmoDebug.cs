using UnityEngine;
using Opsive.UltimateCharacterController.Items.Actions;
using Opsive.UltimateCharacterController.Items.Actions.Modules.Shootable;
using Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;

/// <summary>
/// Debug script to diagnose ammo/reload issues with UIS integration.
/// Attach to the weapon GameObject or character.
/// </summary>
public class WeaponAmmoDebug : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool m_LogOnUpdate = false;
    [SerializeField] private KeyCode m_DebugKey = KeyCode.F8;

    private ShootableAction m_ShootableAction;
    private Inventory m_Inventory;

    private void Start()
    {
        // Try to find ShootableAction on this object or parent
        m_ShootableAction = GetComponent<ShootableAction>();
        if (m_ShootableAction == null)
            m_ShootableAction = GetComponentInChildren<ShootableAction>();
        if (m_ShootableAction == null)
            m_ShootableAction = GetComponentInParent<ShootableAction>();

        // Try to find UIS Inventory
        m_Inventory = FindFirstObjectByType<Inventory>();

        Debug.Log($"[WeaponAmmoDebug] Started. ShootableAction: {(m_ShootableAction != null ? "Found" : "NOT FOUND")}, Inventory: {(m_Inventory != null ? "Found" : "NOT FOUND")}");

        if (m_ShootableAction != null)
        {
            LogWeaponState();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(m_DebugKey))
        {
            LogFullDebugInfo();
        }

        if (m_LogOnUpdate && m_ShootableAction != null)
        {
            LogWeaponState();
        }
    }

    [ContextMenu("Log Full Debug Info")]
    public void LogFullDebugInfo()
    {
        Debug.Log("========== WEAPON AMMO DEBUG ==========");

        // Find all ShootableActions in scene
        var shootables = FindObjectsByType<ShootableAction>(FindObjectsSortMode.None);
        Debug.Log($"[Debug] Found {shootables.Length} ShootableAction(s) in scene");

        foreach (var shootable in shootables)
        {
            Debug.Log($"[Debug] --- Weapon: {shootable.gameObject.name} ---");
            Debug.Log($"[Debug] IsActive: {shootable.gameObject.activeInHierarchy}");
            Debug.Log($"[Debug] ClipRemainingCount: {shootable.ClipRemainingCount}");
            Debug.Log($"[Debug] ClipSize: {shootable.ClipSize}");

            // Check ammo module
            var ammoModule = shootable.AmmoModuleGroup?.FirstEnabledModule;
            if (ammoModule != null)
            {
                Debug.Log($"[Debug] AmmoModule Type: {ammoModule.GetType().Name}");
                Debug.Log($"[Debug] HasAmmoRemaining: {ammoModule.HasAmmoRemaining()}");
                Debug.Log($"[Debug] GetAmmoRemainingCount: {ammoModule.GetAmmoRemainingCount()}");
                Debug.Log($"[Debug] AmmoItemDefinition: {ammoModule.AmmoItemDefinition?.name ?? "NULL"}");

                if (ammoModule is InventoryItemAmmo inventoryAmmo)
                {
                    Debug.Log($"[Debug] Using InventoryItemAmmo (UIS Integration) - CORRECT!");

                    // Use reflection to check internal state
                    var ammoItemField = typeof(InventoryItemAmmo).GetField("m_AmmoItem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var ammoCollectionsField = typeof(InventoryItemAmmo).GetField("m_AmmoItemCollections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var inventoryField = typeof(InventoryItemAmmo).GetField("m_Inventory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (ammoItemField != null)
                    {
                        var ammoItem = ammoItemField.GetValue(inventoryAmmo) as Opsive.UltimateInventorySystem.Core.Item;
                        Debug.Log($"[Debug] m_AmmoItem: {(ammoItem != null ? ammoItem.name + " (ID:" + ammoItem.ID + ")" : "NULL")}");
                        if (ammoItem != null)
                            Debug.Log($"[Debug] m_AmmoItem.ItemDefinition: {ammoItem.ItemDefinition?.name ?? "NULL"}");
                    }

                    if (inventoryField != null)
                    {
                        var inv = inventoryField.GetValue(inventoryAmmo) as Inventory;
                        Debug.Log($"[Debug] m_Inventory: {(inv != null ? inv.gameObject.name : "NULL")}");
                    }

                    if (ammoCollectionsField != null)
                    {
                        var collections = ammoCollectionsField.GetValue(inventoryAmmo) as Opsive.UltimateInventorySystem.Core.InventoryCollections.ItemCollectionGroup;
                        if (collections != null)
                        {
                            Debug.Log($"[Debug] m_AmmoItemCollections count: {collections.ItemCollections?.Count ?? 0}");
                            if (collections.ItemCollections != null)
                            {
                                foreach (var col in collections.ItemCollections)
                                {
                                    Debug.Log($"[Debug]   - Collection: '{col?.Name ?? "NULL"}'");
                                }
                            }

                            // Direct test - check what's in the collection and compare
                            var ammoItem = ammoItemField?.GetValue(inventoryAmmo) as Opsive.UltimateInventorySystem.Core.Item;
                            if (ammoItem != null && collections.ItemCollections?.Count > 0)
                            {
                                var col = collections.ItemCollections[0];
                                var stacks = col.GetAllItemStacks();
                                Debug.Log($"[Debug] === ITEM MATCHING TEST ===");
                                Debug.Log($"[Debug] Looking for m_AmmoItem - DefID: {ammoItem.ItemDefinition?.ID}, DefName: {ammoItem.ItemDefinition?.name}");

                                foreach (var stack in stacks)
                                {
                                    var invItem = stack.Item;
                                    bool defMatch = invItem?.ItemDefinition == ammoItem.ItemDefinition;
                                    bool stackableMatch = invItem != null && ammoItem.StackableEquivalentTo(invItem);

                                    Debug.Log($"[Debug] Stack: {invItem?.name} x{stack.Amount}");
                                    Debug.Log($"[Debug]   DefID: {invItem?.ItemDefinition?.ID}, DefName: {invItem?.ItemDefinition?.name}");
                                    Debug.Log($"[Debug]   ItemDefinition == match: {defMatch}");
                                    Debug.Log($"[Debug]   StackableEquivalentTo: {stackableMatch}");

                                    if (invItem?.ItemDefinition?.name == "9mm Rounds")
                                    {
                                        // Direct amount check
                                        int directAmount = col.GetItemAmount(invItem);
                                        int ammoItemAmount = col.GetItemAmount(ammoItem);
                                        Debug.Log($"[Debug]   GetItemAmount(invItem): {directAmount}");
                                        Debug.Log($"[Debug]   GetItemAmount(m_AmmoItem): {ammoItemAmount}");
                                    }
                                }
                            }
                        }
                        else
                        {
                            Debug.LogError("[Debug] m_AmmoItemCollections is NULL!");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[Debug] NOT using InventoryItemAmmo! Using: {ammoModule.GetType().Name}");
                }
            }
            else
            {
                Debug.LogError($"[Debug] NO AMMO MODULE FOUND!");
            }

            // Check reloader module
            var reloaderModule = shootable.ReloaderModuleGroup?.FirstEnabledModule;
            if (reloaderModule != null)
            {
                Debug.Log($"[Debug] ReloaderModule Type: {reloaderModule.GetType().Name}");
                Debug.Log($"[Debug] CanReloadItem: {reloaderModule.CanReloadItem(false)}");
            }
        }

        // Check UIS Inventory
        if (m_Inventory == null)
            m_Inventory = FindFirstObjectByType<Inventory>();

        if (m_Inventory != null)
        {
            Debug.Log($"[Debug] --- UIS Inventory: {m_Inventory.gameObject.name} ---");

            // Get all items from the inventory
            var allItems = m_Inventory.AllItemInfos;
            Debug.Log($"[Debug] Total items in inventory: {allItems.Count}");

            foreach (var itemInfo in allItems)
            {
                var item = itemInfo.Item;
                var collection = itemInfo.ItemCollection;
                Debug.Log($"[Debug]   - {item?.name ?? "NULL"} x{itemInfo.Amount} in '{collection?.Name ?? "NULL"}' (Def: {item?.ItemDefinition?.name ?? "NULL"})");
            }

            // Check main collection specifically
            var mainCollection = m_Inventory.MainItemCollection;
            if (mainCollection != null)
            {
                Debug.Log($"[Debug] MainItemCollection name: '{mainCollection.Name}'");
            }

            // Check for "Default" collection
            var defaultCollection = m_Inventory.GetItemCollection("Default");
            if (defaultCollection != null)
            {
                Debug.Log($"[Debug] 'Default' collection found with {defaultCollection.GetAllItemStacks().Count} item stacks");
            }
            else
            {
                Debug.LogWarning("[Debug] 'Default' collection NOT FOUND!");
            }
        }
        else
        {
            Debug.LogError("[Debug] NO UIS INVENTORY FOUND IN SCENE!");
        }

        Debug.Log("========================================");
    }

    private void LogWeaponState()
    {
        if (m_ShootableAction == null) return;

        Debug.Log($"[Ammo] Clip: {m_ShootableAction.ClipRemainingCount}/{m_ShootableAction.ClipSize}, " +
                  $"Reserve: {m_ShootableAction.AmmoModuleGroup?.FirstEnabledModule?.GetAmmoRemainingCount() ?? -1}");
    }
}
