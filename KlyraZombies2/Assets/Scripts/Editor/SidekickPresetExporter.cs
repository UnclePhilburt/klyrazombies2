using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using Synty.SidekickCharacters.Database;
using Synty.SidekickCharacters.Database.DTO;
using Synty.SidekickCharacters.Enums;

/// <summary>
/// Editor tool to export Sidekick preset data from SQLite to ScriptableObject.
/// Run this before building for WebGL.
/// </summary>
public class SidekickPresetExporter : EditorWindow
{
    private bool m_ExportHeadPresets = true;
    private bool m_ExportUpperBodyPresets = true;
    private bool m_ExportLowerBodyPresets = true;
    private bool m_ExportIndividualParts = true;
    private bool m_FilterZombiePresets = true;

    private Vector2 m_ScrollPosition;
    private string m_StatusMessage = "";
    private MessageType m_StatusType = MessageType.None;

    [MenuItem("Project Klyra/Sidekick/Export Presets for WebGL")]
    public static void ShowWindow()
    {
        var window = GetWindow<SidekickPresetExporter>("Sidekick Preset Exporter");
        window.minSize = new Vector2(400, 300);
    }

    private void OnGUI()
    {
        m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Sidekick Preset Exporter", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This tool exports Sidekick preset data from SQLite to a ScriptableObject.\n" +
            "Run this before building for WebGL, as SQLite doesn't work in browser builds.",
            MessageType.Info);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Export Options", EditorStyles.boldLabel);

        m_ExportHeadPresets = EditorGUILayout.Toggle("Head Presets", m_ExportHeadPresets);
        m_ExportUpperBodyPresets = EditorGUILayout.Toggle("Upper Body Presets", m_ExportUpperBodyPresets);
        m_ExportLowerBodyPresets = EditorGUILayout.Toggle("Lower Body Presets", m_ExportLowerBodyPresets);
        m_ExportIndividualParts = EditorGUILayout.Toggle("Individual Parts (Hair, etc.)", m_ExportIndividualParts);

        EditorGUILayout.Space(5);
        m_FilterZombiePresets = EditorGUILayout.Toggle("Filter Out Zombie Presets", m_FilterZombiePresets);

        EditorGUILayout.Space(20);

        if (GUILayout.Button("Export Presets", GUILayout.Height(40)))
        {
            ExportPresets();
        }

        EditorGUILayout.Space(10);

        // Show existing data info
        var existingData = Resources.Load<SidekickPresetData>("SidekickPresetData");
        if (existingData != null)
        {
            EditorGUILayout.LabelField("Existing Export", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Presets: {existingData.GetAllPresets()?.Count ?? 0}");
            EditorGUILayout.LabelField($"Parts: {existingData.GetAllParts()?.Count ?? 0}");
        }
        else
        {
            EditorGUILayout.HelpBox("No existing export found. Run export to create one.", MessageType.Warning);
        }

        if (!string.IsNullOrEmpty(m_StatusMessage))
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(m_StatusMessage, m_StatusType);
        }

