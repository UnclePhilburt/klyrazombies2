using UnityEngine;
using UnityEditor;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;

/// <summary>
/// Editor tool to add InteractionHighlight to all lootable objects in the scene.
/// </summary>
public class LootableHighlightSetup : EditorWindow
{
    [MenuItem("Project Klyra/Loot/Add Highlight to All")]
    public static void AddHighlightToAllLootables()
    {
        int added = 0;
        int skipped = 0;

        // Find all objects with Inventory component (lootable containers)
        var inventories = Object.FindObjectsByType<Inventory>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var inventory in inventories)
        {
            // Skip player inventory
            if (inventory.gameObject.layer == LayerMask.NameToLayer("Player") ||
                inventory.gameObject.layer == LayerMask.NameToLayer("Character"))
            {
                skipped++;
                continue;
            }

            // Check if it already has InteractionHighlight
            if (inventory.GetComponent<InteractionHighlight>() != null ||
                inventory.GetComponentInChildren<InteractionHighlight>() != null)
            {
                skipped++;
                continue;
            }

            // Check if it has any renderers (visual mesh)
            var renderers = inventory.GetComponentsInChildren<Renderer>();
            bool hasValidRenderer = false;
            foreach (var r in renderers)
            {
                if (r is MeshRenderer || r is SkinnedMeshRenderer)
                {
                    hasValidRenderer = true;
                    break;
                }
            }

            if (!hasValidRenderer)
            {
                Debug.LogWarning($"[LootableHighlightSetup] {inventory.gameObject.name} has no mesh renderers, skipping.");
                skipped++;
                continue;
            }

            // Add InteractionHighlight
            Undo.AddComponent<InteractionHighlight>(inventory.gameObject);
            added++;
            Debug.Log($"[LootableHighlightSetup] Added InteractionHighlight to {inventory.gameObject.name}");
        }

        Debug.Log($"[LootableHighlightSetup] Done! Added: {added}, Skipped: {skipped}");
        EditorUtility.DisplayDialog("Lootable Highlight Setup",
            $"Added InteractionHighlight to {added} objects.\nSkipped {skipped} objects (already have highlight or no renderers).",
            "OK");
    }

    [MenuItem("Project Klyra/Loot/Add Highlight to Selected")]
    public static void AddHighlightToSelected()
    {
        int added = 0;

        foreach (var obj in Selection.gameObjects)
        {
            if (obj.GetComponent<InteractionHighlight>() == null)
            {
                Undo.AddComponent<InteractionHighlight>(obj);
                added++;
                Debug.Log($"[LootableHighlightSetup] Added InteractionHighlight to {obj.name}");
            }
        }

        Debug.Log($"[LootableHighlightSetup] Added InteractionHighlight to {added} selected objects.");
    }
}
