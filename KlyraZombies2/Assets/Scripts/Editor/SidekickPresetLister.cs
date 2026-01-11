using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Synty.SidekickCharacters.API;
using Synty.SidekickCharacters.Database;
using Synty.SidekickCharacters.Database.DTO;
using Synty.SidekickCharacters.Enums;

/// <summary>
/// Editor tool to list all available Sidekick presets.
/// Helps identify preset names for underwear/bare body configurations.
/// </summary>
public class SidekickPresetLister : EditorWindow
{
    private SidekickRuntime m_Runtime;
    private DatabaseManager m_DbManager;
    private Dictionary<PartGroup, List<SidekickPartPreset>> m_PresetsByGroup;

    private Vector2 m_ScrollPos;
    private bool m_ShowHead = true;
    private bool m_ShowUpperBody = true;
    private bool m_ShowLowerBody = true;
    private string m_SearchFilter = "";

    [MenuItem("Project Klyra/Sidekick/List All Presets")]
    public static void ShowWindow()
    {
        GetWindow<SidekickPresetLister>("Sidekick Presets");
    }

    private void OnEnable()
    {
        LoadPresets();
    }

    private void LoadPresets()
    {
        // Load base model
        GameObject baseModel = Resources.Load<GameObject>("Meshes/SK_BaseModel");
        if (baseModel == null)
        {
            string[] guids = AssetDatabase.FindAssets("SK_BaseModel t:GameObject");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                baseModel = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }

        if (baseModel == null)
        {
            Debug.LogError("[SidekickPresetLister] Could not find SK_BaseModel!");
            return;
        }

        // Load material
        Material baseMaterial = Resources.Load<Material>("Materials/M_BaseMaterial");
        if (baseMaterial == null)
        {
            string[] guids = AssetDatabase.FindAssets("M_BaseMaterial t:Material");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                baseMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
            }
        }

        if (baseMaterial == null)
        {
            Debug.LogError("[SidekickPresetLister] Could not find M_BaseMaterial!");
            return;
        }

        // Initialize
        m_DbManager = new DatabaseManager();
        m_Runtime = new SidekickRuntime(baseModel, baseMaterial, null, m_DbManager);
        _ = SidekickRuntime.PopulateToolData(m_Runtime);

