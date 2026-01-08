using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor tool to register all ItemCategories and ItemDefinitions into the InventoryDatabase.
/// Run this after generating new items to ensure they're properly registered.
/// </summary>
public class DatabaseRegistrationTool : EditorWindow
{
    private ScriptableObject database;
    private Vector2 scrollPosition;
    private List<ScriptableObject> unregisteredCategories = new List<ScriptableObject>();
    private List<ScriptableObject> unregisteredItems = new List<ScriptableObject>();
    private bool scanned = false;

    [MenuItem("Tools/UIS Database Registration")]
    public static void ShowWindow()
    {
        GetWindow<DatabaseRegistrationTool>("Database Registration");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("UIS Database Registration Tool", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This tool scans for ItemCategories and ItemDefinitions that aren't registered " +
            "in the InventoryDatabase and allows you to register them.",
            MessageType.Info);

        EditorGUILayout.Space(10);

        // Database reference
        database = (ScriptableObject)EditorGUILayout.ObjectField(
            "Inventory Database",
            database,
            typeof(ScriptableObject),
            false);

        if (database == null)
        {
            // Try to find it automatically
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject InventoryDatabase",
                new[] { "Assets/Data/InventoryDatabase" });
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                database = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            }
        }

        if (database == null)
        {
            EditorGUILayout.HelpBox("Please assign the InventoryDatabase asset.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Scan for Unregistered Assets", GUILayout.Height(30)))
        {
            ScanForUnregistered();
        }

        if (!scanned)
            return;

        EditorGUILayout.Space(10);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Show unregistered categories
        EditorGUILayout.LabelField($"Unregistered Categories: {unregisteredCategories.Count}", EditorStyles.boldLabel);
        if (unregisteredCategories.Count > 0)
        {
            EditorGUI.indentLevel++;
            foreach (var cat in unregisteredCategories)
            {
                EditorGUILayout.LabelField($"- {cat.name}");
            }
            EditorGUI.indentLevel--;

            if (GUILayout.Button($"Register All {unregisteredCategories.Count} Categories"))
            {
                RegisterCategories();
            }
        }
        else
        {
            EditorGUILayout.LabelField("  All categories are registered!", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space(10);

        // Show unregistered items
        EditorGUILayout.LabelField($"Unregistered Items: {unregisteredItems.Count}", EditorStyles.boldLabel);
        if (unregisteredItems.Count > 0)
        {
            EditorGUI.indentLevel++;
            int shown = 0;
            foreach (var item in unregisteredItems)
            {
                EditorGUILayout.LabelField($"- {item.name}");
                shown++;
                if (shown >= 50)
                {
                    EditorGUILayout.LabelField($"  ... and {unregisteredItems.Count - 50} more");
                    break;
                }
            }
            EditorGUI.indentLevel--;

            if (GUILayout.Button($"Register All {unregisteredItems.Count} Items"))
            {
                RegisterItems();
            }
        }
        else
        {
            EditorGUILayout.LabelField("  All items are registered!", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);

        if (unregisteredCategories.Count > 0 || unregisteredItems.Count > 0)
        {
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Register Everything", GUILayout.Height(40)))
            {
                RegisterCategories();
                RegisterItems();
                ScanForUnregistered();
            }
            GUI.backgroundColor = Color.white;
        }
    }

    private void ScanForUnregistered()
    {
        unregisteredCategories.Clear();
        unregisteredItems.Clear();

        var serializedDb = new SerializedObject(database);

        // Get currently registered items
        var categoriesProp = serializedDb.FindProperty("m_ItemCategories");
        var itemsProp = serializedDb.FindProperty("m_ItemDefinitions");

        HashSet<string> registeredCategoryGuids = new HashSet<string>();
        HashSet<string> registeredItemGuids = new HashSet<string>();

        // Collect registered category GUIDs
        if (categoriesProp != null)
        {
            for (int i = 0; i < categoriesProp.arraySize; i++)
            {
                var element = categoriesProp.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue != null)
                {
                    string path = AssetDatabase.GetAssetPath(element.objectReferenceValue);
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    registeredCategoryGuids.Add(guid);
                }
            }
        }

        // Collect registered item GUIDs
        if (itemsProp != null)
        {
            for (int i = 0; i < itemsProp.arraySize; i++)
            {
                var element = itemsProp.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue != null)
                {
                    string path = AssetDatabase.GetAssetPath(element.objectReferenceValue);
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    registeredItemGuids.Add(guid);
                }
            }
        }

        // Scan for all categories
        string[] categoryGuids = AssetDatabase.FindAssets("t:ScriptableObject",
            new[] { "Assets/Data/InventoryDatabase/InventoryDatabase/ItemCategories" });

        foreach (string guid in categoryGuids)
        {
            if (!registeredCategoryGuids.Contains(guid))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset != null && IsItemCategory(asset))
                {
                    unregisteredCategories.Add(asset);
                }
            }
        }

        // Scan for all items
        string[] itemGuids = AssetDatabase.FindAssets("t:ScriptableObject",
            new[] { "Assets/Data/InventoryDatabase/InventoryDatabase/ItemDefinitions" });

        foreach (string guid in itemGuids)
        {
            if (!registeredItemGuids.Contains(guid))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path)) continue;

                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset != null && IsItemDefinition(asset))
                {
                    unregisteredItems.Add(asset);
                }
            }
        }

        // Sort alphabetically
        unregisteredCategories = unregisteredCategories.OrderBy(x => x.name).ToList();
        unregisteredItems = unregisteredItems.OrderBy(x => x.name).ToList();

        scanned = true;

        Debug.Log($"[DatabaseRegistration] Found {unregisteredCategories.Count} unregistered categories and {unregisteredItems.Count} unregistered items.");
    }

    private bool IsItemCategory(ScriptableObject asset)
    {
        // Check if it has m_IsMutable property (ItemCategory indicator)
        var serialized = new SerializedObject(asset);
        return serialized.FindProperty("m_IsMutable") != null;
    }

    private bool IsItemDefinition(ScriptableObject asset)
    {
        // Check if it has m_Category property (ItemDefinition indicator)
        var serialized = new SerializedObject(asset);
        return serialized.FindProperty("m_Category") != null;
    }

    private void RegisterCategories()
    {
        if (unregisteredCategories.Count == 0) return;

        var serializedDb = new SerializedObject(database);
        var categoriesProp = serializedDb.FindProperty("m_ItemCategories");

        Undo.RecordObject(database, "Register Categories");

        foreach (var category in unregisteredCategories)
        {
            int index = categoriesProp.arraySize;
            categoriesProp.InsertArrayElementAtIndex(index);
            categoriesProp.GetArrayElementAtIndex(index).objectReferenceValue = category;
        }

        serializedDb.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        Debug.Log($"[DatabaseRegistration] Registered {unregisteredCategories.Count} categories.");
        unregisteredCategories.Clear();
    }

    private void RegisterItems()
    {
        if (unregisteredItems.Count == 0) return;

        var serializedDb = new SerializedObject(database);
        var itemsProp = serializedDb.FindProperty("m_ItemDefinitions");

        Undo.RecordObject(database, "Register Items");

        foreach (var item in unregisteredItems)
        {
            int index = itemsProp.arraySize;
            itemsProp.InsertArrayElementAtIndex(index);
            itemsProp.GetArrayElementAtIndex(index).objectReferenceValue = item;
        }

        serializedDb.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        Debug.Log($"[DatabaseRegistration] Registered {unregisteredItems.Count} items.");
        unregisteredItems.Clear();
    }
}
