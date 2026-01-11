using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Synty.SidekickCharacters.Enums;

/// <summary>
/// Runtime loader for Sidekick preset data in WebGL builds.
/// Provides the same API as the SQLite-based system but loads from ScriptableObject.
/// </summary>
public static class SidekickWebGLLoader
{
    private static SidekickPresetData s_Data;
    private static bool s_Initialized;

    /// <summary>
    /// Check if we should use WebGL loader (no SQLite available).
    /// </summary>
    public static bool ShouldUseWebGLLoader
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }
    }

    /// <summary>
    /// Initialize the loader. Call this before using any other methods.
    /// </summary>
    public static bool Initialize()
    {
        Debug.Log("[SidekickWebGLLoader] Initialize() called");

        if (s_Initialized)
        {
            Debug.Log($"[SidekickWebGLLoader] Already initialized, s_Data is {(s_Data != null ? "valid" : "NULL")}");
            return s_Data != null;
        }

        Debug.Log("[SidekickWebGLLoader] Attempting to load SidekickPresetData from Resources...");
        s_Data = SidekickPresetData.Instance;
        s_Initialized = true;

        if (s_Data == null)
        {
            Debug.LogError("[SidekickWebGLLoader] FAILED to load SidekickPresetData! " +
                "Make sure to run 'Project Klyra > Sidekick > Export Presets for WebGL' before building.");
            return false;
        }

        int headCount = s_Data.GetPresetsForGroup(PartGroup.Head)?.Count ?? 0;
        int upperCount = s_Data.GetPresetsForGroup(PartGroup.UpperBody)?.Count ?? 0;
        int lowerCount = s_Data.GetPresetsForGroup(PartGroup.LowerBody)?.Count ?? 0;

        Debug.Log($"[SidekickWebGLLoader] SUCCESS! Loaded {headCount} head presets, " +
            $"{upperCount} upper body presets, {lowerCount} lower body presets");

        if (headCount == 0 && upperCount == 0 && lowerCount == 0)
        {
            Debug.LogWarning("[SidekickWebGLLoader] WARNING: No presets found in data! Did you run the export tool?");
        }

        return true;
    }

    /// <summary>
    /// Get all presets for a part group.
    /// </summary>
    public static List<WebGLPreset> GetPresetsForGroup(PartGroup group)
    {
        if (!s_Initialized) Initialize();
        if (s_Data == null) return new List<WebGLPreset>();

        var entries = s_Data.GetPresetsForGroup(group);
        return entries.Select(e => new WebGLPreset(e)).ToList();
    }

    /// <summary>
    /// Get a preset by name and group.
    /// </summary>
    public static WebGLPreset GetPresetByName(string name, PartGroup group)
    {
        if (!s_Initialized) Initialize();
        if (s_Data == null) return null;

        var entry = s_Data.GetPresetByName(name, group);
        return entry != null ? new WebGLPreset(entry) : null;
    }

    /// <summary>
    /// Get all parts for a character part type (for face customization).
    /// </summary>
    public static List<WebGLPart> GetPartsForType(CharacterPartType partType)
    {
        if (!s_Initialized) Initialize();
        if (s_Data == null) return new List<WebGLPart>();

        var entries = s_Data.GetPartsForType(partType);
        return entries.Select(e => new WebGLPart(e)).ToList();
    }

    /// <summary>
    /// Get a part by name.
    /// </summary>
    public static WebGLPart GetPartByName(string name)
    {
        if (!s_Initialized) Initialize();
        if (s_Data == null) return null;

        var entry = s_Data.GetPartByName(name);
        return entry != null ? new WebGLPart(entry) : null;
    }

    /// <summary>
    /// Wrapper class that mimics SidekickPartPreset for WebGL.
    /// </summary>
    public class WebGLPreset
    {
        public int ID { get; private set; }
        public string Name { get; private set; }
        public PartGroup PartGroup { get; private set; }
        public int SpeciesId { get; private set; }
        public string Outfit { get; private set; }
        public List<WebGLPresetPart> Parts { get; private set; }

        public WebGLPreset(SidekickPresetData.PresetEntry entry)
        {
            ID = entry.id;
            Name = entry.name;
            PartGroup = entry.partGroup;
            SpeciesId = entry.speciesId;
            Outfit = entry.outfit;
            Parts = entry.parts?.Select(p => new WebGLPresetPart(p)).ToList() ?? new List<WebGLPresetPart>();
        }
    }

    /// <summary>
    /// Wrapper class that mimics SidekickPartPresetRow for WebGL.
    /// </summary>
    public class WebGLPresetPart
    {
        public string PartName { get; private set; }
        public string PartType { get; private set; }
        public string FilePath { get; private set; }

        public WebGLPresetPart(SidekickPresetData.PresetPartRow row)
        {
            PartName = row.partName;
            PartType = row.partType;
            FilePath = row.filePath;
        }

        /// <summary>
        /// Load the mesh for this part from Resources.
        /// </summary>
        public GameObject GetPartModel()
        {
            if (string.IsNullOrEmpty(FilePath))
            {
                Debug.LogWarning($"[WebGLPresetPart] No file path for part: {PartName}");
                return null;
            }

            Debug.Log($"[WebGLPresetPart] Loading part '{PartName}' from path: {FilePath}");

            // Normalize path separators to forward slashes
            string resourcePath = FilePath.Replace('\\', '/');

            // Remove "Assets/" prefix if present
            if (resourcePath.StartsWith("Assets/"))
            {
                resourcePath = resourcePath.Substring(7);
            }

            // Find "Resources/" in the path and get everything after it
            int resourcesIndex = resourcePath.IndexOf("Resources/");
            if (resourcesIndex >= 0)
            {
                resourcePath = resourcePath.Substring(resourcesIndex + 10);
            }

            // Remove file extension if present
            if (resourcePath.EndsWith(".prefab"))
            {
                resourcePath = resourcePath.Substring(0, resourcePath.Length - 7);
            }
            else if (resourcePath.EndsWith(".fbx"))
            {
                resourcePath = resourcePath.Substring(0, resourcePath.Length - 4);
            }

            // Handle any remaining double slashes or nested references
            resourcePath = resourcePath.Replace("//", "/");

            Debug.Log($"[WebGLPresetPart] Converted to Resources path: {resourcePath}");

            GameObject model = Resources.Load<GameObject>(resourcePath);
            if (model == null)
            {
                Debug.LogError($"[WebGLPresetPart] FAILED to load part '{PartName}' from: {resourcePath} (original: {FilePath})");
            }
            else
            {
                Debug.Log($"[WebGLPresetPart] SUCCESS loaded part '{PartName}'");
            }
            return model;
        }
    }

    /// <summary>
    /// Wrapper class that mimics SidekickPart for WebGL.
    /// </summary>
    public class WebGLPart
    {
        public int ID { get; private set; }
        public string Name { get; private set; }
        public string PartType { get; private set; }
        public int SpeciesId { get; private set; }
        public string FilePath { get; private set; }

        public WebGLPart(SidekickPresetData.PartEntry entry)
        {
            ID = entry.id;
            Name = entry.name;
            PartType = entry.partType;
            SpeciesId = entry.speciesId;
            FilePath = entry.filePath;
        }
    }
}
