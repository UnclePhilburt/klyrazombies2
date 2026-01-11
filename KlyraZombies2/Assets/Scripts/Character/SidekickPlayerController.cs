using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Synty.SidekickCharacters.API;
using Synty.SidekickCharacters.Database;
using Synty.SidekickCharacters.Database.DTO;
using Synty.SidekickCharacters.Enums;
using Synty.SidekickCharacters.Utils;

/// <summary>
/// Controls a Sidekick character that was set up with Opsive Character Manager.
/// Swaps appearance (meshes) at runtime while keeping the skeleton intact.
/// Supports individual part selection (Hair, Eyebrows, Nose, Ears, FacialHair) and colors.
///
/// IMPORTANT: Add this to a character that was created with:
/// 1. SidekickPlayerSetup editor tool
/// 2. Then built with Opsive Character Manager
/// </summary>
public class SidekickPlayerController : MonoBehaviour
{
    [Header("Sidekick Resources")]
    [Tooltip("Leave empty to load from Resources/Meshes/SK_BaseModel")]
    [SerializeField] private GameObject m_BaseModelOverride;

    [Tooltip("Leave empty to load from Resources/Materials/M_BaseMaterial")]
    [SerializeField] private Material m_BaseMaterialOverride;

    [Header("Character Settings")]
    [SerializeField] private string m_SaveKey = "PlayerCharacter";
    [SerializeField] private bool m_LoadSavedAppearance = true;

    [Header("Body Shape")]
    [Range(-100f, 100f)]
    [SerializeField] private float m_BodyType = 0f;

    [Range(0f, 100f)]
    [SerializeField] private float m_Muscles = 50f;

    [Range(-100f, 100f)]
    [SerializeField] private float m_BodySize = 0f;

    [Header("Underwear Mode (for character creation)")]
    [Tooltip("When true, upper/lower body are locked to base body presets (underwear/naked)")]
    [SerializeField] private bool m_UnderwearOnly = false;

    // The name of the base body preset we created in the database
    private const string BASE_BODY_PRESET = "Base Body";

    [Header("Preview Mode (for main menu)")]
    [Tooltip("When true, creates a new character from scratch instead of swapping meshes on existing skeleton")]
    [SerializeField] private bool m_BuildFromScratch = false;

    [Tooltip("Animator controller for preview character")]
    [SerializeField] private RuntimeAnimatorController m_AnimatorController;

    [Header("Backpack Attachment Offsets (from original prefab)")]
    [Tooltip("Local position offset for small backpack (relative to spine bone)")]
    [SerializeField] private Vector3 m_SmallBackpackOffset = new Vector3(-0.432f, 0.061f, -0.01f);
    [Tooltip("Local rotation for small backpack")]
    [SerializeField] private Vector3 m_SmallBackpackRotation = new Vector3(76.26f, -137.805f, -42.228f);

    [Tooltip("Local position offset for medium/regular backpack (relative to spine bone)")]
    [SerializeField] private Vector3 m_MediumBackpackOffset = new Vector3(-0.228f, 0.276f, 0.049f);
    [Tooltip("Local rotation for medium backpack")]
    [SerializeField] private Vector3 m_MediumBackpackRotation = new Vector3(-79.719f, -17.725f, -94.296f);

    [Tooltip("Local position offset for large backpack (relative to spine bone)")]
    [SerializeField] private Vector3 m_LargeBackpackOffset = new Vector3(-0.245f, 0.07f, 0.01f);
    [Tooltip("Local rotation for large backpack")]
    [SerializeField] private Vector3 m_LargeBackpackRotation = new Vector3(79.421f, -7.74f, 84.652f);

    [Header("Holster Attachment Offsets (from original prefab)")]
    [Tooltip("Local position offset for pistol holster (relative to pelvis/hips bone)")]
    [SerializeField] private Vector3 m_PistolHolsterOffset = new Vector3(-0.003f, -0.005f, 0.201f);
    [Tooltip("Local rotation for pistol holster")]
    [SerializeField] private Vector3 m_PistolHolsterRotation = new Vector3(2.8f, 90.54f, 165.73f);
    [Tooltip("ObjectIdentifier ID for pistol holster (must match weapon's HolsterTarget ID)")]
    [SerializeField] private uint m_PistolHolsterID = 1003;

    [Header("Rifle Holster (changes position based on backpack)")]
    [Tooltip("Default rifle holster position (no backpack)")]
    [SerializeField] private Vector3 m_RifleHolsterDefaultOffset = new Vector3(-0.35f, 0.18f, 0f);
    [SerializeField] private Vector3 m_RifleHolsterDefaultRotation = new Vector3(2.8f, 93.6f, 96.32f);

    [Tooltip("Rifle holster position with small backpack")]
    [SerializeField] private Vector3 m_RifleHolsterSmallBPOffset = new Vector3(-0.49f, 0.33f, 0f);
    [SerializeField] private Vector3 m_RifleHolsterSmallBPRotation = new Vector3(2.8f, 93.6f, 96.32f);

    [Tooltip("Rifle holster position with medium backpack")]
    [SerializeField] private Vector3 m_RifleHolsterMediumBPOffset = new Vector3(-0.33f, 0.3f, 0.25f);
    [SerializeField] private Vector3 m_RifleHolsterMediumBPRotation = new Vector3(3.1f, 92f, 182.34f);

    [Tooltip("Rifle holster position with large backpack")]
    [SerializeField] private Vector3 m_RifleHolsterLargeBPOffset = new Vector3(-0.38f, 0.27f, 0.18f);
    [SerializeField] private Vector3 m_RifleHolsterLargeBPRotation = new Vector3(2.8f, 93.6f, 178.9f);

    [Tooltip("ObjectIdentifier ID for rifle holster")]
    [SerializeField] private uint m_RifleHolsterID = 1002;

    [Header("Debug")]
    [SerializeField] private bool m_DebugLog = true; // Default to true for testing

    // Sidekick runtime
    private SidekickRuntime m_Runtime;
    private DatabaseManager m_DbManager;

    // WebGL mode flag
    private bool m_UseWebGLLoader;

    // WebGL mode resources (used when SidekickRuntime is not available)
    private GameObject m_WebGLBaseModel;
    private Material m_WebGLBaseMaterial;

    // Part libraries
    private Dictionary<CharacterPartType, Dictionary<string, SidekickPart>> m_PartLibrary;
    private Dictionary<PartGroup, List<SidekickPartPreset>> m_PresetsByGroup;

    // WebGL alternatives (used when SQLite is not available)
    private Dictionary<PartGroup, List<SidekickWebGLLoader.WebGLPreset>> m_WebGLPresetsByGroup;
    private Dictionary<CharacterPartType, List<SidekickWebGLLoader.WebGLPart>> m_WebGLPartsByType;

    // Individual parts by type (for granular customization)
    private Dictionary<CharacterPartType, List<SidekickPart>> m_PartsByType;

    // Current presets (for clothing/body)
    private SidekickPartPreset m_CurrentHeadPreset;
    private SidekickPartPreset m_CurrentUpperBodyPreset;
    private SidekickPartPreset m_CurrentLowerBodyPreset;

    // Current WebGL presets (used when m_UseWebGLLoader is true)
    private SidekickWebGLLoader.WebGLPreset m_CurrentWebGLHeadPreset;
    private SidekickWebGLLoader.WebGLPreset m_CurrentWebGLUpperBodyPreset;
    private SidekickWebGLLoader.WebGLPreset m_CurrentWebGLLowerBodyPreset;

    // Current individual parts (for face customization)
    private SidekickPart m_CurrentHair;
    private SidekickPart m_CurrentEyebrows; // Applied to both left/right
    private SidekickPart m_CurrentNose;
    private SidekickPart m_CurrentEars; // Applied to both left/right
    private SidekickPart m_CurrentFacialHair;

    // Color properties
    private List<SidekickColorProperty> m_ColorProperties;
    private Dictionary<string, SidekickColorRow> m_CurrentColors; // Property name -> color row

    // Default color names (these are common across Sidekick)
    public static readonly string COLOR_SKIN = "Skin";
    public static readonly string COLOR_HAIR = "Hair";
    public static readonly string COLOR_EYES = "Eye";
    public static readonly string COLOR_EYEBROWS = "Eyebrow";
    public static readonly string COLOR_STUBBLE = "Stubble";

    // WebGL color storage (used when SQLite SidekickColorRow isn't available)
    private Color m_WebGLSkinColor = new Color(0.87f, 0.72f, 0.53f); // Default medium skin tone
    private Color m_WebGLHairColor = new Color(0.25f, 0.15f, 0.08f); // Default dark brown
    private Color m_WebGLEyeColor = new Color(0.25f, 0.15f, 0.08f); // Default brown

    // Skeleton reference (from Opsive setup)
    private Transform m_SkeletonRoot;
    private Transform[] m_AllBones;
    private Dictionary<string, Transform> m_BoneMap;

    // Current mesh objects (we swap these)
    private List<GameObject> m_MeshObjects = new List<GameObject>();

    // Built character (for preview mode)
    private GameObject m_BuiltCharacter;

    // Events
    public event Action OnAppearanceChanged;
    public event Action<GameObject> OnCharacterBuilt; // For UI compatibility

    public bool IsInitialized => m_Runtime != null || (m_UseWebGLLoader && m_WebGLBaseModel != null);
    public GameObject SpawnedCharacter => m_BuildFromScratch ? m_BuiltCharacter : gameObject;
    public SidekickRuntime Runtime => m_Runtime;
    public DatabaseManager DbManager => m_DbManager;

    private void Start()
    {
        Initialize();

        if (m_LoadSavedAppearance)
        {
            LoadAppearance();
        }
    }

    /// <summary>
    /// Initialize the Sidekick system.
    /// </summary>
    [ContextMenu("Initialize")]
    public bool Initialize()
    {
        // Check if already initialized (works for both SQLite and WebGL modes)
        if (IsInitialized) return true;

        // Determine if we should use WebGL loader (no SQLite available in WebGL builds)
        m_UseWebGLLoader = SidekickWebGLLoader.ShouldUseWebGLLoader;

        if (m_DebugLog)
        {
            Debug.Log($"[SidekickPlayerController] Using WebGL loader: {m_UseWebGLLoader}");
        }

        // In build from scratch mode, we don't need an existing skeleton
        if (!m_BuildFromScratch)
        {
            // Find the skeleton root (Sidekick uses lowercase "root")
            m_SkeletonRoot = FindSkeletonRoot();
            if (m_SkeletonRoot == null)
            {
                Debug.LogError("[SidekickPlayerController] Could not find skeleton root! Enable 'Build From Scratch' for preview mode.");
                return false;
            }

            // Build bone map
            BuildBoneMap();

            // Find existing meshes
            FindExistingMeshes();
        }

        // Load base model
        GameObject baseModel = m_BaseModelOverride;
        if (baseModel == null)
        {
            baseModel = Resources.Load<GameObject>("Meshes/SK_BaseModel");
        }

        if (baseModel == null)
        {
            Debug.LogError("[SidekickPlayerController] Could not load SK_BaseModel!");
            return false;
        }

        // Load material
        Material baseMaterial = m_BaseMaterialOverride;
        if (baseMaterial == null)
        {
            baseMaterial = Resources.Load<Material>("Materials/M_BaseMaterial");
        }

        if (baseMaterial == null)
        {
            Debug.LogError("[SidekickPlayerController] Could not load M_BaseMaterial!");
            return false;
        }

        if (m_UseWebGLLoader)
        {
            // WebGL mode: Initialize the WebGL loader and skip SQLite entirely
            if (!SidekickWebGLLoader.Initialize())
            {
                Debug.LogError("[SidekickPlayerController] Failed to initialize WebGL loader!");
                return false;
            }

            // Don't create SidekickRuntime in WebGL - it tries to use SQLite internally
            m_DbManager = null;
            m_Runtime = null;

            // Store base model and material for manual character building
            m_WebGLBaseModel = baseModel;
            m_WebGLBaseMaterial = baseMaterial;

            // Load presets from ScriptableObject
            LoadPresetsWebGL();
        }
        else
        {
            // Standard mode: Use SQLite database
            m_DbManager = new DatabaseManager();
            m_Runtime = new SidekickRuntime(baseModel, baseMaterial, m_AnimatorController, m_DbManager);

            // Load part data
            _ = SidekickRuntime.PopulateToolData(m_Runtime);
            m_PartLibrary = m_Runtime.MappedPartDictionary;

            // Load presets
            LoadPresets();
        }

        if (m_DebugLog)
        {
            if (m_BuildFromScratch)
            {
                Debug.Log($"[SidekickPlayerController] Initialized in BUILD FROM SCRATCH mode (WebGL: {m_UseWebGLLoader})");
            }
            else
            {
                Debug.Log($"[SidekickPlayerController] Initialized with skeleton root: {m_SkeletonRoot?.name} (WebGL: {m_UseWebGLLoader})");
            }
        }

        return true;
    }

    private Transform FindSkeletonRoot()
    {
        // Look for Sidekick's root bone (lowercase)
        var allTransforms = GetComponentsInChildren<Transform>(true);
        foreach (var t in allTransforms)
        {
            if (t.name == "root")
            {
                return t;
            }
        }
        // Fallback to capitalized
        foreach (var t in allTransforms)
        {
            if (t.name == "Root")
            {
                return t;
            }
        }
        return null;
    }

    private void BuildBoneMap()
    {
        m_BoneMap = new Dictionary<string, Transform>();
        m_AllBones = m_SkeletonRoot.GetComponentsInChildren<Transform>();

        foreach (var bone in m_AllBones)
        {
            if (!m_BoneMap.ContainsKey(bone.name))
            {
                m_BoneMap[bone.name] = bone;
            }
        }

        if (m_DebugLog)
        {
            Debug.Log($"[SidekickPlayerController] Built bone map with {m_BoneMap.Count} bones");
        }
    }

    private void FindExistingMeshes()
    {
        m_MeshObjects.Clear();
        var meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var renderer in meshRenderers)
        {
            // Don't include the skeleton transforms themselves
            if (renderer.transform != m_SkeletonRoot && !IsPartOfSkeleton(renderer.transform))
            {
                m_MeshObjects.Add(renderer.gameObject);
            }
        }

