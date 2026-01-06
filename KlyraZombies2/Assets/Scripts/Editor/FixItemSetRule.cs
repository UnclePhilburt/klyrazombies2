using UnityEngine;
using UnityEditor;
using Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem;
using Opsive.UltimateInventorySystem.Core;

public class FixItemSetRule : EditorWindow
{
    private ItemCategory m_RangedWeaponCategory;

    [MenuItem("Tools/Fix Item Set Rule")]
    public static void ShowWindow()
    {
        GetWindow<FixItemSetRule>("Fix Item Set Rule");
    }

    private void OnEnable()
    {
        // Try to find the RangedWeapon category
        string[] guids = AssetDatabase.FindAssets("RangedWeapon t:ItemCategory", new[] { "Assets/Data" });
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            m_RangedWeaponCategory = AssetDatabase.LoadAssetAtPath<ItemCategory>(path);
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Fix Item Set Rule", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        m_RangedWeaponCategory = (ItemCategory)EditorGUILayout.ObjectField(
            "RangedWeapon Category", m_RangedWeaponCategory, typeof(ItemCategory), false);

        EditorGUILayout.Space();

        if (m_RangedWeaponCategory == null)
        {
            EditorGUILayout.HelpBox("Please assign your RangedWeapon category", MessageType.Warning);
            return;
        }

        EditorGUILayout.HelpBox(
            "This will create a properly initialized Item Set Rule for your RangedWeapon category.",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Create Item Set Rule", GUILayout.Height(40)))
        {
            CreateItemSetRule();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "After creating, assign this rule to your character's Inventory Item Set Manager:\n" +
            "1. Select your character\n" +
            "2. Find Inventory Item Set Manager\n" +
            "3. In Item Set Groups > RangedWeapon Group > Item Set Rules\n" +
            "4. Remove the old rule and add the new one",
            MessageType.Info);
    }

    private void CreateItemSetRule()
    {
        string folder = "Assets/Data/InventoryDatabase/InventoryDatabase/ItemSetRules";

        // Ensure folder exists
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets/Data/InventoryDatabase/InventoryDatabase", "ItemSetRules");
        }

        // Create the rule using ScriptableObject.CreateInstance
        var rule = ScriptableObject.CreateInstance<ItemCategoryItemSetRule>();

        // Use reflection to set the internal fields properly
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        // Set ItemCategorySlots using the public property (which accepts ItemCategory[])
        rule.ItemCategorySlots = new ItemCategory[] { m_RangedWeaponCategory };

        // Set AllowEmptyItemSet
        var allowEmptyField = typeof(ItemCategoryItemSetRule).BaseType?.GetField("m_AllowEmptyItemSet", flags);
        if (allowEmptyField != null)
        {
            allowEmptyField.SetValue(rule, true);
        }

        // Set DoNotShareItemBetweenSet
        var doNotShareField = typeof(ItemCategoryItemSetRule).BaseType?.GetField("m_DoNotShareItemBetweenSet", flags);
        if (doNotShareField != null)
        {
            doNotShareField.SetValue(rule, true);
        }

        string path = $"{folder}/FixedRangedWeaponItemSetRule.asset";
        AssetDatabase.CreateAsset(rule, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.SetDirty(rule);

        // Select the created asset
        Selection.activeObject = rule;
        EditorGUIUtility.PingObject(rule);

        Debug.Log($"Created Item Set Rule at: {path}");
        EditorUtility.DisplayDialog("Success",
            $"Created FixedRangedWeaponItemSetRule at:\n{path}\n\n" +
            "Now assign it to your Inventory Item Set Manager.", "OK");
    }
}
