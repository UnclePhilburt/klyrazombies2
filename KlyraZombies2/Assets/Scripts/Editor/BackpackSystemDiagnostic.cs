using UnityEngine;
using UnityEditor;
using System.Linq;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;

/// <summary>
/// Editor tool to diagnose and fix backpack system issues.
/// - Removes duplicate BackpackAttachmentHandler component
/// - Checks if backpack detection is working
/// - Verifies inventory collections
/// </summary>
public class BackpackSystemDiagnostic : EditorWindow
{
    private Vector2 m_ScrollPos;

    [MenuItem("Project Klyra/Diagnostics/Backpack System")]
    public static void ShowWindow()
    {
        GetWindow<BackpackSystemDiagnostic>("Backpack Diagnostic");
    }

    private void OnGUI()
    {
        m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

        GUILayout.Label("Backpack System Diagnostic", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "This tool helps diagnose backpack system issues:\n" +
            "- Duplicate backpacks spawning\n" +
            "- Rifle holster not moving\n" +
            "- Backpack not detected",
            MessageType.Info);

        GUILayout.Space(10);

        // === CLEANUP SECTION ===
        GUILayout.Label("Cleanup", EditorStyles.boldLabel);

        if (GUILayout.Button("Remove Duplicate BackpackAttachmentHandler from Selection"))
        {
            RemoveDuplicateBackpackHandlerFromSelection();
        }

        if (GUILayout.Button("Remove BackpackAttachmentHandler from All Prefabs"))
        {
            RemoveDuplicateBackpackHandlerFromAllPrefabs();
        }

        GUILayout.Space(20);

        // === DIAGNOSTIC SECTION ===
        GUILayout.Label("Diagnostics (Play Mode Only)", EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(!Application.isPlaying);

        if (GUILayout.Button("List All Inventories in Scene"))
        {
            ListAllInventories();
        }

        if (GUILayout.Button("Check Player Inventory Collections"))
        {
            CheckPlayerInventoryCollections();
        }

        if (GUILayout.Button("Find RifleHolsterManager Status"))
        {
            FindRifleHolsterManagerStatus();
        }

        if (GUILayout.Button("Find All Backpack Handlers"))
        {
            FindAllBackpackHandlers();
        }

        EditorGUI.EndDisabledGroup();

        GUILayout.Space(20);

        // === INFO SECTION ===
        GUILayout.Label("Component Info", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "The backpack system uses these components:\n\n" +
            "BackpackEquipHandler (KEEP)\n" +
            "- Uses events, efficient\n" +
            "- Spawns visual backpack at attach point\n" +
            "- Monitors 'Equippable' collection\n\n" +
            "BackpackAttachmentHandler (REMOVE - DUPLICATE)\n" +
            "- Polls every Update(), inefficient\n" +
            "- Also spawns backpack, causing duplicates\n\n" +
            "RifleHolsterManager\n" +
            "- Moves rifle holster based on backpack size\n" +
            "- Needs to find Inventory via GetComponentInParent",
            MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    private void RemoveDuplicateBackpackHandlerFromSelection()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("No GameObject selected!");
            return;
        }

        var handler = Selection.activeGameObject.GetComponent<BackpackAttachmentHandler>();
        if (handler != null)
        {
            Undo.DestroyObjectImmediate(handler);
            Debug.Log($"Removed BackpackAttachmentHandler from {Selection.activeGameObject.name}");
        }
        else
        {
            Debug.Log($"No BackpackAttachmentHandler found on {Selection.activeGameObject.name}");
        }
    }

    private void RemoveDuplicateBackpackHandlerFromAllPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
        int removedCount = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null) continue;

            // Check if this prefab has both handlers
            var equipHandler = prefab.GetComponent<BackpackEquipHandler>();
            var attachHandler = prefab.GetComponent<BackpackAttachmentHandler>();