        // Load presets
        ReloadPresets();
    }

    private void ReloadPresets()
    {
        m_PresetsByGroup = new Dictionary<PartGroup, List<SidekickPartPreset>>();

        foreach (PartGroup group in System.Enum.GetValues(typeof(PartGroup)))
        {
            var presets = SidekickPartPreset.GetAllByGroup(m_DbManager, group)
                .Where(p => p.HasAllPartsAvailable(m_DbManager))
                .ToList();

            m_PresetsByGroup[group] = presets;
        }

        Debug.Log($"[SidekickPresetLister] Loaded presets - Head: {m_PresetsByGroup[PartGroup.Head].Count}, Upper: {m_PresetsByGroup[PartGroup.UpperBody].Count}, Lower: {m_PresetsByGroup[PartGroup.LowerBody].Count}");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Sidekick Preset Browser", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (m_PresetsByGroup == null)
        {
            EditorGUILayout.HelpBox("Failed to load presets. Check console for errors.", MessageType.Error);
            if (GUILayout.Button("Retry"))
            {
                LoadPresets();
            }
            return;
        }

        // Search filter
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
        m_SearchFilter = EditorGUILayout.TextField(m_SearchFilter);
        if (GUILayout.Button("Clear", GUILayout.Width(50)))
        {
            m_SearchFilter = "";
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Quick filters
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Show All"))
        {
            m_SearchFilter = "";
        }
        if (GUILayout.Button("Show Base Body"))
        {
            m_SearchFilter = "Base Body";
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Foldouts
        m_ShowHead = EditorGUILayout.Foldout(m_ShowHead, $"Head Presets ({GetFilteredCount(PartGroup.Head)})");
        m_ShowUpperBody = EditorGUILayout.Foldout(m_ShowUpperBody, $"Upper Body Presets ({GetFilteredCount(PartGroup.UpperBody)})");
        m_ShowLowerBody = EditorGUILayout.Foldout(m_ShowLowerBody, $"Lower Body Presets ({GetFilteredCount(PartGroup.LowerBody)})");

        EditorGUILayout.Space();

        m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

        if (m_ShowHead)
        {
            DrawPresetGroup(PartGroup.Head, "HEAD PRESETS");
        }

        if (m_ShowUpperBody)
        {
            DrawPresetGroup(PartGroup.UpperBody, "UPPER BODY PRESETS");
        }

        if (m_ShowLowerBody)
        {
            DrawPresetGroup(PartGroup.LowerBody, "LOWER BODY PRESETS");
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // Copy buttons
        EditorGUILayout.LabelField("Copy Preset Names", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Copy Underwear Names"))
        {
            CopyUnderwearNames();
        }

        if (GUILayout.Button("Copy All Names"))
        {
            CopyAllNames();
        }

        EditorGUILayout.EndHorizontal();
    }

    private int GetFilteredCount(PartGroup group)
    {
        if (!m_PresetsByGroup.ContainsKey(group)) return 0;
        if (string.IsNullOrEmpty(m_SearchFilter)) return m_PresetsByGroup[group].Count;

        return m_PresetsByGroup[group].Count(p =>
            p.Name.ToLower().Contains(m_SearchFilter.ToLower()));
    }

    private void DrawPresetGroup(PartGroup group, string header)
    {
        if (!m_PresetsByGroup.ContainsKey(group)) return;

        EditorGUILayout.LabelField(header, EditorStyles.boldLabel);

        var presets = m_PresetsByGroup[group];
        int index = 0;

        foreach (var preset in presets)
        {
            // Filter
            if (!string.IsNullOrEmpty(m_SearchFilter))
            {
                if (!preset.Name.ToLower().Contains(m_SearchFilter.ToLower()))
                    continue;
            }

            EditorGUILayout.BeginHorizontal();

            // Highlight underwear/bare presets
            bool isUnderwear = preset.Name.ToLower().Contains("underwear") ||
                               preset.Name.ToLower().Contains("bare") ||
                               preset.Name.ToLower().Contains("naked");

            if (isUnderwear)
            {
                GUI.color = Color.green;
            }

            EditorGUILayout.LabelField($"{index}.", GUILayout.Width(30));
            EditorGUILayout.LabelField(preset.Name);

            if (GUILayout.Button("Copy", GUILayout.Width(50)))
            {
                EditorGUIUtility.systemCopyBuffer = preset.Name;
                Debug.Log($"Copied: {preset.Name}");
            }

            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            index++;
        }

        EditorGUILayout.Space();
    }

    private void CopyUnderwearNames()
    {
        List<string> names = new List<string>();

        foreach (var group in m_PresetsByGroup)
        {
            foreach (var preset in group.Value)
            {
                if (preset.Name.ToLower().Contains("underwear") ||
                    preset.Name.ToLower().Contains("bare") ||
                    preset.Name.ToLower().Contains("naked"))
                {
                    names.Add($"{group.Key}: {preset.Name}");
                }
            }
        }

        if (names.Count > 0)
        {
            EditorGUIUtility.systemCopyBuffer = string.Join("\n", names);
            Debug.Log($"Copied {names.Count} underwear preset names to clipboard");
        }
        else
        {
            Debug.LogWarning("No underwear/bare presets found!");
        }
    }

    private void CopyAllNames()
    {
        List<string> names = new List<string>();

        foreach (var group in m_PresetsByGroup)
        {
            names.Add($"=== {group.Key} ===");
            foreach (var preset in group.Value)
            {
                names.Add(preset.Name);
            }
            names.Add("");
        }

        EditorGUIUtility.systemCopyBuffer = string.Join("\n", names);
        Debug.Log($"Copied all preset names to clipboard");
    }
}
