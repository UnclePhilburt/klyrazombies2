#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;

/// <summary>
/// Creates a ZombieLootContainer prefab with Inventory component
/// </summary>
public class ZombieLootableSetup : EditorWindow
{
    [MenuItem("Project Klyra/Zombies/Create Loot Container Prefab")]
    public static void CreateZombieLootContainerPrefab()
    {
        // Create the prefab directory if it doesn't exist
        string dir = "Assets/Resources/Prefabs";
        if (!System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
        }

        string prefabPath = $"{dir}/ZombieLootContainer.prefab";

        // Check if already exists
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            if (!EditorUtility.DisplayDialog("Prefab Exists",
                "ZombieLootContainer prefab already exists. Overwrite?", "Yes", "No"))
            {
                return;
            }
            AssetDatabase.DeleteAsset(prefabPath);
        }

        // Create the GameObject
        GameObject container = new GameObject("ZombieLootContainer");

        // Add Inventory component - it will auto-create a default ItemCollection at runtime
        container.AddComponent<Inventory>();

        // Add LootableContainer
        var lootable = container.AddComponent<LootableContainer>();
        lootable.populateOnStart = false;
        lootable.debugLog = true;

        // Save as prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(container, prefabPath);
        DestroyImmediate(container);

        if (prefab != null)
        {
            EditorUtility.DisplayDialog("Success",
                $"Created ZombieLootContainer prefab at:\n{prefabPath}\n\n" +
                "The Inventory will auto-create a Main ItemCollection at runtime.",
                "OK");
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "Failed to create prefab.", "OK");
        }

        AssetDatabase.Refresh();
    }
}
#endif