            if (equipHandler != null && attachHandler != null)
            {
                // Has both - remove the attachment handler
                GameObject prefabContents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var toRemove = prefabContents.GetComponent<BackpackAttachmentHandler>();
                    if (toRemove != null)
                    {
                        DestroyImmediate(toRemove);
                        PrefabUtility.SaveAsPrefabAsset(prefabContents, path);
                        removedCount++;
                        Debug.Log($"Removed BackpackAttachmentHandler from: {path}");
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabContents);
                }
            }
        }

        Debug.Log($"Removed BackpackAttachmentHandler from {removedCount} prefabs");

        if (removedCount > 0)
        {
            AssetDatabase.Refresh();
        }
    }

    private void ListAllInventories()
    {
        var inventories = FindObjectsByType<Inventory>(FindObjectsSortMode.None);
        Debug.Log($"=== Found {inventories.Length} Inventory components ===");

        foreach (var inv in inventories)
        {
            string path = GetHierarchyPath(inv.transform);
            Debug.Log($"Inventory on: {path}", inv.gameObject);

            if (inv.ItemCollectionsReadOnly != null)
            {
                foreach (var col in inv.ItemCollectionsReadOnly)
                {
                    var stacks = col.GetAllItemStacks();
                    int count = stacks?.Count ?? 0;
                    Debug.Log($"  Collection '{col.Name}': {count} items");
                }
            }
        }
    }

    private void CheckPlayerInventoryCollections()
    {
        // Find player inventory
        var inventories = FindObjectsByType<Inventory>(FindObjectsSortMode.None);
        Inventory playerInv = null;

        foreach (var inv in inventories)
        {
            if (inv.gameObject.name.Contains("Player") || inv.gameObject.name.Contains("Sidekick"))
            {
                playerInv = inv;
                break;
            }
        }

        if (playerInv == null)
        {
            Debug.LogWarning("Could not find player inventory!");
            return;
        }

        Debug.Log($"=== Player Inventory: {playerInv.gameObject.name} ===");

        if (playerInv.ItemCollectionsReadOnly != null)
        {
            foreach (var col in playerInv.ItemCollectionsReadOnly)
            {
                var stacks = col.GetAllItemStacks();
                Debug.Log($"Collection '{col.Name}':");

                if (stacks != null)
                {
                    foreach (var stack in stacks)
                    {
                        if (stack?.Item != null)
                        {
                            string catName = stack.Item.Category?.name ?? "NoCategory";

                            // Get parent categories
                            string parentCats = "NoParent";
                            if (stack.Item.Category != null)
                            {
                                var parents = stack.Item.Category.GetDirectParents();
                                if (parents != null && parents.Count > 0)
                                {
                                    parentCats = string.Join(", ", System.Linq.Enumerable.Select(parents, p => p.name));
                                }
                            }

                            bool isBackpack = catName.Contains("Backpack") || parentCats.Contains("Backpack") ||
                                              stack.Item.name.ToLower().Contains("backpack");

                            string marker = isBackpack ? "[BACKPACK] " : "";
                            Debug.Log($"  {marker}'{stack.Item.name}' x{stack.Amount} (Cat: {catName}, Parents: {parentCats})");
                        }
                    }
                }
            }
        }
    }

    private void FindRifleHolsterManagerStatus()
    {
        var managers = FindObjectsByType<RifleHolsterManager>(FindObjectsSortMode.None);
        Debug.Log($"=== Found {managers.Length} RifleHolsterManager components ===");

        foreach (var mgr in managers)
        {
            string path = GetHierarchyPath(mgr.transform);
            Debug.Log($"RifleHolsterManager on: {path}", mgr.gameObject);

            // Check if it can find inventory
            var inv = mgr.GetComponentInParent<Inventory>();
            if (inv != null)
            {
                Debug.Log($"  Found Inventory: {inv.gameObject.name}");
            }
            else
            {
                inv = mgr.GetComponent<Inventory>();
                if (inv != null)
                {
                    Debug.Log($"  Found Inventory on same object: {inv.gameObject.name}");
                }
                else
                {
                    Debug.LogWarning($"  CANNOT FIND INVENTORY! This is the problem.");
                }
            }

            // Use reflection to read private fields
            var type = typeof(RifleHolsterManager);
            var currentBackpackField = type.GetField("m_CurrentBackpack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var initializedField = type.GetField("m_Initialized", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (currentBackpackField != null)
            {
                var value = currentBackpackField.GetValue(mgr);
                Debug.Log($"  Current Backpack: {value}");
            }

            if (initializedField != null)
            {
                var value = initializedField.GetValue(mgr);
                Debug.Log($"  Initialized: {value}");
            }
        }
    }

    private void FindAllBackpackHandlers()
    {
        var equipHandlers = FindObjectsByType<BackpackEquipHandler>(FindObjectsSortMode.None);
        var attachHandlers = FindObjectsByType<BackpackAttachmentHandler>(FindObjectsSortMode.None);

        Debug.Log($"=== BackpackEquipHandler: {equipHandlers.Length} instances ===");
        foreach (var h in equipHandlers)
        {
            Debug.Log($"  {GetHierarchyPath(h.transform)}", h.gameObject);
        }

        Debug.Log($"=== BackpackAttachmentHandler: {attachHandlers.Length} instances (SHOULD BE 0) ===");
        foreach (var h in attachHandlers)
        {
            Debug.LogWarning($"  DUPLICATE: {GetHierarchyPath(h.transform)}", h.gameObject);
        }

        if (attachHandlers.Length > 0)
        {
            Debug.LogWarning("Found BackpackAttachmentHandler components! These cause duplicate backpacks. Remove them!");
        }
    }

    private string GetHierarchyPath(Transform t)
    {
        string path = t.name;
        Transform parent = t.parent;
        int depth = 0;

        while (parent != null && depth < 5)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
            depth++;
        }

        return path;
    }
}