        if (m_DebugLog)
        {
            Debug.Log($"[SidekickPlayerController] Found {m_MeshObjects.Count} existing mesh objects");
        }
    }

    private bool IsPartOfSkeleton(Transform t)
    {
        // Check if this transform is a bone (part of the skeleton hierarchy)
        return m_BoneMap != null && m_BoneMap.ContainsKey(t.name);
    }

    private void LoadPresets()
    {
        m_PresetsByGroup = new Dictionary<PartGroup, List<SidekickPartPreset>>();

        // Keywords that indicate zombie presets (to be filtered out)
        string[] zombieKeywords = { "zombie", "undead", "infected", "ghoul", "corpse", "rotten" };

        foreach (PartGroup group in Enum.GetValues(typeof(PartGroup)))
        {
            // Get all presets for this group
            var rawPresets = SidekickPartPreset.GetAllByGroup(m_DbManager, group).ToList();

            // NOTE: We no longer filter by HasAllPartsAvailable() because many presets have
            // optional empty attachment slots (backpacks, face accessories) that are intentionally empty.
            // The Sidekick system handles empty part names gracefully when applying presets.

            // Filter out zombie presets by name
            var humanPresets = rawPresets
                .Where(p => !IsZombiePreset(p.Name, zombieKeywords))
                .ToList();

            m_PresetsByGroup[group] = humanPresets;

            if (m_DebugLog)
            {
                int filtered = rawPresets.Count - humanPresets.Count;
                Debug.Log($"[SidekickPlayerController] Loaded {humanPresets.Count} presets for {group} (filtered out {filtered} zombie presets)");
                foreach (var preset in humanPresets.Take(5))
                {
                    Debug.Log($"  - {preset.Name}");
                }
            }
        }

        // Set default presets - prefer "Base Body" for underwear mode
        if (m_PresetsByGroup[PartGroup.Head].Count > 0)
            m_CurrentHeadPreset = m_PresetsByGroup[PartGroup.Head][0];

        if (m_PresetsByGroup[PartGroup.UpperBody].Count > 0)
        {
            // Try to find Base Body preset first
            m_CurrentUpperBodyPreset = m_PresetsByGroup[PartGroup.UpperBody]
                .FirstOrDefault(p => p.Name == BASE_BODY_PRESET)
                ?? m_PresetsByGroup[PartGroup.UpperBody][0];
        }

        if (m_PresetsByGroup[PartGroup.LowerBody].Count > 0)
        {
            // Try to find Base Body preset first
            m_CurrentLowerBodyPreset = m_PresetsByGroup[PartGroup.LowerBody]
                .FirstOrDefault(p => p.Name == BASE_BODY_PRESET)
                ?? m_PresetsByGroup[PartGroup.LowerBody][0];
        }

        // Load individual parts by type for granular customization
        LoadIndividualParts();

        // Load color properties
        LoadColorProperties();
    }

    /// <summary>
    /// Load presets from ScriptableObject for WebGL builds.
    /// </summary>
    private void LoadPresetsWebGL()
    {
        m_WebGLPresetsByGroup = new Dictionary<PartGroup, List<SidekickWebGLLoader.WebGLPreset>>();

        // Keywords that indicate zombie presets (to be filtered out)
        string[] zombieKeywords = { "zombie", "undead", "infected", "ghoul", "corpse", "rotten" };

        foreach (PartGroup group in Enum.GetValues(typeof(PartGroup)))
        {
            var rawPresets = SidekickWebGLLoader.GetPresetsForGroup(group);

            // Filter out zombie presets by name
            var humanPresets = rawPresets
                .Where(p => !IsZombiePreset(p.Name, zombieKeywords))
                .ToList();

            m_WebGLPresetsByGroup[group] = humanPresets;

            if (m_DebugLog)
            {
                int filtered = rawPresets.Count - humanPresets.Count;
                Debug.Log($"[SidekickPlayerController] WebGL: Loaded {humanPresets.Count} presets for {group} (filtered out {filtered} zombie presets)");
            }
        }

        // Set default presets for WebGL
        Debug.Log("[SidekickPlayerController] WebGL: Setting default presets...");

        if (m_WebGLPresetsByGroup[PartGroup.Head].Count > 0)
        {
            // Try to find a human head preset first (Species Humans)
            m_CurrentWebGLHeadPreset = m_WebGLPresetsByGroup[PartGroup.Head]
                .FirstOrDefault(p => p.Name.StartsWith("Species Humans"))
                ?? m_WebGLPresetsByGroup[PartGroup.Head]
                .FirstOrDefault(p => p.Name.Contains("Human"))
                ?? m_WebGLPresetsByGroup[PartGroup.Head][0];
            Debug.Log($"[SidekickPlayerController] WebGL: Default Head preset: {m_CurrentWebGLHeadPreset?.Name} with {m_CurrentWebGLHeadPreset?.Parts?.Count ?? 0} parts");
        }
        else
        {
            Debug.LogWarning("[SidekickPlayerController] WebGL: No Head presets available!");
        }

        if (m_WebGLPresetsByGroup[PartGroup.UpperBody].Count > 0)
        {
            // Try to find Base Body preset first
            m_CurrentWebGLUpperBodyPreset = m_WebGLPresetsByGroup[PartGroup.UpperBody]
                .FirstOrDefault(p => p.Name == BASE_BODY_PRESET)
                ?? m_WebGLPresetsByGroup[PartGroup.UpperBody][0];
            Debug.Log($"[SidekickPlayerController] WebGL: Default UpperBody preset: {m_CurrentWebGLUpperBodyPreset?.Name} with {m_CurrentWebGLUpperBodyPreset?.Parts?.Count ?? 0} parts");
        }
        else
        {
            Debug.LogWarning("[SidekickPlayerController] WebGL: No UpperBody presets available!");
        }

        if (m_WebGLPresetsByGroup[PartGroup.LowerBody].Count > 0)
        {
            // Try to find Base Body preset first
            m_CurrentWebGLLowerBodyPreset = m_WebGLPresetsByGroup[PartGroup.LowerBody]
                .FirstOrDefault(p => p.Name == BASE_BODY_PRESET)
                ?? m_WebGLPresetsByGroup[PartGroup.LowerBody][0];
            Debug.Log($"[SidekickPlayerController] WebGL: Default LowerBody preset: {m_CurrentWebGLLowerBodyPreset?.Name} with {m_CurrentWebGLLowerBodyPreset?.Parts?.Count ?? 0} parts");
        }
        else
        {
            Debug.LogWarning("[SidekickPlayerController] WebGL: No LowerBody presets available!");
        }

        // Load individual parts for WebGL
        LoadIndividualPartsWebGL();

        if (m_DebugLog)
        {
            Debug.Log("[SidekickPlayerController] WebGL preset loading complete");
        }
    }

    /// <summary>
    /// Load individual parts from ScriptableObject for WebGL builds.
    /// </summary>
    private void LoadIndividualPartsWebGL()
    {
        m_WebGLPartsByType = new Dictionary<CharacterPartType, List<SidekickWebGLLoader.WebGLPart>>();

        // Keywords that indicate zombie parts (to be filtered out)
        string[] zombieKeywords = { "zombie", "undead", "infected", "ghoul", "corpse", "rotten" };

        // Part types we want to expose for customization
        CharacterPartType[] customizableTypes = new[]
        {
            CharacterPartType.Hair,
            CharacterPartType.EyebrowLeft,
            CharacterPartType.EyebrowRight,
            CharacterPartType.Nose,
            CharacterPartType.EarLeft,
            CharacterPartType.EarRight,
            CharacterPartType.FacialHair,
            CharacterPartType.Head
        };

        foreach (var partType in customizableTypes)
        {
            var rawParts = SidekickWebGLLoader.GetPartsForType(partType);

            // Filter out zombie parts by name
            var filteredParts = rawParts
                .Where(p => !IsZombiePart(p.Name, zombieKeywords))
                .ToList();

            m_WebGLPartsByType[partType] = filteredParts;

            if (m_DebugLog)
            {
                int filtered = rawParts.Count - filteredParts.Count;
                Debug.Log($"[SidekickPlayerController] WebGL: Loaded {filteredParts.Count} individual parts for {partType} (filtered {filtered} zombie parts)");
            }
        }

        // Set default individual parts (first available or null)
        SetDefaultIndividualParts();
    }

    /// <summary>
    /// Check if a preset name indicates it's a zombie preset.
    /// </summary>
    private bool IsZombiePreset(string presetName, string[] zombieKeywords)
    {
        if (string.IsNullOrEmpty(presetName)) return false;
        string lowerName = presetName.ToLower();
        foreach (var keyword in zombieKeywords)
        {
            if (lowerName.Contains(keyword))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Load all individual parts organized by CharacterPartType.
    /// Filters out zombie parts by name.
    /// </summary>
    private void LoadIndividualParts()
    {
        m_PartsByType = new Dictionary<CharacterPartType, List<SidekickPart>>();

        // Keywords that indicate zombie parts (to be filtered out)
        string[] zombieKeywords = { "zombie", "undead", "infected", "ghoul", "corpse", "rotten" };

        // Part types we want to expose for customization
        CharacterPartType[] customizableTypes = new[]
        {
            CharacterPartType.Hair,
            CharacterPartType.EyebrowLeft,
            CharacterPartType.EyebrowRight,
            CharacterPartType.Nose,
            CharacterPartType.EarLeft,
            CharacterPartType.EarRight,
            CharacterPartType.FacialHair,
            CharacterPartType.Head // For base head shape
        };

        foreach (var partType in customizableTypes)
        {
            if (m_PartLibrary.TryGetValue(partType, out var partDict))
            {
                // Filter out zombie parts by name
                var parts = partDict.Values
                    .Where(p => !IsZombiePart(p.Name, zombieKeywords))
                    .ToList();

                m_PartsByType[partType] = parts;

                if (m_DebugLog)
                {
                    int filtered = partDict.Values.Count - parts.Count;
                    Debug.Log($"[SidekickPlayerController] Loaded {parts.Count} individual parts for {partType} (filtered {filtered} zombie parts)");
                }
            }
            else
            {
                m_PartsByType[partType] = new List<SidekickPart>();
            }
        }

        // Set default individual parts (first available or null)
        SetDefaultIndividualParts();
    }

    /// <summary>
    /// Check if a part name indicates it's a zombie part.
    /// </summary>
    private bool IsZombiePart(string partName, string[] zombieKeywords)
    {
        if (string.IsNullOrEmpty(partName)) return false;
        string lowerName = partName.ToLower();
        foreach (var keyword in zombieKeywords)
        {
            if (lowerName.Contains(keyword))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Set default individual parts from available parts.
    /// </summary>
    private void SetDefaultIndividualParts()
    {
        // Use GetPartsForType which handles both WebGL and SQLite modes

        // Hair - pick first available (could be bald/none)
        var hairParts = GetPartsForType(CharacterPartType.Hair);
        if (hairParts.Count > 0)
        {
            m_CurrentHair = hairParts[0];
        }

        // Eyebrows - we use EyebrowLeft and mirror to right
        var eyebrowParts = GetPartsForType(CharacterPartType.EyebrowLeft);
        if (eyebrowParts.Count > 0)
        {
            m_CurrentEyebrows = eyebrowParts[0];
        }

        // Nose
        var noseParts = GetPartsForType(CharacterPartType.Nose);
        if (noseParts.Count > 0)
        {
            m_CurrentNose = noseParts[0];
        }

        // Ears
        var earParts = GetPartsForType(CharacterPartType.EarLeft);
        if (earParts.Count > 0)
        {
            m_CurrentEars = earParts[0];
        }

        // Facial hair (can be null for none)
        var facialHairParts = GetPartsForType(CharacterPartType.FacialHair);
        if (facialHairParts.Count > 0)
        {
            // Default to no facial hair (null) - user must select
            m_CurrentFacialHair = null;
        }
    }

    /// <summary>
    /// Load color properties from the database.
    /// </summary>
    private void LoadColorProperties()
    {
        m_CurrentColors = new Dictionary<string, SidekickColorRow>();

        if (m_UseWebGLLoader)
        {
            // In WebGL mode, we don't have access to color properties from database
            // Colors will be applied directly without the property lookup
            m_ColorProperties = new List<SidekickColorProperty>();
            if (m_DebugLog)
            {
                Debug.Log("[SidekickPlayerController] WebGL mode: Color properties not loaded from database");
            }
        }
        else
        {
            m_ColorProperties = SidekickColorProperty.GetAll(m_DbManager);

            if (m_DebugLog)
            {
                Debug.Log($"[SidekickPlayerController] Loaded {m_ColorProperties.Count} color properties");
                foreach (var prop in m_ColorProperties)
                {
                    Debug.Log($"  - {prop.Name} (U:{prop.U}, V:{prop.V})");
                }
            }
        }
    }

    /// <summary>
    /// Apply current appearance to the character by swapping meshes or building from scratch.
    /// </summary>
    [ContextMenu("Apply Appearance")]
    public void ApplyAppearance()
    {
        if (m_DebugLog)
        {
            Debug.Log("[SidekickPlayerController] ApplyAppearance called!");
        }

        if (!IsInitialized)
        {
            Debug.LogError("[SidekickPlayerController] Not initialized! Call Initialize() first.");
            return;
        }

        // Collect parts from presets
        List<SkinnedMeshRenderer> newParts = new List<SkinnedMeshRenderer>();

        if (m_UseWebGLLoader)
        {
            // WebGL mode: Use WebGL presets
            Debug.Log($"[SidekickPlayerController] WebGL ApplyAppearance - Current presets:");
            Debug.Log($"  Head: {m_CurrentWebGLHeadPreset?.Name ?? "NULL"}");
            Debug.Log($"  Upper: {m_CurrentWebGLUpperBodyPreset?.Name ?? "NULL"}");
            Debug.Log($"  Lower: {m_CurrentWebGLLowerBodyPreset?.Name ?? "NULL"}");
            Debug.Log($"  Custom Hair: {m_CurrentHair?.Name ?? "NULL"}");
            Debug.Log($"  Custom FacialHair: {m_CurrentFacialHair?.Name ?? "NULL"}");
            Debug.Log($"  Custom Eyebrows: {m_CurrentEyebrows?.Name ?? "NULL"}");
            Debug.Log($"  Custom Nose: {m_CurrentNose?.Name ?? "NULL"}");
            Debug.Log($"  Custom Ears: {m_CurrentEars?.Name ?? "NULL"}");

            // Skip backpack attachments from clothing presets
            // Part type codes: 24ABCK = AttachmentBack
            HashSet<string> skipPartTypes = new HashSet<string> { "24ABCK" };

            // Also skip part types if we have custom selections for them
            // Part type codes from database: 02HAIR, 03EBRL, 04EBRR, 07EARL, 08EARR, 35NOSE, 09FCHR
            if (m_CurrentHair != null) skipPartTypes.Add("02HAIR");
            if (m_CurrentFacialHair != null) skipPartTypes.Add("09FCHR");
            if (m_CurrentEyebrows != null)
            {
                skipPartTypes.Add("03EBRL");
                skipPartTypes.Add("04EBRR");
            }
            if (m_CurrentNose != null) skipPartTypes.Add("35NOSE");
            if (m_CurrentEars != null)
            {
                skipPartTypes.Add("07EARL");
                skipPartTypes.Add("08EARR");
            }

            AddPartsFromWebGLPreset(m_CurrentWebGLHeadPreset, newParts, skipPartTypes);
            AddPartsFromWebGLPreset(m_CurrentWebGLUpperBodyPreset, newParts, skipPartTypes);
            AddPartsFromWebGLPreset(m_CurrentWebGLLowerBodyPreset, newParts, skipPartTypes);

            Debug.Log($"[SidekickPlayerController] WebGL: After adding preset parts, total parts count: {newParts.Count}");

            // Add custom individual parts (WebGL mode)
            if (m_CurrentHair != null)
            {
                Debug.Log($"[SidekickPlayerController] WebGL: Adding custom hair: {m_CurrentHair.Name}");
                AddIndividualPartWebGL(m_CurrentHair, newParts);
            }
            if (m_CurrentFacialHair != null)
            {
                Debug.Log($"[SidekickPlayerController] WebGL: Adding custom facial hair: {m_CurrentFacialHair.Name}");
                AddIndividualPartWebGL(m_CurrentFacialHair, newParts);
            }
            if (m_CurrentEyebrows != null)
            {
                Debug.Log($"[SidekickPlayerController] WebGL: Adding custom eyebrows: {m_CurrentEyebrows.Name}");
                AddIndividualPartWebGL(m_CurrentEyebrows, newParts, CharacterPartType.EyebrowLeft);
                // Mirror to right eyebrow (most eyebrow parts are just left, and we mirror)
            }
            if (m_CurrentNose != null)
            {
                Debug.Log($"[SidekickPlayerController] WebGL: Adding custom nose: {m_CurrentNose.Name}");
                AddIndividualPartWebGL(m_CurrentNose, newParts);
            }
            if (m_CurrentEars != null)
            {
                Debug.Log($"[SidekickPlayerController] WebGL: Adding custom ears: {m_CurrentEars.Name}");
                AddIndividualPartWebGL(m_CurrentEars, newParts, CharacterPartType.EarLeft);
                // Mirror to right ear
            }

            Debug.Log($"[SidekickPlayerController] WebGL: After adding individual parts, total parts count: {newParts.Count}");
        }
        else
        {
            // Standard SQLite mode
            if (m_DebugLog)
            {
                Debug.Log($"[SidekickPlayerController] Current presets - Head: {m_CurrentHeadPreset?.Name}, Upper: {m_CurrentUpperBodyPreset?.Name}, Lower: {m_CurrentLowerBodyPreset?.Name}");
            }

            // Determine which part types to skip from head preset (if we have custom selections)
            HashSet<CharacterPartType> skipHeadTypes = new HashSet<CharacterPartType>();
            if (m_CurrentHair != null) skipHeadTypes.Add(CharacterPartType.Hair);
            if (m_CurrentFacialHair != null) skipHeadTypes.Add(CharacterPartType.FacialHair);

            // Skip backpack attachments from clothing presets - we use our own backpack system
            HashSet<CharacterPartType> skipUpperBodyTypes = new HashSet<CharacterPartType>();
            skipUpperBodyTypes.Add(CharacterPartType.AttachmentBack);

            AddPartsFromPreset(m_CurrentHeadPreset, newParts, skipHeadTypes);
            AddPartsFromPreset(m_CurrentUpperBodyPreset, newParts, skipUpperBodyTypes);
            AddPartsFromPreset(m_CurrentLowerBodyPreset, newParts);

            // Add custom hair if selected (this replaces the preset's hair)
            if (m_CurrentHair != null)
            {
                Debug.Log($"[SidekickPlayerController] Adding custom hair: {m_CurrentHair.Name}");
                AddIndividualPart(m_CurrentHair, newParts);
            }
            else
            {
                Debug.Log("[SidekickPlayerController] No custom hair set (m_CurrentHair is null)");
            }

            // Add custom facial hair if selected
            if (m_CurrentFacialHair != null)
            {
                Debug.Log($"[SidekickPlayerController] Adding custom facial hair: {m_CurrentFacialHair.Name}");
                AddIndividualPart(m_CurrentFacialHair, newParts);
            }
        }

        if (m_DebugLog)
        {
            Debug.Log($"[SidekickPlayerController] Collected {newParts.Count} mesh parts (including individual face parts)");
        }

        if (newParts.Count == 0)
        {
            Debug.LogError("[SidekickPlayerController] No parts found from presets!");
            return;
        }

        if (m_UseWebGLLoader)
        {
            Debug.Log($"[SidekickPlayerController] WebGL: About to build character with {newParts.Count} parts, BuildFromScratch={m_BuildFromScratch}");

            // WebGL mode: Build character without SidekickRuntime
            if (m_BuildFromScratch)
            {
                ApplyAppearanceBuildFromScratchWebGL(newParts);
            }
            else
            {
                Debug.LogWarning("[SidekickPlayerController] WebGL mode only supports BuildFromScratch mode");
                ApplyAppearanceBuildFromScratchWebGL(newParts);
            }
        }
        else
        {
            // Standard mode with SidekickRuntime
            // Set body shape on runtime
            m_Runtime.BodyTypeBlendValue = m_BodyType;
            m_Runtime.MusclesBlendValue = m_Muscles;
            m_Runtime.BodySizeHeavyBlendValue = m_BodySize > 0 ? m_BodySize : 0;
            m_Runtime.BodySizeSkinnyBlendValue = m_BodySize < 0 ? -m_BodySize : 0;

            if (m_BuildFromScratch)
            {
                // Build from scratch mode - create a new character using Sidekick runtime
                ApplyAppearanceBuildFromScratch(newParts);
            }
            else
            {
                // Swap meshes on existing skeleton
                ApplyAppearanceSwapMeshes(newParts);
            }

            // Apply saved colors to the character
            ApplyColors();
        }

        OnAppearanceChanged?.Invoke();
        OnCharacterBuilt?.Invoke(SpawnedCharacter);
    }

    /// <summary>
    /// Build a new character from scratch using SidekickRuntime.CreateCharacter.
    /// Used for main menu preview and game spawning.
    /// </summary>
    private void ApplyAppearanceBuildFromScratch(List<SkinnedMeshRenderer> newParts)
    {
        // Destroy old character if it exists
        if (m_BuiltCharacter != null)
        {
            DestroyImmediate(m_BuiltCharacter);
            m_BuiltCharacter = null;
        }

        // Create new character using Sidekick
        m_BuiltCharacter = m_Runtime.CreateCharacter("SidekickCharacter", newParts, false, true);

        if (m_BuiltCharacter != null)
        {
            // Find the Opsive-controlled animator (the one with AnimatorMonitor)
            Animator opsiveAnimator = null;
            var oldMonitors = GetComponentsInChildren<Opsive.UltimateCharacterController.Character.AnimatorMonitor>(true);
            foreach (var oldMonitor in oldMonitors)
            {
                // IMPORTANT: Don't disable the GameObject - just hide the meshes!
                // The GameObject needs to stay active for physics (colliders, rigidbody, etc.)
                var meshRenderers = oldMonitor.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var renderer in meshRenderers)
                {
                    renderer.enabled = false;
                    Debug.Log($"[SidekickPlayerController] Hid mesh: {renderer.name}");
                }

                // Also hide any regular MeshRenderers
                var regularRenderers = oldMonitor.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var renderer in regularRenderers)
                {
                    renderer.enabled = false;
                }

                // Keep track of the Opsive animator
                var animator = oldMonitor.GetComponent<Animator>();
                if (animator != null)
                {
                    opsiveAnimator = animator;
                }

                Debug.Log($"[SidekickPlayerController] Hid old model meshes: {oldMonitor.gameObject.name} (keeping physics active)");
            }

            // Parent the new character
            m_BuiltCharacter.transform.SetParent(transform);
            m_BuiltCharacter.transform.localPosition = Vector3.zero;
            m_BuiltCharacter.transform.localRotation = Quaternion.identity;

            // Get the Sidekick character's animator
            var sidekickAnimator = m_BuiltCharacter.GetComponent<Animator>();

            if (sidekickAnimator != null && opsiveAnimator != null)
            {
                // Copy the controller from Opsive animator
                if (sidekickAnimator.runtimeAnimatorController == null)
                {
                    sidekickAnimator.runtimeAnimatorController = opsiveAnimator.runtimeAnimatorController;
                }

                // Add AnimatorSync to copy animation state from Opsive to Sidekick
                var animSync = m_BuiltCharacter.GetComponent<AnimatorSync>();
                if (animSync == null)
                {
                    animSync = m_BuiltCharacter.AddComponent<AnimatorSync>();
                }
                animSync.Initialize(opsiveAnimator, sidekickAnimator);

                // Add AnimationEventRelay to forward animation events to the main character
                var eventRelay = m_BuiltCharacter.GetComponent<AnimationEventRelay>();
                if (eventRelay == null)
                {
                    eventRelay = m_BuiltCharacter.AddComponent<AnimationEventRelay>();
                }

                Debug.Log($"[SidekickPlayerController] Set up AnimatorSync: {opsiveAnimator.gameObject.name} -> {sidekickAnimator.gameObject.name}");
            }
            else if (sidekickAnimator != null)
            {
                // Fallback: try to find any animator with a controller
                Animator sourceAnimator = GetComponent<Animator>();
                if (sourceAnimator == null)
                {
                    sourceAnimator = GetComponentInParent<Animator>();
                }

                if (sourceAnimator != null && sidekickAnimator.runtimeAnimatorController == null)
                {
                    sidekickAnimator.runtimeAnimatorController = sourceAnimator.runtimeAnimatorController;
                }

                Debug.Log($"[SidekickPlayerController] Fallback: copied controller to Sidekick animator");
            }

            // Update backpack attach points to use Sidekick skeleton
            UpdateBackpackAttachPoints();

            // Create holsters on Sidekick skeleton
            CreateHolstersOnSidekick();

            // Set up weapon item sync to handle dynamically spawned weapons
            SetupWeaponItemSync();

            if (m_DebugLog)
            {
                Debug.Log($"[SidekickPlayerController] Built character from scratch, parented to {transform.name}");
            }
        }
        else
        {
            Debug.LogError("[SidekickPlayerController] Failed to create character from scratch!");
        }
    }

    /// <summary>
    /// Build a new character from scratch for WebGL builds (without SidekickRuntime).
    /// Manually combines meshes and creates the character hierarchy.
    /// </summary>
    private void ApplyAppearanceBuildFromScratchWebGL(List<SkinnedMeshRenderer> newParts)
    {
        if (m_DebugLog)
        {
            Debug.Log($"[SidekickPlayerController] WebGL: Building character with {newParts.Count} parts");
        }

        // Destroy old character if it exists
        if (m_BuiltCharacter != null)
        {
            DestroyImmediate(m_BuiltCharacter);
            m_BuiltCharacter = null;
        }

        // Create the character root
        m_BuiltCharacter = new GameObject("SidekickCharacter_WebGL");

        // Instantiate the base model for skeleton
        if (m_WebGLBaseModel != null)
        {
            var baseInstance = Instantiate(m_WebGLBaseModel, m_BuiltCharacter.transform);
            baseInstance.name = "BaseModel";

            // Get the animator from base model
            var animator = baseInstance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = m_BuiltCharacter.AddComponent<Animator>();
            }

            // Assign animator controller
            if (m_AnimatorController != null)
            {
                animator.runtimeAnimatorController = m_AnimatorController;
            }

            // Find the skeleton root from base model
            Transform skeletonRoot = null;
            foreach (Transform child in baseInstance.GetComponentsInChildren<Transform>())
            {
                if (child.name.ToLower() == "root" || child.name.ToLower() == "hips" || child.name == "Armature")
                {
                    skeletonRoot = child;
                    break;
                }
            }

            if (skeletonRoot != null)
            {
                // Build a dictionary of bones in the target skeleton
                var targetBoneDict = new Dictionary<string, Transform>();
                foreach (var bone in baseInstance.GetComponentsInChildren<Transform>())
                {
                    if (!targetBoneDict.ContainsKey(bone.name))
                    {
                        targetBoneDict[bone.name] = bone;
                    }
                }

                if (m_DebugLog)
                {
                    Debug.Log($"[SidekickPlayerController] WebGL: Target skeleton has {targetBoneDict.Count} bones");
                }

                // Create a shared material instance with colors applied (shared by all mesh parts)
                Material sharedMaterialInstance = null;
                if (m_WebGLBaseMaterial != null)
                {
                    sharedMaterialInstance = new Material(m_WebGLBaseMaterial);
                    ApplyColorsToMaterial(sharedMaterialInstance);
                }

                // Copy each mesh part and bind to the skeleton
                foreach (var sourceMesh in newParts)
                {
                    if (sourceMesh == null) continue;

                    // IMPORTANT: Capture bone names from the SOURCE mesh BEFORE instantiation
                    // After instantiation, the bone references may become invalid
                    string[] boneNames = new string[sourceMesh.bones.Length];
                    string rootBoneName = sourceMesh.rootBone != null ? sourceMesh.rootBone.name : null;

                    for (int i = 0; i < sourceMesh.bones.Length; i++)
                    {
                        if (sourceMesh.bones[i] != null)
                        {
                            boneNames[i] = sourceMesh.bones[i].name;
                        }
                    }

                    // Instantiate the mesh part
                    var meshCopy = Instantiate(sourceMesh.gameObject, m_BuiltCharacter.transform);
                    meshCopy.name = sourceMesh.name;

                    var copiedRenderer = meshCopy.GetComponent<SkinnedMeshRenderer>();
                    if (copiedRenderer != null)
                    {
                        // Rebind bones using the captured bone names
                        RebindMeshToSkeletonByNames(copiedRenderer, boneNames, rootBoneName, targetBoneDict);

                        // Apply the shared material with colors
                        if (sharedMaterialInstance != null)
                        {
                            copiedRenderer.sharedMaterial = sharedMaterialInstance;
                        }
                    }
                }

                // Hide the base model's original meshes (keep skeleton)
                var baseRenderers = baseInstance.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var renderer in baseRenderers)
                {
                    renderer.enabled = false;
                }
            }
            else
            {
                Debug.LogWarning("[SidekickPlayerController] WebGL: Could not find skeleton root in base model");
            }
        }
        else
        {
            Debug.LogError("[SidekickPlayerController] WebGL: No base model available!");
            return;
        }

        // Hide old Opsive model meshes and find the Opsive animator
        var oldMonitors = GetComponentsInChildren<Opsive.UltimateCharacterController.Character.AnimatorMonitor>(true);
        Animator opsiveAnimator = null;
        Debug.Log($"[SidekickPlayerController] WebGL: Found {oldMonitors.Length} AnimatorMonitors");

        foreach (var oldMonitor in oldMonitors)
        {
            var meshRenderers = oldMonitor.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var renderer in meshRenderers)
            {
                renderer.enabled = false;
            }

            var regularRenderers = oldMonitor.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var renderer in regularRenderers)
            {
                renderer.enabled = false;
            }

            var animator = oldMonitor.GetComponent<Animator>();
            if (animator != null)
            {
                opsiveAnimator = animator;
                Debug.Log($"[SidekickPlayerController] WebGL: Found Opsive animator on {oldMonitor.name}");
            }
        }

        // Fallback: Try to find any animator on this gameobject if AnimatorMonitor search failed
        if (opsiveAnimator == null)
        {
            opsiveAnimator = GetComponentInChildren<Animator>(true);
            if (opsiveAnimator != null)
            {
                Debug.Log($"[SidekickPlayerController] WebGL: Found fallback animator on {opsiveAnimator.gameObject.name}");
            }
        }

        // Parent the new character
        m_BuiltCharacter.transform.SetParent(transform);
        m_BuiltCharacter.transform.localPosition = Vector3.zero;
        m_BuiltCharacter.transform.localRotation = Quaternion.identity;

        // Set up animator sync
        var sidekickAnimator = m_BuiltCharacter.GetComponentInChildren<Animator>();
        Debug.Log($"[SidekickPlayerController] WebGL: Sidekick animator: {(sidekickAnimator != null ? sidekickAnimator.gameObject.name : "NULL")}");
        Debug.Log($"[SidekickPlayerController] WebGL: Opsive animator: {(opsiveAnimator != null ? opsiveAnimator.gameObject.name : "NULL")}");

        if (sidekickAnimator != null)
        {
            // Always try to assign an animator controller
            if (sidekickAnimator.runtimeAnimatorController == null)
            {
                if (opsiveAnimator != null && opsiveAnimator.runtimeAnimatorController != null)
                {
                    sidekickAnimator.runtimeAnimatorController = opsiveAnimator.runtimeAnimatorController;
                    Debug.Log($"[SidekickPlayerController] WebGL: Copied animator controller from Opsive: {opsiveAnimator.runtimeAnimatorController.name}");
                }
                else if (m_AnimatorController != null)
                {
                    sidekickAnimator.runtimeAnimatorController = m_AnimatorController;
                    Debug.Log($"[SidekickPlayerController] WebGL: Using assigned animator controller: {m_AnimatorController.name}");
                }
                else
                {
                    Debug.LogError("[SidekickPlayerController] WebGL: No animator controller available! Character will T-pose.");
                }
            }

            // Set up AnimatorSync if we have both animators
            if (opsiveAnimator != null)
            {
                var animSync = m_BuiltCharacter.GetComponent<AnimatorSync>();
                if (animSync == null)
                {
                    animSync = m_BuiltCharacter.AddComponent<AnimatorSync>();
                }
                animSync.Initialize(opsiveAnimator, sidekickAnimator);

                var eventRelay = m_BuiltCharacter.GetComponent<AnimationEventRelay>();
                if (eventRelay == null)
                {
                    eventRelay = m_BuiltCharacter.AddComponent<AnimationEventRelay>();
                }
            }
            else
            {
                Debug.LogWarning("[SidekickPlayerController] WebGL: No Opsive animator found - AnimatorSync will not work");
            }
        }
        else
        {
            Debug.LogError("[SidekickPlayerController] WebGL: No Sidekick animator found!");
        }

        // Update backpack attach points
        UpdateBackpackAttachPoints();

        // Create holsters on the new skeleton
        CreateHolstersOnSidekick();

        // Set up weapon item sync
        SetupWeaponItemSync();

        if (m_DebugLog)
        {
            Debug.Log($"[SidekickPlayerController] WebGL: Built character successfully");
        }
    }

    /// <summary>
    /// Rebind a SkinnedMeshRenderer's bones to a new skeleton using pre-captured bone names.
    /// This is more reliable than trying to read bone names from an instantiated mesh.
    /// </summary>
    private void RebindMeshToSkeletonByNames(SkinnedMeshRenderer meshRenderer, string[] boneNames, string rootBoneName, Dictionary<string, Transform> targetBoneDict)
    {
        if (meshRenderer == null || boneNames == null) return;

        var newBones = new Transform[boneNames.Length];
        int foundCount = 0;
        int missingCount = 0;

        // Build a fallback bone lookup for different body parts
        Transform headFallback = null;
        Transform hipsFallback = null;
        Transform spineFallback = null;
        Transform rootFallback = null;

        targetBoneDict.TryGetValue("head", out headFallback);
        if (headFallback == null) targetBoneDict.TryGetValue("Head", out headFallback);
        targetBoneDict.TryGetValue("hips", out hipsFallback);
        if (hipsFallback == null) targetBoneDict.TryGetValue("Hips", out hipsFallback);
        targetBoneDict.TryGetValue("spine_01", out spineFallback);
        if (spineFallback == null) targetBoneDict.TryGetValue("Spine", out spineFallback);
        if (spineFallback == null) spineFallback = hipsFallback;

        // Get root bone as the ultimate fallback - NEVER leave bones null
        targetBoneDict.TryGetValue("root", out rootFallback);
        if (rootFallback == null) targetBoneDict.TryGetValue("Root", out rootFallback);
        if (rootFallback == null) rootFallback = hipsFallback;
        if (rootFallback == null && targetBoneDict.Count > 0)
        {
            // Use any bone as fallback if nothing else works
            foreach (var kvp in targetBoneDict)
            {
                rootFallback = kvp.Value;
                break;
            }
        }

        for (int i = 0; i < boneNames.Length; i++)
        {
            string boneName = boneNames[i];

            // CRITICAL: For empty bone names, still need to assign something to avoid null
            if (string.IsNullOrEmpty(boneName))
            {
                newBones[i] = rootFallback;
                continue;
            }

            if (targetBoneDict.TryGetValue(boneName, out var newBone))
            {
                newBones[i] = newBone;
                foundCount++;
            }
            else
            {
                // Try common bone name variations
                string altName = TryFindAlternateBoneName(boneName, targetBoneDict);
                if (altName != null && targetBoneDict.TryGetValue(altName, out newBone))
                {
                    newBones[i] = newBone;
                    foundCount++;
                }
                else
                {
                    // Smart fallback based on bone type
                    Transform fallbackBone = GetSmartFallbackBone(boneName, targetBoneDict, headFallback, hipsFallback, spineFallback);

                    // CRITICAL: Always use a fallback - NEVER leave bones null
                    // Null bones cause vertices to stretch to origin (spaghetti effect)
                    if (fallbackBone == null)
                    {
                        fallbackBone = rootFallback;
                    }

                    newBones[i] = fallbackBone;

                    if (m_DebugLog && fallbackBone != null)
                    {
                        Debug.LogWarning($"[SidekickPlayerController] WebGL: Bone '{boneName}' not found, using fallback '{fallbackBone.name}'");
                    }
                    missingCount++;
                }
            }
        }

        // Final safety check - ensure no null bones remain
        for (int i = 0; i < newBones.Length; i++)
        {
            if (newBones[i] == null)
            {
                newBones[i] = rootFallback;
                if (m_DebugLog)
                {
                    Debug.LogWarning($"[SidekickPlayerController] WebGL: Bone index {i} was null, using root fallback");
                }
            }
        }

        meshRenderer.bones = newBones;

        // Update root bone
        if (!string.IsNullOrEmpty(rootBoneName) && targetBoneDict.TryGetValue(rootBoneName, out var newRootBone))
        {
            meshRenderer.rootBone = newRootBone;
        }
        else if (!string.IsNullOrEmpty(rootBoneName))
        {
            // Try to find an alternate root bone
            string altRoot = TryFindAlternateBoneName(rootBoneName, targetBoneDict);
            if (altRoot != null && targetBoneDict.TryGetValue(altRoot, out newRootBone))
            {
                meshRenderer.rootBone = newRootBone;
            }
        }

        if (m_DebugLog)
        {
            Debug.Log($"[SidekickPlayerController] WebGL: Bound {foundCount}/{boneNames.Length} bones for {meshRenderer.name} ({missingCount} using fallback)");
        }
    }

    /// <summary>
    /// Try to find alternate bone names (handles case differences and common naming variations).
    /// </summary>
    private string TryFindAlternateBoneName(string boneName, Dictionary<string, Transform> boneDict)
    {
        // Try exact match first (already done in caller)

        // Try case-insensitive match
        foreach (var kvp in boneDict)
        {
            if (string.Equals(kvp.Key, boneName, System.StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Key;
            }
        }

        // Try common naming variations
        // Sidekick uses names like "spine_01" while some rigs use "Spine1"
        string normalized = boneName.Replace("_", "").Replace("-", "").ToLower();
        foreach (var kvp in boneDict)
        {
            string targetNormalized = kvp.Key.Replace("_", "").Replace("-", "").ToLower();
            if (normalized == targetNormalized)
            {
                return kvp.Key;
            }
        }

        // Try partial match for L/R suffixes
        // e.g., "hand_l" might be "HandLeft" or "hand.L"
        if (boneName.EndsWith("_l") || boneName.EndsWith("_L") || boneName.EndsWith(".l") || boneName.EndsWith(".L"))
        {
            string baseName = boneName.Substring(0, boneName.Length - 2);
            foreach (var kvp in boneDict)
            {
                if (kvp.Key.ToLower().Contains(baseName.ToLower()) &&
                    (kvp.Key.ToLower().Contains("left") || kvp.Key.EndsWith("_l") || kvp.Key.EndsWith(".L")))
                {
                    return kvp.Key;
                }
            }
        }
        if (boneName.EndsWith("_r") || boneName.EndsWith("_R") || boneName.EndsWith(".r") || boneName.EndsWith(".R"))
        {
            string baseName = boneName.Substring(0, boneName.Length - 2);
            foreach (var kvp in boneDict)
            {
                if (kvp.Key.ToLower().Contains(baseName.ToLower()) &&
                    (kvp.Key.ToLower().Contains("right") || kvp.Key.EndsWith("_r") || kvp.Key.EndsWith(".R")))
                {
                    return kvp.Key;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Get a smart fallback bone based on the missing bone's name.
    /// Only returns fallback for dynamic/physics bones. Critical skeleton bones should match exactly.
    /// </summary>
    private Transform GetSmartFallbackBone(string boneName, Dictionary<string, Transform> boneDict,
        Transform headFallback, Transform hipsFallback, Transform spineFallback)
    {
        string lowerName = boneName.ToLower();

        // Dynamic/physics bones (hair_dyn_*, cloth_dyn_*, etc.) - use head fallback
        if (lowerName.Contains("_dyn") || lowerName.Contains("dyn_"))
        {
            return headFallback;
        }

        // Twist bones (arm_upper_l_twist) - try to find parent bone
        if (lowerName.Contains("twist"))
        {
            // Remove "_twist" or "twist_" and try to find the parent
            string parentName = boneName.Replace("_twist", "").Replace("twist_", "");
            if (boneDict.TryGetValue(parentName, out var parentBone))
            {
                return parentBone;
            }
            // Try alternate naming
            string altParent = TryFindAlternateBoneName(parentName, boneDict);
            if (altParent != null && boneDict.TryGetValue(altParent, out parentBone))
            {
                return parentBone;
            }
        }

        // Helper bones (ik targets, etc.) - try to find related bone
        if (lowerName.Contains("_ik") || lowerName.Contains("ik_") ||
            lowerName.Contains("_target") || lowerName.Contains("target_") ||
            lowerName.Contains("_helper") || lowerName.Contains("helper_"))
        {
            // These are optional bones, use appropriate fallback
            if (lowerName.Contains("arm") || lowerName.Contains("hand"))
            {
                return spineFallback;
            }
            if (lowerName.Contains("leg") || lowerName.Contains("foot"))
            {
                return hipsFallback;
            }
            return spineFallback;
        }

        // Roll bones - try to find parent
        if (lowerName.Contains("_roll") || lowerName.Contains("roll_"))
        {
            string parentName = boneName.Replace("_roll", "").Replace("roll_", "");
            if (boneDict.TryGetValue(parentName, out var parentBone))
            {
                return parentBone;
            }
            string altParent = TryFindAlternateBoneName(parentName, boneDict);
            if (altParent != null && boneDict.TryGetValue(altParent, out parentBone))
            {
                return parentBone;
            }
        }

        // For critical skeleton bones (arm, leg, spine, etc.), don't use fallback
        // These should be found - if not, there's a naming mismatch we need to fix
        if (lowerName.Contains("arm") || lowerName.Contains("leg") ||
            lowerName.Contains("hand") || lowerName.Contains("foot") ||
            lowerName.Contains("spine") || lowerName.Contains("hip") ||
            lowerName.Contains("shoulder") || lowerName.Contains("clavicle") ||
            lowerName.Contains("thigh") || lowerName.Contains("calf") ||
            lowerName.Contains("shin") || lowerName.Contains("forearm") ||
            lowerName.Contains("upper") || lowerName.Contains("lower"))
        {
            // Log error but don't use fallback - this would cause stretching
            if (m_DebugLog)
            {
                Debug.LogError($"[SidekickPlayerController] CRITICAL: Bone '{boneName}' not found! This will cause mesh issues.");
            }
            return null;
        }

        // Unknown bone type - use spine as generic fallback
        return spineFallback;
    }

    /// <summary>
    /// Rebind a SkinnedMeshRenderer's bones to a new skeleton (legacy method for non-WebGL).
    /// </summary>
    private void RebindMeshToSkeleton(SkinnedMeshRenderer meshRenderer, Transform newSkeletonRoot)
    {
        if (meshRenderer == null || meshRenderer.bones == null) return;

        // Build a dictionary of bones in the new skeleton
        var boneDict = new Dictionary<string, Transform>();
        foreach (var bone in newSkeletonRoot.GetComponentsInChildren<Transform>())
        {
            if (!boneDict.ContainsKey(bone.name))
            {
                boneDict[bone.name] = bone;
            }
        }

        // Capture bone names first
        string[] boneNames = new string[meshRenderer.bones.Length];
        for (int i = 0; i < meshRenderer.bones.Length; i++)
        {
            if (meshRenderer.bones[i] != null)
            {
                boneNames[i] = meshRenderer.bones[i].name;
            }
        }

        string rootBoneName = meshRenderer.rootBone != null ? meshRenderer.rootBone.name : null;

        // Use the improved method
        RebindMeshToSkeletonByNames(meshRenderer, boneNames, rootBoneName, boneDict);
    }

    /// <summary>
    /// Updates BackpackEquipHandler to use bones from the Sidekick character instead of the Opsive skeleton.
    /// This prevents jiggling caused by slight animation timing differences between skeletons.
    /// </summary>
    private void UpdateBackpackAttachPoints()
    {
        if (m_BuiltCharacter == null) return;

        var backpackHandler = GetComponent<BackpackEquipHandler>();
        if (backpackHandler == null) return;

        // Find spine bone on the Sidekick character
        Transform sidekickSpine = FindBoneInHierarchy(m_BuiltCharacter.transform, "Spine", "spine", "Spine1", "spine_01", "spine_02", "Spine2");
        if (sidekickSpine == null)
        {
            Debug.LogWarning("[SidekickPlayerController] Could not find spine bone on Sidekick character for backpack attachment");
            return;
        }

        // Find or create attach points on the Sidekick skeleton using configurable offsets and rotations
        var smallAttach = FindOrCreateAttachPoint(sidekickSpine, "SmallBackpackAttachPoint", m_SmallBackpackOffset, m_SmallBackpackRotation);
        var mediumAttach = FindOrCreateAttachPoint(sidekickSpine, "MediumBackpackAttachPoint", m_MediumBackpackOffset, m_MediumBackpackRotation);
        var largeAttach = FindOrCreateAttachPoint(sidekickSpine, "LargeBackpackAttachPoint", m_LargeBackpackOffset, m_LargeBackpackRotation);

        // Update the BackpackEquipHandler via reflection or serialized fields
        var handlerType = backpackHandler.GetType();
        var smallField = handlerType.GetField("m_SmallBackpackAttachPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var mediumField = handlerType.GetField("m_MediumBackpackAttachPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var largeField = handlerType.GetField("m_LargeBackpackAttachPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (smallField != null) smallField.SetValue(backpackHandler, smallAttach);
        if (mediumField != null) mediumField.SetValue(backpackHandler, mediumAttach);
        if (largeField != null) largeField.SetValue(backpackHandler, largeAttach);

        Debug.Log($"[SidekickPlayerController] Updated backpack attach points to Sidekick skeleton: {sidekickSpine.name}");
    }

    /// <summary>
    /// Creates holster attachment points on the Sidekick character skeleton.
    /// Also removes duplicate ObjectIdentifiers from the original Opsive character
    /// to prevent Opsive from finding the wrong holster.
    /// </summary>
    private void CreateHolstersOnSidekick()
    {
        if (m_BuiltCharacter == null) return;

        // IMPORTANT: First, remove ObjectIdentifiers from the original Opsive character
        // to prevent duplicates (Opsive will find the first one, which might be wrong)
        RemoveDuplicateObjectIdentifiers(m_PistolHolsterID);
        RemoveDuplicateObjectIdentifiers(m_RifleHolsterID);

        // Find pelvis/hips bone on the Sidekick character for pistol holster
        Transform sidekickPelvis = FindBoneInHierarchy(m_BuiltCharacter.transform,
            "Pelvis", "pelvis", "Hips", "hips", "Hip", "hip", "pelvis_01");

        if (sidekickPelvis == null)
        {
            Debug.LogWarning("[SidekickPlayerController] Could not find pelvis bone on Sidekick character for pistol holster");
            return;
        }

        // Create pistol holster
        var pistolHolster = FindOrCreateAttachPoint(sidekickPelvis, "PistolHolster", m_PistolHolsterOffset, m_PistolHolsterRotation);
        AddObjectIdentifier(pistolHolster, m_PistolHolsterID);
        Debug.Log($"[SidekickPlayerController] Created PistolHolster on Sidekick skeleton, ID={m_PistolHolsterID}");

        // Find spine bone for rifle holster
        Transform sidekickSpine = FindBoneInHierarchy(m_BuiltCharacter.transform,
            "Spine", "spine", "Spine1", "spine_01", "spine_02", "Spine2");

        if (sidekickSpine == null)
        {
            Debug.LogWarning("[SidekickPlayerController] Could not find spine bone for rifle holster");
            return;
        }

        // Create rifle holster (the one that actually holds the rifle)
        var rifleHolster = FindOrCreateAttachPoint(sidekickSpine, "RifleHolster", m_RifleHolsterDefaultOffset, m_RifleHolsterDefaultRotation);
        AddObjectIdentifier(rifleHolster, m_RifleHolsterID);

        // Create holster spots for different backpack sizes (these are position references for HolsterPositionAdjuster)
        var defaultSpot = FindOrCreateAttachPoint(sidekickSpine, "DefaultHolsterSpot", m_RifleHolsterDefaultOffset, m_RifleHolsterDefaultRotation);
        var smallBPSpot = FindOrCreateAttachPoint(sidekickSpine, "SmallBackpackHolsterSpot", m_RifleHolsterSmallBPOffset, m_RifleHolsterSmallBPRotation);
        var mediumBPSpot = FindOrCreateAttachPoint(sidekickSpine, "MediumBackpackHolsterSpot", m_RifleHolsterMediumBPOffset, m_RifleHolsterMediumBPRotation);
        var largeBPSpot = FindOrCreateAttachPoint(sidekickSpine, "LargeBackpackHolsterSpot", m_RifleHolsterLargeBPOffset, m_RifleHolsterLargeBPRotation);

        // Update HolsterPositionAdjuster references
        var holsterAdjuster = GetComponent<HolsterPositionAdjuster>();
        if (holsterAdjuster != null)
        {
            var adjusterType = holsterAdjuster.GetType();
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            adjusterType.GetField("m_RifleHolster", flags)?.SetValue(holsterAdjuster, rifleHolster);
            adjusterType.GetField("m_DefaultHolsterSpot", flags)?.SetValue(holsterAdjuster, defaultSpot);
            adjusterType.GetField("m_SmallBackpackHolsterSpot", flags)?.SetValue(holsterAdjuster, smallBPSpot);
            adjusterType.GetField("m_MediumBackpackHolsterSpot", flags)?.SetValue(holsterAdjuster, mediumBPSpot);
            adjusterType.GetField("m_LargeBackpackHolsterSpot", flags)?.SetValue(holsterAdjuster, largeBPSpot);

            Debug.Log($"[SidekickPlayerController] Updated HolsterPositionAdjuster with Sidekick rifle holster spots");
        }

        Debug.Log($"[SidekickPlayerController] Created RifleHolster on Sidekick skeleton, ID={m_RifleHolsterID}");
    }

    private void AddObjectIdentifier(Transform target, uint id)
    {
        var objectId = target.GetComponent<Opsive.UltimateCharacterController.Objects.ObjectIdentifier>();
        if (objectId == null)
        {
            objectId = target.gameObject.AddComponent<Opsive.UltimateCharacterController.Objects.ObjectIdentifier>();
        }
        objectId.ID = id;
    }

    /// <summary>
    /// Removes ObjectIdentifiers with the specified ID from the Sidekick character only.
    /// We KEEP the original Opsive character's holsters because that's where Opsive looks.
    /// </summary>
    private void RemoveDuplicateObjectIdentifiers(uint targetId)
    {
        var allIdentifiers = GetComponentsInChildren<Opsive.UltimateCharacterController.Objects.ObjectIdentifier>(true);

        foreach (var identifier in allIdentifiers)
        {
            // Only remove if this IS part of the Sidekick character
            // We want to KEEP the holsters on the original Opsive character (SidekickPlayer)
            if (m_BuiltCharacter != null && identifier.transform.IsChildOf(m_BuiltCharacter.transform))
            {
                if (identifier.ID == targetId)
                {
                    Debug.Log($"[SidekickPlayerController] Removing ObjectIdentifier ID {targetId} from SidekickCharacter: {identifier.gameObject.name}");
                    identifier.ID = 0;
                }
            }
        }
    }

    /// <summary>
    /// Set up the WeaponItemSync component to handle dynamically spawned weapons.
    /// This ensures weapons spawned by Opsive appear attached to the Sidekick skeleton.
    /// </summary>
    private void SetupWeaponItemSync()
    {
        if (m_BuiltCharacter == null) return;

        // Add WeaponItemSync if not present
        var weaponSync = GetComponent<WeaponItemSync>();
        if (weaponSync == null)
        {
            weaponSync = gameObject.AddComponent<WeaponItemSync>();
        }

        // Initialize with the Sidekick character
        weaponSync.Initialize(m_BuiltCharacter);

        if (m_DebugLog)
        {
            Debug.Log("[SidekickPlayerController] Set up WeaponItemSync for dynamic weapon reparenting");
        }
    }

    /// <summary>
    /// Re-parent weapon item slots from the Opsive skeleton to the Sidekick skeleton.
    /// NOTE: This method is deprecated - use SetupWeaponItemSync instead for dynamic weapon handling.
    /// </summary>
    private void ReparentItemSlotsToSidekick()
    {
        if (m_BuiltCharacter == null) return;

        // Find the Sidekick hand bones
        Transform sidekickRightHand = FindBoneInHierarchy(m_BuiltCharacter.transform,
            "RightHand", "Hand_R", "hand_r", "Right Hand", "hand.R", "HandR", "r_hand", "R_Hand");
        Transform sidekickLeftHand = FindBoneInHierarchy(m_BuiltCharacter.transform,
            "LeftHand", "Hand_L", "hand_l", "Left Hand", "hand.L", "HandL", "l_hand", "L_Hand");

        if (sidekickRightHand == null && sidekickLeftHand == null)
        {
            Debug.LogWarning("[SidekickPlayerController] Could not find hand bones on Sidekick character");
            return;
        }

        // Find item slots on the Opsive character (they're typically children of hand bones or named "Items")
        var allTransforms = GetComponentsInChildren<Transform>(true);

        foreach (var t in allTransforms)
        {
            // Skip transforms that are part of the Sidekick character
            if (m_BuiltCharacter != null && t.IsChildOf(m_BuiltCharacter.transform))
                continue;

            string name = t.name.ToLower();

            // Look for item slot patterns
            bool isRightHandSlot = name.Contains("right") && (name.Contains("item") || name.Contains("slot") || name.Contains("hand"));
            bool isLeftHandSlot = name.Contains("left") && (name.Contains("item") || name.Contains("slot") || name.Contains("hand"));
            bool isItemsContainer = name == "items" && t.parent != null;

            if (isRightHandSlot && sidekickRightHand != null)
            {
                // Store local position/rotation before reparenting
                Vector3 localPos = t.localPosition;
                Quaternion localRot = t.localRotation;

                t.SetParent(sidekickRightHand);
                t.localPosition = localPos;
                t.localRotation = localRot;

                Debug.Log($"[SidekickPlayerController] Re-parented '{t.name}' to Sidekick right hand");
            }
            else if (isLeftHandSlot && sidekickLeftHand != null)
            {
                Vector3 localPos = t.localPosition;
                Quaternion localRot = t.localRotation;

                t.SetParent(sidekickLeftHand);
                t.localPosition = localPos;
                t.localRotation = localRot;

                Debug.Log($"[SidekickPlayerController] Re-parented '{t.name}' to Sidekick left hand");
            }
            else if (isItemsContainer)
            {
                // Check if the parent is a hand bone on the Opsive skeleton
                string parentName = t.parent.name.ToLower();
                if (parentName.Contains("right") && parentName.Contains("hand") && sidekickRightHand != null)
                {
                    Vector3 localPos = t.localPosition;
                    Quaternion localRot = t.localRotation;

                    t.SetParent(sidekickRightHand);
                    t.localPosition = localPos;
                    t.localRotation = localRot;

                    Debug.Log($"[SidekickPlayerController] Re-parented '{t.name}' to Sidekick right hand (from {parentName})");
                }
                else if (parentName.Contains("left") && parentName.Contains("hand") && sidekickLeftHand != null)
                {
                    Vector3 localPos = t.localPosition;
                    Quaternion localRot = t.localRotation;

                    t.SetParent(sidekickLeftHand);
                    t.localPosition = localPos;
                    t.localRotation = localRot;

                    Debug.Log($"[SidekickPlayerController] Re-parented '{t.name}' to Sidekick left hand (from {parentName})");
                }
            }
        }

        Debug.Log($"[SidekickPlayerController] Finished re-parenting item slots to Sidekick skeleton");
    }

    private Transform FindBoneInHierarchy(Transform root, params string[] possibleNames)
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            foreach (var name in possibleNames)
            {
                if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }
        }
        return null;
    }

    private Transform FindOrCreateAttachPoint(Transform parent, string name, Vector3 localOffset, Vector3 localRotation)
    {
        // Check if it already exists
        var existing = parent.Find(name);
        if (existing != null)
        {
            // Update position and rotation in case they changed
            existing.localPosition = localOffset;
            existing.localRotation = Quaternion.Euler(localRotation);
            return existing;
        }

        // Create new attach point
        var attachPoint = new GameObject(name).transform;
        attachPoint.SetParent(parent);
        attachPoint.localPosition = localOffset;
        attachPoint.localRotation = Quaternion.Euler(localRotation);
        return attachPoint;
    }

    /// <summary>
    /// Swap meshes on existing skeleton.
    /// Used for in-game clothing changes on Opsive character.
    /// </summary>
    private void ApplyAppearanceSwapMeshes(List<SkinnedMeshRenderer> newParts)
    {
        // Remove old meshes
        foreach (var meshObj in m_MeshObjects)
        {
            if (meshObj != null)
            {
                Destroy(meshObj);
            }
        }
        m_MeshObjects.Clear();

        // Get the base material (load from Resources if not cached)
        Material runtimeMaterial = m_BaseMaterialOverride;
        if (runtimeMaterial == null)
        {
            runtimeMaterial = Resources.Load<Material>("Materials/M_BaseMaterial");
        }

        // Create new meshes bound to the existing skeleton
        foreach (var sourceMesh in newParts)
        {
            GameObject meshObj = CreateMeshPart(sourceMesh, runtimeMaterial);
            if (meshObj != null)
            {
                m_MeshObjects.Add(meshObj);
            }
        }

        if (m_DebugLog)
        {
            Debug.Log($"[SidekickPlayerController] Swapped {m_MeshObjects.Count} meshes on existing skeleton");
        }
    }

    private GameObject CreateMeshPart(SkinnedMeshRenderer sourceMesh, Material runtimeMaterial = null)
    {
        // Create a new GameObject for this mesh part
        GameObject partObj = new GameObject(sourceMesh.name);
        partObj.transform.SetParent(transform);
        partObj.transform.localPosition = Vector3.zero;
        partObj.transform.localRotation = Quaternion.identity;
        partObj.transform.localScale = Vector3.one;

        // Add SkinnedMeshRenderer and copy mesh data
        SkinnedMeshRenderer newRenderer = partObj.AddComponent<SkinnedMeshRenderer>();
        newRenderer.sharedMesh = sourceMesh.sharedMesh;

        // Use runtime material if provided (for proper color support), otherwise copy from source
        if (runtimeMaterial != null)
        {
            newRenderer.sharedMaterial = runtimeMaterial;
        }
        else
        {
            newRenderer.sharedMaterials = sourceMesh.sharedMaterials;
        }

        // Bind to existing skeleton - map source bones to our skeleton
        Transform[] sourceBones = sourceMesh.bones;
        Transform[] newBones = new Transform[sourceBones.Length];

        for (int i = 0; i < sourceBones.Length; i++)
        {
            if (sourceBones[i] != null && m_BoneMap.TryGetValue(sourceBones[i].name, out Transform matchedBone))
            {
                newBones[i] = matchedBone;
            }
            else
            {
                // Fallback to skeleton root if bone not found
                newBones[i] = m_SkeletonRoot;
            }
        }

        newRenderer.bones = newBones;
        newRenderer.rootBone = m_SkeletonRoot;
        newRenderer.localBounds = sourceMesh.localBounds;

        // Copy blend shape weights from source if any
        if (sourceMesh.sharedMesh.blendShapeCount > 0)
        {
            for (int i = 0; i < sourceMesh.sharedMesh.blendShapeCount; i++)
            {
                float weight = sourceMesh.GetBlendShapeWeight(i);
                newRenderer.SetBlendShapeWeight(i, weight);
            }
        }

        return partObj;
    }

    private void AddPartsFromPreset(SidekickPartPreset preset, List<SkinnedMeshRenderer> partsList, HashSet<CharacterPartType> skipTypes = null)
    {
        if (preset == null) return;

        var rows = SidekickPartPresetRow.GetAllByPreset(m_DbManager, preset);
        foreach (var row in rows)
        {
            if (string.IsNullOrEmpty(row.PartName)) continue;

            try
            {
                CharacterPartType type = Enum.Parse<CharacterPartType>(
                    CharacterPartTypeUtils.GetTypeNameFromShortcode(row.PartType));

                // Skip this part type if we have a custom selection for it
                if (skipTypes != null && skipTypes.Contains(type))
                {
                    if (m_DebugLog)
                    {
                        Debug.Log($"[SidekickPlayerController] Skipping {type} from preset (custom selection)");
                    }
                    continue;
                }

                if (m_PartLibrary.TryGetValue(type, out var partDict))
                {
                    if (partDict.TryGetValue(row.PartName, out var part))
                    {
                        GameObject partModel = part.GetPartModel();
                        if (partModel != null)
                        {
                            var mesh = partModel.GetComponentInChildren<SkinnedMeshRenderer>();
                            if (mesh != null)
                            {
                                partsList.Add(mesh);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (m_DebugLog)
                {
                    Debug.LogWarning($"[SidekickPlayerController] Failed to load part {row.PartName}: {e.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Add parts from a WebGL preset to the parts list.
    /// Used in WebGL builds where SQLite is not available.
    /// </summary>
    private void AddPartsFromWebGLPreset(SidekickWebGLLoader.WebGLPreset preset, List<SkinnedMeshRenderer> partsList, HashSet<string> skipPartTypes = null)
    {
        if (preset == null)
        {
            Debug.LogWarning("[SidekickPlayerController] WebGL: AddPartsFromWebGLPreset called with NULL preset!");
            return;
        }

        Debug.Log($"[SidekickPlayerController] WebGL: AddPartsFromWebGLPreset for '{preset.Name}' with {preset.Parts?.Count ?? 0} parts");

        if (preset.Parts == null || preset.Parts.Count == 0)
        {
            Debug.LogWarning($"[SidekickPlayerController] WebGL: Preset '{preset.Name}' has no parts!");
            return;
        }

        foreach (var part in preset.Parts)
        {
            if (string.IsNullOrEmpty(part.PartName)) continue;

            // Skip this part type if requested
            if (skipPartTypes != null && skipPartTypes.Contains(part.PartType))
            {
                if (m_DebugLog)
                {
                    Debug.Log($"[SidekickPlayerController] WebGL: Skipping {part.PartType} from preset");
                }
                continue;
            }

            try
            {
                GameObject partModel = part.GetPartModel();
                if (partModel != null)
                {
                    var mesh = partModel.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (mesh != null)
                    {
                        partsList.Add(mesh);
                        if (m_DebugLog)
                        {
                            Debug.Log($"[SidekickPlayerController] WebGL: Added part {part.PartName}");
                        }
                    }
                }
                else
                {
                    if (m_DebugLog)
                    {
                        Debug.LogWarning($"[SidekickPlayerController] WebGL: Could not load model for part {part.PartName}");
                    }
                }
            }
            catch (Exception e)
            {
                if (m_DebugLog)
                {
                    Debug.LogWarning($"[SidekickPlayerController] WebGL: Failed to load part {part.PartName}: {e.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Add an individual part's mesh to the parts list.
    /// Used for face customization (hair, eyebrows, nose, ears, facial hair).
    /// </summary>
    private void AddIndividualPart(SidekickPart part, List<SkinnedMeshRenderer> partsList)
    {
        if (part == null) return;

        try
        {
            GameObject partModel = part.GetPartModel();
            if (partModel != null)
            {
                var mesh = partModel.GetComponentInChildren<SkinnedMeshRenderer>();
                if (mesh != null)
                {
                    partsList.Add(mesh);
                    if (m_DebugLog)
                    {
                        Debug.Log($"[SidekickPlayerController] Added individual part: {part.Name}");
                    }
                }
            }
        }
        catch (Exception e)
        {
            if (m_DebugLog)
            {
                Debug.LogWarning($"[SidekickPlayerController] Failed to load individual part {part.Name}: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Add an individual part's mesh to the parts list (WebGL mode).
    /// Uses the Location property to load from Resources.
    /// </summary>
    /// <param name="part">The SidekickPart with Location set to the file path</param>
    /// <param name="partsList">List to add the mesh to</param>
    /// <param name="partType">Optional: specify if this is a paired part (EyebrowLeft/EarLeft) that needs mirroring</param>
    private void AddIndividualPartWebGL(SidekickPart part, List<SkinnedMeshRenderer> partsList, CharacterPartType? partType = null)
    {
        if (part == null) return;

        string filePath = part.Location;
        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogWarning($"[SidekickPlayerController] WebGL: No file path for part: {part.Name}");
            return;
        }

        try
        {
            Debug.Log($"[SidekickPlayerController] WebGL: Loading individual part '{part.Name}' from path: {filePath}");

            // Normalize path separators to forward slashes
            string resourcePath = filePath.Replace('\\', '/');

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

            // Remove file extension
            int lastDot = resourcePath.LastIndexOf('.');
            if (lastDot >= 0)
            {
                resourcePath = resourcePath.Substring(0, lastDot);
            }

            Debug.Log($"[SidekickPlayerController] WebGL: Final resource path: {resourcePath}");

            GameObject partModel = Resources.Load<GameObject>(resourcePath);
            if (partModel != null)
            {
                var mesh = partModel.GetComponentInChildren<SkinnedMeshRenderer>();
                if (mesh != null)
                {
                    partsList.Add(mesh);
                    Debug.Log($"[SidekickPlayerController] WebGL: Added individual part: {part.Name}");

                    // For paired parts, we may need to load the right side as well
                    // The preset system handles left/right matching, but for individual parts
                    // we might only have left side stored - check if we need the mirror
                    if (partType.HasValue)
                    {
                        // Try to find and add the matching right-side part
                        string rightPartPath = TryGetMirroredPartPath(resourcePath, partType.Value);
                        if (!string.IsNullOrEmpty(rightPartPath))
                        {
                            GameObject rightModel = Resources.Load<GameObject>(rightPartPath);
                            if (rightModel != null)
                            {
                                var rightMesh = rightModel.GetComponentInChildren<SkinnedMeshRenderer>();
                                if (rightMesh != null)
                                {
                                    partsList.Add(rightMesh);
                                    Debug.Log($"[SidekickPlayerController] WebGL: Added mirrored part: {rightPartPath}");
                                }
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[SidekickPlayerController] WebGL: No SkinnedMeshRenderer found on part model: {part.Name}");
                }
            }
            else
            {
                Debug.LogWarning($"[SidekickPlayerController] WebGL: Could not load model from path: {resourcePath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SidekickPlayerController] WebGL: Failed to load individual part {part.Name}: {e.Message}");
        }
    }

    /// <summary>
    /// Try to find the mirrored (right-side) part path for a left-side part.
    /// </summary>
    private string TryGetMirroredPartPath(string leftPath, CharacterPartType partType)
    {
        // Map left part types to right
        string leftSuffix = "";
        string rightSuffix = "";

        switch (partType)
        {
            case CharacterPartType.EyebrowLeft:
                leftSuffix = "EYEBRL";
                rightSuffix = "EYEBRR";
                break;
            case CharacterPartType.EarLeft:
                leftSuffix = "EARL";
                rightSuffix = "EARR";
                break;
            default:
                return null;
        }

        // Try to replace the left suffix with right suffix in the path
        if (leftPath.Contains(leftSuffix))
        {
            return leftPath.Replace(leftSuffix, rightSuffix);
        }

        // Also try common naming patterns
        if (leftPath.Contains("Left"))
        {
            return leftPath.Replace("Left", "Right");
        }
        if (leftPath.Contains("_L_"))
        {
            return leftPath.Replace("_L_", "_R_");
        }

        return null;
    }

    /// <summary>
    /// Save appearance to PlayerPrefs.
    /// </summary>
    public void SaveAppearance()
    {
        CharacterSaveData saveData = new CharacterSaveData
        {
            headPresetName = m_CurrentHeadPreset?.Name,
            upperBodyPresetName = m_CurrentUpperBodyPreset?.Name,
            lowerBodyPresetName = m_CurrentLowerBodyPreset?.Name,
            bodyType = m_BodyType,
            muscles = m_Muscles,
            bodySize = m_BodySize
        };

        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(m_SaveKey, json);
        PlayerPrefs.Save();

        if (m_DebugLog)
        {
            Debug.Log("[SidekickPlayerController] Saved appearance");
        }
    }

    /// <summary>
    /// Load appearance from PlayerPrefs.
    /// </summary>
    public void LoadAppearance()
    {
        string json = PlayerPrefs.GetString(m_SaveKey, "");
        if (string.IsNullOrEmpty(json))
        {
            if (m_DebugLog)
            {
                Debug.Log("[SidekickPlayerController] No saved appearance found");
            }
            return;
        }

        try
        {
            CharacterSaveData saveData = JsonUtility.FromJson<CharacterSaveData>(json);
            if (saveData != null)
            {
                LoadSaveData(saveData);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SidekickPlayerController] Failed to load appearance: {e.Message}");
        }
    }

    /// <summary>
    /// Load from save data object.
    /// </summary>
    public void LoadSaveData(CharacterSaveData data)
    {
        if (data == null) return;

        if (m_UseWebGLLoader)
        {
            // WebGL mode: Load from WebGL preset collections
            var headPresets = m_WebGLPresetsByGroup[PartGroup.Head];
            var upperPresets = m_WebGLPresetsByGroup[PartGroup.UpperBody];
            var lowerPresets = m_WebGLPresetsByGroup[PartGroup.LowerBody];

            m_CurrentWebGLHeadPreset = headPresets.FirstOrDefault(p => p.Name == data.headPresetName) ?? headPresets.FirstOrDefault();

            if (m_UnderwearOnly)
            {
                m_CurrentWebGLUpperBodyPreset = upperPresets.FirstOrDefault(p => p.Name == BASE_BODY_PRESET) ?? upperPresets.FirstOrDefault();
                m_CurrentWebGLLowerBodyPreset = lowerPresets.FirstOrDefault(p => p.Name == BASE_BODY_PRESET) ?? lowerPresets.FirstOrDefault();
            }
            else
            {
                m_CurrentWebGLUpperBodyPreset = upperPresets.FirstOrDefault(p => p.Name == data.upperBodyPresetName) ?? upperPresets.FirstOrDefault();
                m_CurrentWebGLLowerBodyPreset = lowerPresets.FirstOrDefault(p => p.Name == data.lowerBodyPresetName) ?? lowerPresets.FirstOrDefault();
            }

            if (m_DebugLog)
            {
                Debug.Log($"[SidekickPlayerController] WebGL: Loaded appearance: Head={m_CurrentWebGLHeadPreset?.Name}, Upper={m_CurrentWebGLUpperBodyPreset?.Name}, Lower={m_CurrentWebGLLowerBodyPreset?.Name}");
            }
        }
        else
        {
            // Standard SQLite mode
            var headPresets = GetPresetsForGroup(PartGroup.Head);
            var upperPresets = GetPresetsForGroup(PartGroup.UpperBody);
            var lowerPresets = GetPresetsForGroup(PartGroup.LowerBody);

            m_CurrentHeadPreset = headPresets.FirstOrDefault(p => p.Name == data.headPresetName) ?? headPresets.FirstOrDefault();

            // If in underwear mode, force Base Body presets regardless of saved data
            if (m_UnderwearOnly)
            {
                m_CurrentUpperBodyPreset = upperPresets.FirstOrDefault(p => p.Name == BASE_BODY_PRESET) ?? upperPresets.FirstOrDefault();
                m_CurrentLowerBodyPreset = lowerPresets.FirstOrDefault(p => p.Name == BASE_BODY_PRESET) ?? lowerPresets.FirstOrDefault();
            }
            else
            {
                m_CurrentUpperBodyPreset = upperPresets.FirstOrDefault(p => p.Name == data.upperBodyPresetName) ?? upperPresets.FirstOrDefault();
                m_CurrentLowerBodyPreset = lowerPresets.FirstOrDefault(p => p.Name == data.lowerBodyPresetName) ?? lowerPresets.FirstOrDefault();
            }

            // Individual parts (SQLite mode only - we have SidekickPart objects)
            if (!string.IsNullOrEmpty(data.hairPartName))
            {
                SetPartByName(CharacterPartType.Hair, data.hairPartName, false);
            }
            if (!string.IsNullOrEmpty(data.eyebrowsPartName))
            {
                SetPartByName(CharacterPartType.EyebrowLeft, data.eyebrowsPartName, false);
            }
            if (!string.IsNullOrEmpty(data.nosePartName))
            {
                SetPartByName(CharacterPartType.Nose, data.nosePartName, false);
            }
            if (!string.IsNullOrEmpty(data.earsPartName))
            {
                SetPartByName(CharacterPartType.EarLeft, data.earsPartName, false);
            }
            if (!string.IsNullOrEmpty(data.facialHairPartName))
            {
                SetPartByName(CharacterPartType.FacialHair, data.facialHairPartName, false);
            }

            if (m_DebugLog)
            {
                Debug.Log($"[SidekickPlayerController] Loaded appearance: Head={m_CurrentHeadPreset?.Name}, Upper={m_CurrentUpperBodyPreset?.Name}, Lower={m_CurrentLowerBodyPreset?.Name}");
                Debug.Log($"[SidekickPlayerController] Individual parts: Hair={m_CurrentHair?.Name}, Eyebrows={m_CurrentEyebrows?.Name}, Nose={m_CurrentNose?.Name}");
            }
        }

        // Colors (works in both SQLite and WebGL modes)
        if (!string.IsNullOrEmpty(data.skinColorHex))
        {
            SetSkinColor(ColorSaveHelper.FromHex(data.skinColorHex, m_WebGLSkinColor), false);
        }
        if (!string.IsNullOrEmpty(data.hairColorHex))
        {
            SetHairColor(ColorSaveHelper.FromHex(data.hairColorHex, m_WebGLHairColor), false);
        }
        if (!string.IsNullOrEmpty(data.eyeColorHex))
        {
            SetEyeColor(ColorSaveHelper.FromHex(data.eyeColorHex, m_WebGLEyeColor), false);
        }

        // Body shape (works in both modes)
        m_BodyType = data.bodyType;
        m_Muscles = data.muscles;
        m_BodySize = data.bodySize;

        ApplyAppearance();
    }

    /// <summary>
    /// Get save data for current appearance.
    /// </summary>
    public CharacterSaveData GetSaveData()
    {
        var data = new CharacterSaveData
        {
            // Presets - use WebGL preset names if in WebGL mode
            headPresetName = m_UseWebGLLoader ? m_CurrentWebGLHeadPreset?.Name : m_CurrentHeadPreset?.Name,
            upperBodyPresetName = m_UseWebGLLoader ? m_CurrentWebGLUpperBodyPreset?.Name : m_CurrentUpperBodyPreset?.Name,
            lowerBodyPresetName = m_UseWebGLLoader ? m_CurrentWebGLLowerBodyPreset?.Name : m_CurrentLowerBodyPreset?.Name,

            // Body shape
            bodyType = m_BodyType,
            muscles = m_Muscles,
            bodySize = m_BodySize,

            // Individual parts (SQLite mode only)
            hairPartName = m_CurrentHair?.Name,
            eyebrowsPartName = m_CurrentEyebrows?.Name,
            nosePartName = m_CurrentNose?.Name,
            earsPartName = m_CurrentEars?.Name,
            facialHairPartName = m_CurrentFacialHair?.Name,

            // Colors - use SQLite colors if available, otherwise use WebGL colors
            skinColorHex = m_CurrentColors?.ContainsKey(COLOR_SKIN) == true
                ? ColorSaveHelper.ToHex(m_CurrentColors[COLOR_SKIN].NiceColor)
                : ColorSaveHelper.ToHex(m_WebGLSkinColor),
            hairColorHex = m_CurrentColors?.ContainsKey(COLOR_HAIR) == true
                ? ColorSaveHelper.ToHex(m_CurrentColors[COLOR_HAIR].NiceColor)
                : ColorSaveHelper.ToHex(m_WebGLHairColor),
            eyeColorHex = m_CurrentColors?.ContainsKey(COLOR_EYES) == true
                ? ColorSaveHelper.ToHex(m_CurrentColors[COLOR_EYES].NiceColor)
                : ColorSaveHelper.ToHex(m_WebGLEyeColor)
        };

        return data;
    }

    // Preset access methods
    public List<SidekickPartPreset> GetPresetsForGroup(PartGroup group)
    {
        if (m_PresetsByGroup == null || !m_PresetsByGroup.ContainsKey(group))
            return new List<SidekickPartPreset>();
        return m_PresetsByGroup[group];
    }

    public SidekickPartPreset GetCurrentPreset(PartGroup group)
    {
        return group switch
        {
            PartGroup.Head => m_CurrentHeadPreset,
            PartGroup.UpperBody => m_CurrentUpperBodyPreset,
            PartGroup.LowerBody => m_CurrentLowerBodyPreset,
            _ => null
        };
    }

    public int GetCurrentPresetIndex(PartGroup group)
    {
        var presets = GetPresetsForGroup(group);
        var current = GetCurrentPreset(group);
        return current != null ? presets.IndexOf(current) : 0;
    }

    // Preset setters
    public void SetHeadPreset(int index)
    {
        var presets = GetPresetsForGroup(PartGroup.Head);
        if (index >= 0 && index < presets.Count)
        {
            m_CurrentHeadPreset = presets[index];
            ApplyAppearance();
        }
    }

    public void SetUpperBodyPreset(int index)
    {
        var presets = GetPresetsForGroup(PartGroup.UpperBody);
        if (index >= 0 && index < presets.Count)
        {
            m_CurrentUpperBodyPreset = presets[index];
            ApplyAppearance();
        }
    }

    public void SetLowerBodyPreset(int index)
    {
        var presets = GetPresetsForGroup(PartGroup.LowerBody);
        if (index >= 0 && index < presets.Count)
        {
            m_CurrentLowerBodyPreset = presets[index];
            ApplyAppearance();
        }
    }

    // Navigation methods - use Context Menu (right-click component) to test
    [ContextMenu("Next Head")]
    public void NextHeadPreset()
    {
        var presets = GetPresetsForGroup(PartGroup.Head);
        if (presets.Count == 0) return;
        int currentIndex = presets.IndexOf(m_CurrentHeadPreset);
        int nextIndex = (currentIndex + 1) % presets.Count;
        m_CurrentHeadPreset = presets[nextIndex];
        ApplyAppearance();
    }

    [ContextMenu("Previous Head")]
    public void PreviousHeadPreset()
    {
        var presets = GetPresetsForGroup(PartGroup.Head);
        if (presets.Count == 0) return;
        int currentIndex = presets.IndexOf(m_CurrentHeadPreset);
        int prevIndex = currentIndex - 1;
        if (prevIndex < 0) prevIndex = presets.Count - 1;
        m_CurrentHeadPreset = presets[prevIndex];
        ApplyAppearance();
    }

    [ContextMenu("Next Upper Body")]
    public void NextUpperBodyPreset()
    {
        var presets = GetPresetsForGroup(PartGroup.UpperBody);
        if (presets.Count == 0) return;
        int currentIndex = presets.IndexOf(m_CurrentUpperBodyPreset);
        int nextIndex = (currentIndex + 1) % presets.Count;
        m_CurrentUpperBodyPreset = presets[nextIndex];
        ApplyAppearance();
    }

    [ContextMenu("Previous Upper Body")]
    public void PreviousUpperBodyPreset()
    {
        var presets = GetPresetsForGroup(PartGroup.UpperBody);
        if (presets.Count == 0) return;
        int currentIndex = presets.IndexOf(m_CurrentUpperBodyPreset);
        int prevIndex = currentIndex - 1;
        if (prevIndex < 0) prevIndex = presets.Count - 1;
        m_CurrentUpperBodyPreset = presets[prevIndex];
        ApplyAppearance();
    }

    [ContextMenu("Next Lower Body")]
    public void NextLowerBodyPreset()
    {
        var presets = GetPresetsForGroup(PartGroup.LowerBody);
        if (presets.Count == 0) return;
        int currentIndex = presets.IndexOf(m_CurrentLowerBodyPreset);
        int nextIndex = (currentIndex + 1) % presets.Count;
        m_CurrentLowerBodyPreset = presets[nextIndex];
        ApplyAppearance();
    }

    [ContextMenu("Previous Lower Body")]
    public void PreviousLowerBodyPreset()
    {
        var presets = GetPresetsForGroup(PartGroup.LowerBody);
        if (presets.Count == 0) return;
        int currentIndex = presets.IndexOf(m_CurrentLowerBodyPreset);
        int prevIndex = currentIndex - 1;
        if (prevIndex < 0) prevIndex = presets.Count - 1;
        m_CurrentLowerBodyPreset = presets[prevIndex];
        ApplyAppearance();
    }

    // Body shape
    public void SetBodyShape(float bodyType, float muscles, float bodySize)
    {
        m_BodyType = Mathf.Clamp(bodyType, -100f, 100f);
        m_Muscles = Mathf.Clamp(muscles, 0f, 100f);
        m_BodySize = Mathf.Clamp(bodySize, -100f, 100f);
        ApplyAppearance();
    }

    public float BodyType => m_BodyType;
    public float Muscles => m_Muscles;
    public float BodySize => m_BodySize;

    // Randomize
    [ContextMenu("Randomize Appearance")]
    public void Randomize()
    {
        Debug.Log("[SidekickPlayerController] Randomize called!");

        if (!Initialize())
        {
            Debug.LogError("[SidekickPlayerController] Failed to initialize in Randomize!");
            return;
        }

        var headPresets = GetPresetsForGroup(PartGroup.Head);
        var upperPresets = GetPresetsForGroup(PartGroup.UpperBody);
        var lowerPresets = GetPresetsForGroup(PartGroup.LowerBody);

        Debug.Log($"[SidekickPlayerController] Available presets - Head: {headPresets.Count}, Upper: {upperPresets.Count}, Lower: {lowerPresets.Count}");

        if (headPresets.Count > 0)
            m_CurrentHeadPreset = headPresets[UnityEngine.Random.Range(0, headPresets.Count)];

        if (upperPresets.Count > 0)
            m_CurrentUpperBodyPreset = upperPresets[UnityEngine.Random.Range(0, upperPresets.Count)];

        if (lowerPresets.Count > 0)
            m_CurrentLowerBodyPreset = lowerPresets[UnityEngine.Random.Range(0, lowerPresets.Count)];

        m_BodyType = UnityEngine.Random.Range(-50f, 50f);
        m_Muscles = UnityEngine.Random.Range(20f, 80f);
        m_BodySize = UnityEngine.Random.Range(-30f, 30f);

        ApplyAppearance();
    }

    // Legacy method for backwards compatibility
    public void SetBuildOnStart(bool value)
    {
        m_LoadSavedAppearance = value;
    }

    public void SetBuildFromScratch(bool value)
    {
        m_BuildFromScratch = value;
    }

    /// <summary>
    /// Enable/disable underwear-only mode. When enabled, upper/lower body are locked to underwear presets.
    /// </summary>
    public void SetUnderwearOnly(bool value)
    {
        m_UnderwearOnly = value;

        if (m_UnderwearOnly && IsInitialized)
        {
            // Find and apply underwear presets
            ApplyUnderwearPresets();
        }
    }

    /// <summary>
    /// Find and apply the "Base Body" preset for upper and lower body (underwear/naked look).
    /// </summary>
    private void ApplyUnderwearPresets()
    {
        if (!Initialize()) return;

        if (m_UseWebGLLoader)
        {
            // WebGL mode: Use WebGL preset collections
            var upperPresets = m_WebGLPresetsByGroup[PartGroup.UpperBody];
            var lowerPresets = m_WebGLPresetsByGroup[PartGroup.LowerBody];

            // Find the "Base Body" preset by name
            var baseUpper = upperPresets.FirstOrDefault(p => p.Name == BASE_BODY_PRESET);
            var baseLower = lowerPresets.FirstOrDefault(p => p.Name == BASE_BODY_PRESET);

            if (baseUpper != null)
            {
                m_CurrentWebGLUpperBodyPreset = baseUpper;
                if (m_DebugLog)
                {
                    Debug.Log($"[SidekickPlayerController] WebGL: Found Base Body upper preset");
                }
            }
            else if (m_DebugLog)
            {
                Debug.LogWarning($"[SidekickPlayerController] WebGL: Base Body upper preset not found! Using: {m_CurrentWebGLUpperBodyPreset?.Name}");
            }

            if (baseLower != null)
            {
                m_CurrentWebGLLowerBodyPreset = baseLower;
                if (m_DebugLog)
                {
                    Debug.Log($"[SidekickPlayerController] WebGL: Found Base Body lower preset");
                }
            }
            else if (m_DebugLog)
            {
                Debug.LogWarning($"[SidekickPlayerController] WebGL: Base Body lower preset not found! Using: {m_CurrentWebGLLowerBodyPreset?.Name}");
            }

            if (m_DebugLog)
            {
                Debug.Log($"[SidekickPlayerController] WebGL: Applied underwear presets - Upper: {m_CurrentWebGLUpperBodyPreset?.Name}, Lower: {m_CurrentWebGLLowerBodyPreset?.Name}");
            }
        }
        else
        {
            // Standard SQLite mode
            var upperPresets = GetPresetsForGroup(PartGroup.UpperBody);
            var lowerPresets = GetPresetsForGroup(PartGroup.LowerBody);

            // Find the "Base Body" preset by name
            var baseUpper = upperPresets.FirstOrDefault(p => p.Name == BASE_BODY_PRESET);
            var baseLower = lowerPresets.FirstOrDefault(p => p.Name == BASE_BODY_PRESET);

            if (baseUpper != null)
            {
                m_CurrentUpperBodyPreset = baseUpper;
                if (m_DebugLog)
                {
                    Debug.Log($"[SidekickPlayerController] Found Base Body upper preset");
                }
            }
            else if (m_DebugLog)
            {
                Debug.LogWarning($"[SidekickPlayerController] Base Body upper preset not found! Using: {m_CurrentUpperBodyPreset?.Name}");
            }

            if (baseLower != null)
            {
                m_CurrentLowerBodyPreset = baseLower;
                if (m_DebugLog)
                {
                    Debug.Log($"[SidekickPlayerController] Found Base Body lower preset");
                }
            }
            else if (m_DebugLog)
            {
                Debug.LogWarning($"[SidekickPlayerController] Base Body lower preset not found! Using: {m_CurrentLowerBodyPreset?.Name}");
            }

            if (m_DebugLog)
            {
                Debug.Log($"[SidekickPlayerController] Applied underwear presets - Upper: {m_CurrentUpperBodyPreset?.Name}, Lower: {m_CurrentLowerBodyPreset?.Name}");
            }
        }

        ApplyAppearance();
    }

    /// <summary>
    /// Randomize only the head and body shape (keeping clothes/underwear locked).
    /// Used for character creation where players customize face but not clothes.
    /// </summary>
    [ContextMenu("Randomize Head and Body Only")]
    public void RandomizeHeadAndBody()
    {
        if (!Initialize())
        {
            Debug.LogError("[SidekickPlayerController] Failed to initialize in RandomizeHeadAndBody!");
            return;
        }

        var headPresets = GetPresetsForGroup(PartGroup.Head);

        // Only randomize head
        if (headPresets.Count > 0)
        {
            m_CurrentHeadPreset = headPresets[UnityEngine.Random.Range(0, headPresets.Count)];
        }

        // Randomize body shape
        m_BodyType = UnityEngine.Random.Range(-50f, 50f);
        m_Muscles = UnityEngine.Random.Range(20f, 80f);
        m_BodySize = UnityEngine.Random.Range(-30f, 30f);

        // Keep underwear if in underwear mode
        if (m_UnderwearOnly)
        {
            ApplyUnderwearPresets();
        }
        else
        {
            ApplyAppearance();
        }

        if (m_DebugLog)
        {
            Debug.Log($"[SidekickPlayerController] Randomized head: {m_CurrentHeadPreset?.Name}, Body: {m_BodyType:F1}, {m_Muscles:F1}, {m_BodySize:F1}");
        }
    }

    public bool IsUnderwearOnly => m_UnderwearOnly;

    // Methods for clothing/equipment integration
    public void SetHeadPresetByName(string presetName)
    {
        if (m_UseWebGLLoader)
        {
            // WebGL mode: use WebGL presets
            if (m_WebGLPresetsByGroup != null && m_WebGLPresetsByGroup.TryGetValue(PartGroup.Head, out var webGLPresets))
            {
                var webGLPreset = webGLPresets.FirstOrDefault(p => p.Name == presetName);
                if (webGLPreset != null)
                {
                    m_CurrentWebGLHeadPreset = webGLPreset;
                    ApplyAppearance();
                }
            }
        }
        else
        {
            var presets = GetPresetsForGroup(PartGroup.Head);
            var preset = presets.FirstOrDefault(p => p.Name == presetName);
            if (preset != null)
            {
                m_CurrentHeadPreset = preset;
                ApplyAppearance();
            }
        }
    }

    public void SetUpperBodyPresetByName(string presetName)
    {
        Debug.Log($"[SidekickPlayerController] SetUpperBodyPresetByName called with: '{presetName}', WebGL mode: {m_UseWebGLLoader}");

        if (m_UseWebGLLoader)
        {
            // WebGL mode: use WebGL presets
            if (m_WebGLPresetsByGroup != null && m_WebGLPresetsByGroup.TryGetValue(PartGroup.UpperBody, out var webGLPresets))
            {
                Debug.Log($"[SidekickPlayerController] WebGL: Found {webGLPresets.Count} UpperBody presets");
                var webGLPreset = webGLPresets.FirstOrDefault(p => p.Name == presetName);
                if (webGLPreset != null)
                {
                    Debug.Log($"[SidekickPlayerController] WebGL: Found matching preset: {webGLPreset.Name}");
                    m_CurrentWebGLUpperBodyPreset = webGLPreset;
                    ApplyAppearance();
                }
                else
                {
                    Debug.LogWarning($"[SidekickPlayerController] WebGL: No UpperBody preset found matching '{presetName}'");
                    for (int i = 0; i < Mathf.Min(5, webGLPresets.Count); i++)
                    {
                        Debug.Log($"  Available: '{webGLPresets[i].Name}'");
                    }
                }
            }
        }
        else
        {
            var presets = GetPresetsForGroup(PartGroup.UpperBody);
            Debug.Log($"[SidekickPlayerController] Found {presets.Count} UpperBody presets");
            var preset = presets.FirstOrDefault(p => p.Name == presetName);
            if (preset != null)
            {
                Debug.Log($"[SidekickPlayerController] Found matching preset: {preset.Name}");
                m_CurrentUpperBodyPreset = preset;
                ApplyAppearance();
            }
            else
            {
                Debug.LogWarning($"[SidekickPlayerController] No UpperBody preset found matching '{presetName}'");
                for (int i = 0; i < Mathf.Min(5, presets.Count); i++)
                {
                    Debug.Log($"  Available: '{presets[i].Name}'");
                }
            }
        }
    }

    public void SetLowerBodyPresetByName(string presetName)
    {
        Debug.Log($"[SidekickPlayerController] SetLowerBodyPresetByName called with: '{presetName}', WebGL mode: {m_UseWebGLLoader}");

        if (m_UseWebGLLoader)
        {
            // WebGL mode: use WebGL presets
            if (m_WebGLPresetsByGroup != null && m_WebGLPresetsByGroup.TryGetValue(PartGroup.LowerBody, out var webGLPresets))
            {
                Debug.Log($"[SidekickPlayerController] WebGL: Found {webGLPresets.Count} LowerBody presets");
                var webGLPreset = webGLPresets.FirstOrDefault(p => p.Name == presetName);
                if (webGLPreset != null)
                {
                    Debug.Log($"[SidekickPlayerController] WebGL: Found matching preset: {webGLPreset.Name}");
                    m_CurrentWebGLLowerBodyPreset = webGLPreset;
                    ApplyAppearance();
                }
                else
                {
                    Debug.LogWarning($"[SidekickPlayerController] WebGL: No LowerBody preset found matching '{presetName}'");
                    for (int i = 0; i < Mathf.Min(5, webGLPresets.Count); i++)
                    {
                        Debug.Log($"  Available: '{webGLPresets[i].Name}'");
                    }
                }
            }
        }
        else
        {
            var presets = GetPresetsForGroup(PartGroup.LowerBody);
            Debug.Log($"[SidekickPlayerController] Found {presets.Count} LowerBody presets");
            var preset = presets.FirstOrDefault(p => p.Name == presetName);
            if (preset != null)
            {
                Debug.Log($"[SidekickPlayerController] Found matching LowerBody preset: {preset.Name}");
                m_CurrentLowerBodyPreset = preset;
                ApplyAppearance();
            }
            else
            {
                Debug.LogWarning($"[SidekickPlayerController] No LowerBody preset found matching '{presetName}'");
                for (int i = 0; i < Mathf.Min(5, presets.Count); i++)
                {
                    Debug.Log($"  Available LowerBody: '{presets[i].Name}'");
                }
            }
        }
    }

    #region Individual Part Selection

    /// <summary>
    /// Get all available parts for a specific part type.
    /// </summary>
    public List<SidekickPart> GetPartsForType(CharacterPartType partType)
    {
        // In WebGL mode, convert WebGLPart list to SidekickPart list
        if (m_UseWebGLLoader)
        {
            if (m_WebGLPartsByType != null && m_WebGLPartsByType.TryGetValue(partType, out var webGLParts))
            {
                // Convert WebGLPart to SidekickPart wrapper for compatibility
                // SidekickPart uses: ID, Name, Type (CharacterPartType), Location (file path)
                return webGLParts.Select(p => new SidekickPart
                {
                    ID = p.ID,
                    Name = p.Name,
                    Type = partType,
                    Location = p.FilePath
                }).ToList();
            }
            return new List<SidekickPart>();
        }

        // SQLite mode
        if (m_PartsByType != null && m_PartsByType.TryGetValue(partType, out var parts))
        {
            return parts;
        }
        return new List<SidekickPart>();
    }

    /// <summary>
    /// Get the current part for a specific type.
    /// </summary>
    public SidekickPart GetCurrentPart(CharacterPartType partType)
    {
        return partType switch
        {
            CharacterPartType.Hair => m_CurrentHair,
            CharacterPartType.EyebrowLeft or CharacterPartType.EyebrowRight => m_CurrentEyebrows,
            CharacterPartType.Nose => m_CurrentNose,
            CharacterPartType.EarLeft or CharacterPartType.EarRight => m_CurrentEars,
            CharacterPartType.FacialHair => m_CurrentFacialHair,
            _ => null
        };
    }

    /// <summary>
    /// Get the index of the current part within the available parts for that type.
    /// </summary>
    public int GetCurrentPartIndex(CharacterPartType partType)
    {
        var parts = GetPartsForType(partType);
        var current = GetCurrentPart(partType);
        if (current == null) return -1;
        return parts.FindIndex(p => p.Name == current.Name);
    }

    /// <summary>
    /// Set the current part by index within the available parts for that type.
    /// </summary>
    public void SetPartByIndex(CharacterPartType partType, int index, bool applyImmediately = true)
    {
        var parts = GetPartsForType(partType);
        if (index < 0 || index >= parts.Count) return;

        var part = parts[index];
        SetPartInternal(partType, part);

        if (applyImmediately)
        {
            ApplyAppearance();
        }
    }

    /// <summary>
    /// Set the current part by name.
    /// </summary>
    public void SetPartByName(CharacterPartType partType, string partName, bool applyImmediately = true)
    {
        var parts = GetPartsForType(partType);
        var part = parts.FirstOrDefault(p => p.Name == partName);
        if (part != null)
        {
            SetPartInternal(partType, part);
            if (applyImmediately)
            {
                ApplyAppearance();
            }
        }
    }

    /// <summary>
    /// Set a part to null (remove it, e.g., remove facial hair).
    /// </summary>
    public void ClearPart(CharacterPartType partType, bool applyImmediately = true)
    {
        SetPartInternal(partType, null);
        if (applyImmediately)
        {
            ApplyAppearance();
        }
    }

    private void SetPartInternal(CharacterPartType partType, SidekickPart part)
    {
        switch (partType)
        {
            case CharacterPartType.Hair:
                m_CurrentHair = part;
                break;
            case CharacterPartType.EyebrowLeft:
            case CharacterPartType.EyebrowRight:
                m_CurrentEyebrows = part;
                break;
            case CharacterPartType.Nose:
                m_CurrentNose = part;
                break;
            case CharacterPartType.EarLeft:
            case CharacterPartType.EarRight:
                m_CurrentEars = part;
                break;
            case CharacterPartType.FacialHair:
                m_CurrentFacialHair = part;
                break;
        }
    }

    /// <summary>
    /// Navigate to next part for a type.
    /// </summary>
    public void NextPart(CharacterPartType partType)
    {
        var parts = GetPartsForType(partType);
        if (parts.Count == 0) return;

        int currentIndex = GetCurrentPartIndex(partType);
        int nextIndex = (currentIndex + 1) % parts.Count;
        SetPartByIndex(partType, nextIndex);
    }

    /// <summary>
    /// Navigate to previous part for a type.
    /// </summary>
    public void PreviousPart(CharacterPartType partType)
    {
        var parts = GetPartsForType(partType);
        if (parts.Count == 0) return;

        int currentIndex = GetCurrentPartIndex(partType);
        int prevIndex = currentIndex - 1;
        if (prevIndex < 0) prevIndex = parts.Count - 1;
        SetPartByIndex(partType, prevIndex);
    }

    // Convenience properties
    public SidekickPart CurrentHair => m_CurrentHair;
    public SidekickPart CurrentEyebrows => m_CurrentEyebrows;
    public SidekickPart CurrentNose => m_CurrentNose;
    public SidekickPart CurrentEars => m_CurrentEars;
    public SidekickPart CurrentFacialHair => m_CurrentFacialHair;

    #endregion

    #region Color System

    /// <summary>
    /// Get all color properties available for customization.
    /// </summary>
    public List<SidekickColorProperty> GetColorProperties()
    {
        return m_ColorProperties ?? new List<SidekickColorProperty>();
    }

    /// <summary>
    /// Get a color property by name (e.g., "Skin", "Hair", "Eye").
    /// </summary>
    public SidekickColorProperty GetColorPropertyByName(string name)
    {
        return m_ColorProperties?.FirstOrDefault(p => p.Name.Contains(name));
    }

    /// <summary>
    /// Get the current color for a property.
    /// </summary>
    public Color GetCurrentColor(string propertyName)
    {
        if (m_CurrentColors != null && m_CurrentColors.TryGetValue(propertyName, out var colorRow))
        {
            return colorRow.NiceColor;
        }
        return Color.white;
    }

    /// <summary>
    /// Set color for a specific property.
    /// </summary>
    public void SetColor(string propertyName, Color color, bool applyImmediately = true)
    {
        var property = GetColorPropertyByName(propertyName);
        if (property == null)
        {
            // In WebGL mode, color properties aren't loaded from SQLite
            // Colors are stored in m_WebGL*Color fields instead (set by SetSkinColor, etc.)
            if (m_DebugLog && !m_UseWebGLLoader)
            {
                Debug.LogWarning($"[SidekickPlayerController] Color property '{propertyName}' not found");
            }
            return;
        }

        // Create or update color row
        if (m_CurrentColors == null)
        {
            m_CurrentColors = new Dictionary<string, SidekickColorRow>();
        }

        if (!m_CurrentColors.TryGetValue(propertyName, out var colorRow))
        {
            colorRow = new SidekickColorRow
            {
                ColorProperty = property
            };
            m_CurrentColors[propertyName] = colorRow;
        }

        colorRow.NiceColor = color;

        if (applyImmediately && m_Runtime != null)
        {
            m_Runtime.UpdateColor(ColorType.MainColor, colorRow);
        }

        if (m_DebugLog)
        {
            Debug.Log($"[SidekickPlayerController] Set {propertyName} color to {color}");
        }
    }

    /// <summary>
    /// Set skin color.
    /// </summary>
    public void SetSkinColor(Color color, bool applyImmediately = true)
    {
        m_WebGLSkinColor = color; // Always store for WebGL mode
        SetColor(COLOR_SKIN, color, applyImmediately);
    }

    /// <summary>
    /// Set hair color.
    /// </summary>
    public void SetHairColor(Color color, bool applyImmediately = true)
    {
        m_WebGLHairColor = color; // Always store for WebGL mode
        SetColor(COLOR_HAIR, color, applyImmediately);
    }

    /// <summary>
    /// Set eye color.
    /// </summary>
    public void SetEyeColor(Color color, bool applyImmediately = true)
    {
        m_WebGLEyeColor = color; // Always store for WebGL mode
        SetColor(COLOR_EYES, color, applyImmediately);
    }

    /// <summary>
    /// Apply all current colors to the character.
    /// </summary>
    public void ApplyColors()
    {
        if (m_Runtime == null || m_CurrentColors == null) return;

        foreach (var kvp in m_CurrentColors)
        {
            m_Runtime.UpdateColor(ColorType.MainColor, kvp.Value);
        }
    }

    /// <summary>
    /// Apply colors directly to a material (for WebGL mode).
    /// The Sidekick shader uses a color map texture where each color is at specific UV coordinates.
    /// </summary>
    private void ApplyColorsToMaterial(Material material)
    {
        if (material == null) return;

        // Get the color map texture from the material
        Texture2D originalColorMap = material.GetTexture("_ColorMap") as Texture2D;
        if (originalColorMap == null)
        {
            Debug.LogWarning("[SidekickPlayerController] No _ColorMap texture found on material - using fallback skin color");
            // Fallback: Just set the main color on the material
            material.SetColor("_Color", m_WebGLSkinColor);
            material.SetColor("_SkinColor", m_WebGLSkinColor);
            return;
        }

        Debug.Log($"[SidekickPlayerController] WebGL: Original color map size: {originalColorMap.width}x{originalColorMap.height}");

        // Create a writable copy of the color map (use GetPixels/SetPixels for WebGL compatibility)
        Texture2D colorMap = new Texture2D(originalColorMap.width, originalColorMap.height, TextureFormat.RGBA32, false);
        colorMap.filterMode = FilterMode.Point;

        // Try to copy pixels - this can fail in WebGL if texture isn't readable
        bool copySucceeded = false;
        try
        {
            // Method 1: Try Graphics.Blit + ReadPixels (works if texture is readable)
            RenderTexture rt = RenderTexture.GetTemporary(originalColorMap.width, originalColorMap.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(originalColorMap, rt);
            RenderTexture.active = rt;
            colorMap.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            colorMap.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            copySucceeded = true;
            Debug.Log("[SidekickPlayerController] WebGL: Color map copy via Blit/ReadPixels succeeded");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SidekickPlayerController] WebGL: Blit/ReadPixels failed: {e.Message}");
        }

        // Method 2: If Blit failed, try direct GetPixels (requires isReadable=true on texture)
        if (!copySucceeded)
        {
            try
            {
                Color[] pixels = originalColorMap.GetPixels();
                colorMap.SetPixels(pixels);
                colorMap.Apply();
                copySucceeded = true;
                Debug.Log("[SidekickPlayerController] WebGL: Color map copy via GetPixels succeeded");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SidekickPlayerController] WebGL: GetPixels failed: {e.Message}");
            }
        }

        // Method 3: If all else fails, create a new texture with default skin color
        if (!copySucceeded)
        {
            Debug.LogWarning("[SidekickPlayerController] WebGL: Creating fallback color map with skin color");
            Color[] fallbackPixels = new Color[colorMap.width * colorMap.height];
            for (int i = 0; i < fallbackPixels.Length; i++)
            {
                fallbackPixels[i] = m_WebGLSkinColor;
            }
            colorMap.SetPixels(fallbackPixels);
            colorMap.Apply();
        }

        // Get color properties from the exported data
        var presetData = SidekickPresetData.Instance;
        if (presetData == null)
        {
            Debug.LogWarning("[SidekickPlayerController] No SidekickPresetData found for color properties");
            return;
        }

        // FIRST: Fill ALL color map cells with skin color as a base
        // This ensures no default gray/blue colors show through
        FillAllColorCells(colorMap, m_WebGLSkinColor);

        // Apply skin colors (Skin 01, 02, 03)
        ApplyColorToTexture(colorMap, presetData, "Skin 01", m_WebGLSkinColor);
        ApplyColorToTexture(colorMap, presetData, "Skin 02", m_WebGLSkinColor);
        ApplyColorToTexture(colorMap, presetData, "Skin 03", m_WebGLSkinColor);

        // Apply other skin-related colors
        ApplyColorToTexture(colorMap, presetData, "Scar", m_WebGLSkinColor);
        ApplyColorToTexture(colorMap, presetData, "Lips", new Color(0.8f, 0.5f, 0.5f)); // Pinkish lips
        ApplyColorToTexture(colorMap, presetData, "Ear Left", m_WebGLSkinColor);
        ApplyColorToTexture(colorMap, presetData, "Ear Right", m_WebGLSkinColor);
        ApplyColorToTexture(colorMap, presetData, "Eyelids Left", m_WebGLSkinColor);
        ApplyColorToTexture(colorMap, presetData, "Eyelids Right", m_WebGLSkinColor);
        ApplyColorToTexture(colorMap, presetData, "Fingernails Left", new Color(0.9f, 0.8f, 0.75f));
        ApplyColorToTexture(colorMap, presetData, "Fingernails Right", new Color(0.9f, 0.8f, 0.75f));
        ApplyColorToTexture(colorMap, presetData, "Toenails Left", new Color(0.9f, 0.8f, 0.75f));
        ApplyColorToTexture(colorMap, presetData, "Toenails Right", new Color(0.9f, 0.8f, 0.75f));
        ApplyColorToTexture(colorMap, presetData, "Nose", m_WebGLSkinColor);
        ApplyColorToTexture(colorMap, presetData, "Mouth", new Color(0.7f, 0.4f, 0.4f));

        // Teeth and mouth
        ApplyColorToTexture(colorMap, presetData, "Teeth 01", Color.white);
        ApplyColorToTexture(colorMap, presetData, "Teeth 02", new Color(0.95f, 0.95f, 0.9f));
        ApplyColorToTexture(colorMap, presetData, "Gums", new Color(0.8f, 0.4f, 0.4f));
        ApplyColorToTexture(colorMap, presetData, "Tongue", new Color(0.85f, 0.5f, 0.5f));

        // Apply hair colors
        ApplyColorToTexture(colorMap, presetData, "Hair 01", m_WebGLHairColor);
        ApplyColorToTexture(colorMap, presetData, "Hair 02", m_WebGLHairColor);
        ApplyColorToTexture(colorMap, presetData, "Head Stubble", m_WebGLHairColor);

        // Apply eye colors (left and right)
        ApplyColorToTexture(colorMap, presetData, "Eye Outer Left", Color.white);
        ApplyColorToTexture(colorMap, presetData, "Eye Outer Right", Color.white);
        ApplyColorToTexture(colorMap, presetData, "Eye Edge Left", new Color(0.9f, 0.9f, 0.9f));
        ApplyColorToTexture(colorMap, presetData, "Eye Edge Right", new Color(0.9f, 0.9f, 0.9f));
        ApplyColorToTexture(colorMap, presetData, "Eye Inner Left", new Color(0.2f, 0.2f, 0.2f));
        ApplyColorToTexture(colorMap, presetData, "Eye Inner Right", new Color(0.2f, 0.2f, 0.2f));
        ApplyColorToTexture(colorMap, presetData, "Eye Color Left", m_WebGLEyeColor);
        ApplyColorToTexture(colorMap, presetData, "Eye Color Right", m_WebGLEyeColor);
        ApplyColorToTexture(colorMap, presetData, "Eye Highlight Left", Color.white);
        ApplyColorToTexture(colorMap, presetData, "Eye Highlight Right", Color.white);

        // Apply eyebrow colors (same as hair)
        ApplyColorToTexture(colorMap, presetData, "Eyebrow Left", m_WebGLHairColor);
        ApplyColorToTexture(colorMap, presetData, "Eyebrow Right", m_WebGLHairColor);

        // Apply facial hair colors (same as hair)
        ApplyColorToTexture(colorMap, presetData, "Facial Hair 01", m_WebGLHairColor);
        ApplyColorToTexture(colorMap, presetData, "Facial Hair 02", m_WebGLHairColor);
        ApplyColorToTexture(colorMap, presetData, "Facial Stubble", m_WebGLHairColor);

        // CRITICAL: Apply skin color to outfit slots for arms/legs (for underwear/base body)
        // These are the color slots used by the Base Body preset for exposed skin
        for (int i = 1; i <= 5; i++)
        {
            // Arms (skin color for bare arms)
            ApplyColorToTexture(colorMap, presetData, $"Arm Left Outfit 0{i}", m_WebGLSkinColor);
            ApplyColorToTexture(colorMap, presetData, $"Arm Right Outfit 0{i}", m_WebGLSkinColor);

            // Hands
            ApplyColorToTexture(colorMap, presetData, $"Hand Left Outfit 0{i}", m_WebGLSkinColor);
            ApplyColorToTexture(colorMap, presetData, $"Hand Right Outfit 0{i}", m_WebGLSkinColor);

            // Legs (skin color for bare legs/calves)
            ApplyColorToTexture(colorMap, presetData, $"Leg Left Outfit 0{i}", m_WebGLSkinColor);
            ApplyColorToTexture(colorMap, presetData, $"Leg Right Outfit 0{i}", m_WebGLSkinColor);

            // Feet
            ApplyColorToTexture(colorMap, presetData, $"Foot Left Outfit 0{i}", m_WebGLSkinColor);
            ApplyColorToTexture(colorMap, presetData, $"Foot Right Outfit 0{i}", m_WebGLSkinColor);

            // Torso and hips (for underwear)
            ApplyColorToTexture(colorMap, presetData, $"Torso Outfit 0{i}", m_WebGLSkinColor);
            ApplyColorToTexture(colorMap, presetData, $"Hips Outfit 0{i}", m_WebGLSkinColor);

            // Head and Face attachments (use skin color for consistency)
            ApplyColorToTexture(colorMap, presetData, $"Head Attachment 0{i}", m_WebGLSkinColor);
            ApplyColorToTexture(colorMap, presetData, $"Face Attachment 0{i}", m_WebGLSkinColor);

            // Back attachment (for backpacks, etc.)
            ApplyColorToTexture(colorMap, presetData, $"Back Attachment 0{i}", m_WebGLSkinColor);
        }

        // IMPORTANT: Flesh colors - these are used by the base body mesh for exposed skin
        ApplyColorToTexture(colorMap, presetData, "Flesh 01", m_WebGLSkinColor);
        ApplyColorToTexture(colorMap, presetData, "Flesh 02", m_WebGLSkinColor);

        // Accessory colors (hair clips, teeth decorations, etc.)
        ApplyColorToTexture(colorMap, presetData, "Hair Accessory", m_WebGLHairColor);
        ApplyColorToTexture(colorMap, presetData, "Facial Hair Accessory", m_WebGLHairColor);
        ApplyColorToTexture(colorMap, presetData, "Teeth Accessory", Color.white);

        // Horns (use skin color for human characters)
        ApplyColorToTexture(colorMap, presetData, "Horns 01", m_WebGLSkinColor);
        ApplyColorToTexture(colorMap, presetData, "Horns 02", m_WebGLSkinColor);

        // Fill ALL remaining color map cells with skin color as a fallback
        // This ensures any unmapped UV coordinates still get a reasonable color
        FillUnsetColorCells(colorMap, presetData, m_WebGLSkinColor);

        // Apply the modified texture to the material
        colorMap.Apply();
        material.SetTexture("_ColorMap", colorMap);

        if (m_DebugLog)
        {
            Debug.Log($"[SidekickPlayerController] Applied colors to color map texture: Skin={m_WebGLSkinColor}, Hair={m_WebGLHairColor}, Eye={m_WebGLEyeColor}");
        }
    }

    /// <summary>
    /// Apply a color to the color map texture at the UV coordinates for a specific color property.
    /// </summary>
    private void ApplyColorToTexture(Texture2D colorMap, SidekickPresetData presetData, string propertyName, Color color)
    {
        var colorProp = presetData.GetColorPropertyByName(propertyName);
        if (colorProp == null)
        {
            if (m_DebugLog)
            {
                Debug.LogWarning($"[SidekickPlayerController] Color property '{propertyName}' not found in exported data");
            }
            return;
        }

        // Each color cell is 2x2 pixels in the texture (same as SidekickRuntime.UpdateTexture)
        int scaledU = colorProp.u * 2;
        int scaledV = colorProp.v * 2;

        colorMap.SetPixel(scaledU, scaledV, color);
        colorMap.SetPixel(scaledU + 1, scaledV, color);
        colorMap.SetPixel(scaledU, scaledV + 1, color);
        colorMap.SetPixel(scaledU + 1, scaledV + 1, color);
    }

    /// <summary>
    /// Fill any remaining unset color cells in the color map with a fallback color.
    /// This ensures no gray/default colored patches appear on the character.
    /// </summary>
    private void FillUnsetColorCells(Texture2D colorMap, SidekickPresetData presetData, Color fallbackColor)
    {
        // The color map uses 2x2 pixel cells. Typical Sidekick color map is 32x32 pixels
        // which means 16x16 color cells (u and v range from 0 to 15)
        // We'll iterate through all cells and fill any that appear to be "default" (gray, blue, or similar)

        // Multiple default/unset colors to check for
        Color[] defaultColors = new Color[]
        {
            new Color(0.5f, 0.5f, 0.5f, 1f),      // Gray
            new Color(0.4f, 0.4f, 0.5f, 1f),      // Blue-gray
            new Color(0.3f, 0.3f, 0.4f, 1f),      // Dark blue-gray
            new Color(0.6f, 0.6f, 0.7f, 1f),      // Light blue-gray
            new Color(0.45f, 0.45f, 0.55f, 1f),   // Another blue-gray variant
        };
        float threshold = 0.15f; // Increased threshold to catch more variants

        // The color map area used by Sidekick is typically the lower-left portion
        // Based on the exported data, max UV seems to be around 12-13, so we'll cover 0-15
        for (int v = 0; v < 16; v++)
        {
            for (int u = 0; u < 16; u++)
            {
                int scaledU = u * 2;
                int scaledV = v * 2;

                // Check if this cell is within bounds
                if (scaledU + 1 >= colorMap.width || scaledV + 1 >= colorMap.height)
                    continue;

                // Sample the current color
                Color currentColor = colorMap.GetPixel(scaledU, scaledV);

                // Check if it matches any of the default colors
                bool isDefaultColor = false;
                foreach (var defaultColor in defaultColors)
                {
                    float diff = Mathf.Abs(currentColor.r - defaultColor.r) +
                                 Mathf.Abs(currentColor.g - defaultColor.g) +
                                 Mathf.Abs(currentColor.b - defaultColor.b);

                    if (diff < threshold)
                    {
                        isDefaultColor = true;
                        break;
                    }
                }

                // Also consider any color with low saturation and mid-range brightness as potentially unset
                float h, s, v_hsv;
                Color.RGBToHSV(currentColor, out h, out s, out v_hsv);
                if (s < 0.15f && v_hsv > 0.25f && v_hsv < 0.75f)
                {
                    isDefaultColor = true;
                }

                if (isDefaultColor)
                {
                    // This cell appears unset, fill with fallback color
                    colorMap.SetPixel(scaledU, scaledV, fallbackColor);
                    colorMap.SetPixel(scaledU + 1, scaledV, fallbackColor);
                    colorMap.SetPixel(scaledU, scaledV + 1, fallbackColor);
                    colorMap.SetPixel(scaledU + 1, scaledV + 1, fallbackColor);
                }
            }
        }
    }

    /// <summary>
    /// Fill ALL color cells in the color map with a base color.
    /// This is called BEFORE applying specific colors to ensure no default values remain.
    /// </summary>
    private void FillAllColorCells(Texture2D colorMap, Color baseColor)
    {
        // The color map uses 2x2 pixel cells. Typical Sidekick color map is 32x32 pixels
        // which means 16x16 color cells (u and v range from 0 to 15)
        for (int v = 0; v < 16; v++)
        {
            for (int u = 0; u < 16; u++)
            {
                int scaledU = u * 2;
                int scaledV = v * 2;

                // Check if this cell is within bounds
                if (scaledU + 1 >= colorMap.width || scaledV + 1 >= colorMap.height)
                    continue;

                // Fill this 2x2 cell with the base color
                colorMap.SetPixel(scaledU, scaledV, baseColor);
                colorMap.SetPixel(scaledU + 1, scaledV, baseColor);
                colorMap.SetPixel(scaledU, scaledV + 1, baseColor);
                colorMap.SetPixel(scaledU + 1, scaledV + 1, baseColor);
            }
        }

        if (m_DebugLog)
        {
            Debug.Log($"[SidekickPlayerController] Filled all color cells with base color: {baseColor}");
        }
    }

    #endregion

    #region Randomization

    /// <summary>
    /// Randomize all individual parts (hair, eyebrows, nose, ears, facial hair).
    /// </summary>
    public void RandomizeIndividualParts()
    {
        if (!Initialize()) return;

        // Random hair (include possibility of no hair if first slot is "bald")
        var hairParts = GetPartsForType(CharacterPartType.Hair);
        if (hairParts.Count > 0)
        {
            m_CurrentHair = hairParts[UnityEngine.Random.Range(0, hairParts.Count)];
        }

        // Random eyebrows
        var eyebrowParts = GetPartsForType(CharacterPartType.EyebrowLeft);
        if (eyebrowParts.Count > 0)
        {
            m_CurrentEyebrows = eyebrowParts[UnityEngine.Random.Range(0, eyebrowParts.Count)];
        }

        // Random nose
        var noseParts = GetPartsForType(CharacterPartType.Nose);
        if (noseParts.Count > 0)
        {
            m_CurrentNose = noseParts[UnityEngine.Random.Range(0, noseParts.Count)];
        }

        // Random ears
        var earParts = GetPartsForType(CharacterPartType.EarLeft);
        if (earParts.Count > 0)
        {
            m_CurrentEars = earParts[UnityEngine.Random.Range(0, earParts.Count)];
        }

        // Random facial hair (50% chance of none)
        var facialHairParts = GetPartsForType(CharacterPartType.FacialHair);
        if (facialHairParts.Count > 0 && UnityEngine.Random.value > 0.5f)
        {
            m_CurrentFacialHair = facialHairParts[UnityEngine.Random.Range(0, facialHairParts.Count)];
        }
        else
        {
            m_CurrentFacialHair = null;
        }
    }

    /// <summary>
    /// Randomize all customizable features including body shape, parts, and colors.
    /// </summary>
    public void RandomizeAll()
    {
        if (!Initialize()) return;

        // Randomize body shape
        m_BodyType = UnityEngine.Random.Range(-50f, 50f);
        m_Muscles = UnityEngine.Random.Range(20f, 80f);
        m_BodySize = UnityEngine.Random.Range(-30f, 30f);

        // Randomize individual parts
        RandomizeIndividualParts();

        // Randomize colors (works in both SQLite and WebGL modes)
        // These methods update both m_CurrentColors (SQLite) and m_WebGL*Color fields
        SetSkinColor(GetRandomSkinColor(), false);
        SetHairColor(GetRandomHairColor(), false);
        SetEyeColor(GetRandomEyeColor(), false);

        ApplyAppearance();
    }

    private Color GetRandomSkinColor()
    {
        // Natural skin tones from light to dark
        Color[] skinTones = new Color[]
        {
            new Color(1f, 0.87f, 0.77f),      // Light
            new Color(0.96f, 0.80f, 0.69f),   // Fair
            new Color(0.87f, 0.72f, 0.53f),   // Medium
            new Color(0.76f, 0.57f, 0.42f),   // Olive
            new Color(0.55f, 0.38f, 0.28f),   // Brown
            new Color(0.36f, 0.25f, 0.20f)    // Dark
        };
        return skinTones[UnityEngine.Random.Range(0, skinTones.Length)];
    }

    private Color GetRandomHairColor()
    {
        Color[] hairColors = new Color[]
        {
            new Color(0.1f, 0.05f, 0.02f),    // Black
            new Color(0.25f, 0.15f, 0.08f),   // Dark brown
            new Color(0.45f, 0.30f, 0.15f),   // Brown
            new Color(0.65f, 0.45f, 0.25f),   // Light brown
            new Color(0.85f, 0.65f, 0.35f),   // Blonde
            new Color(0.95f, 0.85f, 0.55f),   // Light blonde
            new Color(0.55f, 0.15f, 0.08f),   // Auburn
            new Color(0.75f, 0.25f, 0.10f),   // Red
            new Color(0.5f, 0.5f, 0.5f)       // Gray
        };
        return hairColors[UnityEngine.Random.Range(0, hairColors.Length)];
    }

    private Color GetRandomEyeColor()
    {
        Color[] eyeColors = new Color[]
        {
            new Color(0.25f, 0.15f, 0.08f),   // Brown
            new Color(0.45f, 0.30f, 0.15f),   // Light brown / Hazel
            new Color(0.35f, 0.55f, 0.35f),   // Green
            new Color(0.30f, 0.45f, 0.65f),   // Blue
            new Color(0.50f, 0.55f, 0.65f),   // Gray-blue
            new Color(0.20f, 0.20f, 0.25f)    // Dark
        };
        return eyeColors[UnityEngine.Random.Range(0, eyeColors.Length)];
    }

    #endregion
}

/// <summary>
/// Serializable data for saving/loading character appearance.
/// Includes presets, individual parts, body shape, and colors.
/// </summary>
[System.Serializable]
public class CharacterSaveData
{
    // Preset names (for clothing/body)
    public string headPresetName;
    public string upperBodyPresetName;
    public string lowerBodyPresetName;

    // Body shape
    public float bodyType;
    public float muscles;
    public float bodySize;

    // Individual part names (for face customization)
    public string hairPartName;
    public string eyebrowsPartName;
    public string nosePartName;
    public string earsPartName;
    public string facialHairPartName;

    // Colors (stored as hex strings for serialization)
    public string skinColorHex;
    public string hairColorHex;
    public string eyeColorHex;
}

/// <summary>
/// Helper class for color serialization.
/// </summary>
public static class ColorSaveHelper
{
    public static string ToHex(Color color)
    {
        return ColorUtility.ToHtmlStringRGBA(color);
    }

    public static Color FromHex(string hex, Color defaultColor)
    {
        if (string.IsNullOrEmpty(hex)) return defaultColor;
        if (ColorUtility.TryParseHtmlString("#" + hex, out Color color))
        {
            return color;
        }
        return defaultColor;
    }
}
