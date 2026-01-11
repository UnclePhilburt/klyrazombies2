using UnityEngine;
using System;
using System.Collections.Generic;
using Synty.SidekickCharacters.Enums;

/// <summary>
/// ScriptableObject that stores Sidekick preset data for WebGL builds.
/// This is exported from SQLite at edit-time and loaded at runtime.
/// </summary>
[CreateAssetMenu(fileName = "SidekickPresetData", menuName = "Game/Sidekick Preset Data")]
public class SidekickPresetData : ScriptableObject
{
    private static SidekickPresetData s_Instance;
    public static SidekickPresetData Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = Resources.Load<SidekickPresetData>("SidekickPresetData");
                if (s_Instance == null)
                {
                    Debug.LogError("[SidekickPresetData] Failed to load SidekickPresetData from Resources!");
                }
            }
            return s_Instance;
        }
    }

    [Serializable]
    public class PresetPartRow
    {
        public string partName;
        public string partType; // CharacterPartType as string for serialization
        public string filePath; // Path to load the mesh from
    }

    [Serializable]
    public class PresetEntry
    {
        public int id;
        public string name;
        public PartGroup partGroup;
        public int speciesId;
        public string outfit;
        public List<PresetPartRow> parts = new List<PresetPartRow>();
    }

    [Serializable]
    public class PartEntry
    {
        public int id;
        public string name;
        public string partType;
        public int speciesId;
        public string filePath;
    }

    [Serializable]
    public class ColorPropertyEntry
    {
        public int id;
        public string name;
        public int colorGroup;
        public int u; // X coordinate in color map texture
        public int v; // Y coordinate in color map texture
    }

    [Header("Exported Preset Data")]
    [SerializeField] private List<PresetEntry> m_Presets = new List<PresetEntry>();

    [Header("Exported Part Data (for individual customization)")]
    [SerializeField] private List<PartEntry> m_Parts = new List<PartEntry>();

    [Header("Exported Color Properties")]
    [SerializeField] private List<ColorPropertyEntry> m_ColorProperties = new List<ColorPropertyEntry>();

    [Header("Export Info")]
    [SerializeField] private string m_ExportDate;
    [SerializeField] private int m_PresetCount;
    [SerializeField] private int m_PartCount;

    // Runtime lookup caches
    private Dictionary<PartGroup, List<PresetEntry>> m_PresetsByGroup;
    private Dictionary<CharacterPartType, List<PartEntry>> m_PartsByType;

    /// <summary>
    /// Get all presets for a specific part group.
    /// </summary>
    public List<PresetEntry> GetPresetsForGroup(PartGroup group)
    {
        BuildCacheIfNeeded();

        if (m_PresetsByGroup.TryGetValue(group, out var presets))
        {
            return presets;
        }
        return new List<PresetEntry>();
    }

    /// <summary>
    /// Get a preset by name and group.
    /// </summary>
    public PresetEntry GetPresetByName(string name, PartGroup group)
    {
        var presets = GetPresetsForGroup(group);
        return presets.Find(p => p.name == name);
    }

    /// <summary>
    /// Get all parts for a specific character part type.
    /// </summary>
    public List<PartEntry> GetPartsForType(CharacterPartType partType)
    {
        BuildCacheIfNeeded();

        string typeString = partType.ToString();
        if (m_PartsByType.TryGetValue(partType, out var parts))
        {
            return parts;
        }
        return new List<PartEntry>();
    }

    /// <summary>
    /// Get a part by name.
    /// </summary>
    public PartEntry GetPartByName(string name)
    {
        return m_Parts.Find(p => p.name == name);
    }

    /// <summary>
    /// Get all color properties.
    /// </summary>
    public List<ColorPropertyEntry> GetAllColorProperties()
    {
        return m_ColorProperties;
    }

    /// <summary>
    /// Get a color property by name (partial match).
    /// </summary>
    public ColorPropertyEntry GetColorPropertyByName(string name)
    {
        return m_ColorProperties.Find(p => p.name.Contains(name));
    }

    private void BuildCacheIfNeeded()
    {
        if (m_PresetsByGroup == null)
        {
            m_PresetsByGroup = new Dictionary<PartGroup, List<PresetEntry>>();
            foreach (PartGroup group in Enum.GetValues(typeof(PartGroup)))
            {
                m_PresetsByGroup[group] = new List<PresetEntry>();
            }

            foreach (var preset in m_Presets)
            {
                if (m_PresetsByGroup.ContainsKey(preset.partGroup))
                {
                    m_PresetsByGroup[preset.partGroup].Add(preset);
                }
            }
        }

        if (m_PartsByType == null)
        {
            m_PartsByType = new Dictionary<CharacterPartType, List<PartEntry>>();
            foreach (CharacterPartType partType in Enum.GetValues(typeof(CharacterPartType)))
            {
                m_PartsByType[partType] = new List<PartEntry>();
            }

            foreach (var part in m_Parts)
            {
                if (Enum.TryParse<CharacterPartType>(part.partType, out var partType))
                {
                    if (m_PartsByType.ContainsKey(partType))
                    {
                        m_PartsByType[partType].Add(part);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Clear runtime caches (call after modifying data).
    /// </summary>
    public void ClearCache()
    {
        m_PresetsByGroup = null;
        m_PartsByType = null;
    }

    // Editor methods for populating data
#if UNITY_EDITOR
    public void SetPresets(List<PresetEntry> presets)
    {
        m_Presets = presets;
        m_PresetCount = presets.Count;
        ClearCache();
    }

    public void SetParts(List<PartEntry> parts)
    {
        m_Parts = parts;
        m_PartCount = parts.Count;
        ClearCache();
    }

    public void SetColorProperties(List<ColorPropertyEntry> colorProperties)
    {
        m_ColorProperties = colorProperties;
    }

    public void SetExportDate(string date)
    {
        m_ExportDate = date;
    }

    public List<PresetEntry> GetAllPresets() => m_Presets;
    public List<PartEntry> GetAllParts() => m_Parts;
#endif
}