        EditorGUILayout.EndScrollView();
    }

    private void ExportPresets()
    {
        try
        {
            m_StatusMessage = "Exporting...";
            m_StatusType = MessageType.Info;
            Repaint();

            // Connect to Sidekick database
            var dbManager = new DatabaseManager();
            dbManager.GetDbConnection();

            var presetEntries = new List<SidekickPresetData.PresetEntry>();
            var partEntries = new List<SidekickPresetData.PartEntry>();

            // Keywords to filter out zombie presets
            string[] zombieKeywords = { "zombie", "undead", "infected", "ghoul", "corpse", "rotten" };

            // Export presets by group
            if (m_ExportHeadPresets)
            {
                ExportPresetsForGroup(dbManager, PartGroup.Head, presetEntries, zombieKeywords);
            }
            if (m_ExportUpperBodyPresets)
            {
                ExportPresetsForGroup(dbManager, PartGroup.UpperBody, presetEntries, zombieKeywords);
            }
            if (m_ExportLowerBodyPresets)
            {
                ExportPresetsForGroup(dbManager, PartGroup.LowerBody, presetEntries, zombieKeywords);
            }

            // Export individual parts for face customization
            if (m_ExportIndividualParts)
            {
                ExportIndividualParts(dbManager, partEntries);
            }

            // Export color properties (needed for WebGL color system)
            var colorPropertyEntries = new List<SidekickPresetData.ColorPropertyEntry>();
            ExportColorProperties(dbManager, colorPropertyEntries);

            // Close database connection
            dbManager.CloseConnection();

            // Create or update ScriptableObject
            string resourcesPath = "Assets/Resources";
            string assetPath = $"{resourcesPath}/SidekickPresetData.asset";

            // Ensure Resources folder exists
            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            var presetData = AssetDatabase.LoadAssetAtPath<SidekickPresetData>(assetPath);
            if (presetData == null)
            {
                presetData = ScriptableObject.CreateInstance<SidekickPresetData>();
                AssetDatabase.CreateAsset(presetData, assetPath);
            }

            presetData.SetPresets(presetEntries);
            presetData.SetParts(partEntries);
            presetData.SetColorProperties(colorPropertyEntries);
            presetData.SetExportDate(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            EditorUtility.SetDirty(presetData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            m_StatusMessage = $"Export complete!\nPresets: {presetEntries.Count}\nParts: {partEntries.Count}\nColor Properties: {colorPropertyEntries.Count}";
            m_StatusType = MessageType.Info;

            Debug.Log($"[SidekickPresetExporter] Exported {presetEntries.Count} presets and {partEntries.Count} parts to {assetPath}");
        }
        catch (Exception e)
        {
            m_StatusMessage = $"Export failed: {e.Message}";
            m_StatusType = MessageType.Error;
            Debug.LogError($"[SidekickPresetExporter] Export failed: {e}");
        }

        Repaint();
    }

    private void ExportPresetsForGroup(DatabaseManager dbManager, PartGroup group,
        List<SidekickPresetData.PresetEntry> entries, string[] zombieKeywords)
    {
        var presets = SidekickPartPreset.GetAllByGroup(dbManager, group, excludeMissingParts: false);

        foreach (var preset in presets)
        {
            // Filter zombie presets if requested
            if (m_FilterZombiePresets)
            {
                bool isZombie = zombieKeywords.Any(kw =>
                    preset.Name.ToLower().Contains(kw) ||
                    (preset.Outfit?.ToLower().Contains(kw) ?? false));

                if (isZombie) continue;
            }

            var entry = new SidekickPresetData.PresetEntry
            {
                id = preset.ID,
                name = preset.Name,
                partGroup = preset.PartGroup,
                speciesId = preset.PtrSpecies,
                outfit = preset.Outfit,
                parts = new List<SidekickPresetData.PresetPartRow>()
            };

            // Get all parts for this preset
            var rows = SidekickPartPresetRow.GetAllByPreset(dbManager, preset);
            foreach (var row in rows)
            {
                // Skip rows with empty part names (optional attachment slots)
                if (string.IsNullOrEmpty(row.PartName))
                {
                    continue;
                }

                // Look up the file path for this part
                string filePath = "";
                try
                {
                    // Use SearchForByName to find the part
                    var part = SidekickPart.SearchForByName(dbManager, row.PartName);
                    if (part != null && !string.IsNullOrEmpty(part.Location))
                    {
                        // Use Location directly - it contains the full path to the .fbx file
                        // Don't add FileName as it creates a double reference
                        filePath = part.Location;

                        // Normalize to forward slashes for cross-platform compatibility
                        filePath = filePath.Replace('\\', '/');
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SidekickPresetExporter] Could not get file path for part {row.PartName}: {ex.Message}");
                }

                // Only add parts with valid file paths
                if (string.IsNullOrEmpty(filePath))
                {
                    Debug.LogWarning($"[SidekickPresetExporter] Skipping part '{row.PartName}' - no valid file path found");
                    continue;
                }

                entry.parts.Add(new SidekickPresetData.PresetPartRow
                {
                    partName = row.PartName,
                    partType = row.PartType,
                    filePath = filePath
                });
            }

            entries.Add(entry);
        }

        Debug.Log($"[SidekickPresetExporter] Exported {entries.Count(e => e.partGroup == group)} presets for {group}");
    }

    private void ExportIndividualParts(DatabaseManager dbManager, List<SidekickPresetData.PartEntry> entries)
    {
        // Part types we need for face customization
        CharacterPartType[] relevantTypes = {
            CharacterPartType.Hair,
            CharacterPartType.EyebrowLeft,
            CharacterPartType.EyebrowRight,
            CharacterPartType.FacialHair,
            CharacterPartType.Nose,
            CharacterPartType.EarLeft,
            CharacterPartType.EarRight,
            CharacterPartType.Teeth,
            CharacterPartType.Tongue
        };

        foreach (var partType in relevantTypes)
        {
            var parts = SidekickPart.GetAllForPartType(dbManager, partType);

            foreach (var part in parts)
            {
                // Only export parts that have valid files
                if (!part.FileExists) continue;

                // Use Location directly - it contains the full path to the .fbx file
                string filePath = "";
                if (!string.IsNullOrEmpty(part.Location))
                {
                    filePath = part.Location;
                    // Normalize to forward slashes for cross-platform compatibility
                    filePath = filePath.Replace('\\', '/');
                }

                entries.Add(new SidekickPresetData.PartEntry
                {
                    id = part.ID,
                    name = part.Name,
                    partType = partType.ToString(),
                    speciesId = part.PtrSpecies,
                    filePath = filePath
                });
            }
        }

        Debug.Log($"[SidekickPresetExporter] Exported {entries.Count} individual parts");
    }

    private void ExportColorProperties(DatabaseManager dbManager, List<SidekickPresetData.ColorPropertyEntry> entries)
    {
        // Get all color properties from the database
        var colorProperties = SidekickColorProperty.GetAll(dbManager);

        foreach (var prop in colorProperties)
        {
            entries.Add(new SidekickPresetData.ColorPropertyEntry
            {
                id = prop.ID,
                name = prop.Name,
                colorGroup = (int)prop.Group,
                u = prop.U,
                v = prop.V
            });
        }

        Debug.Log($"[SidekickPresetExporter] Exported {entries.Count} color properties");
    }
}
