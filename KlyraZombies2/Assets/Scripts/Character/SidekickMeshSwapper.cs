using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Synty.SidekickCharacters.API;
using Synty.SidekickCharacters.Database;
using Synty.SidekickCharacters.Database.DTO;
using Synty.SidekickCharacters.Enums;
using Synty.SidekickCharacters.Utils;

/// <summary>
/// Swaps the mesh on an existing character with Sidekick character meshes.
/// This works by REBINDING Sidekick meshes to use the ORIGINAL skeleton,
/// preserving all Opsive configuration, holsters, animation settings, etc.
///
/// Key insight: We don't create a new skeleton - we just swap the visual meshes
/// and rebind them to the existing bones that Opsive already controls.
/// </summary>
public class SidekickMeshSwapper : MonoBehaviour
{
    [Header("Original Character")]
    [Tooltip("Root transform containing the original character meshes to hide")]
    [SerializeField] private Transform m_OriginalModelRoot;

    [Header("Sidekick Resources")]
    [Tooltip("Leave empty to load from Resources")]
    [SerializeField] private GameObject m_BaseModelOverride;
    [SerializeField] private Material m_BaseMaterialOverride;

    [Header("Character Save")]
    [SerializeField] private string m_SaveKey = "PlayerCharacter";

    [Header("Debug")]
    [SerializeField] private bool m_SwapOnStart = true;
    [SerializeField] private bool m_DebugLog = true;

    // Sidekick
    private SidekickRuntime m_Runtime;
    private DatabaseManager m_DbManager;
    private Dictionary<CharacterPartType, Dictionary<string, SidekickPart>> m_PartLibrary;
    private Dictionary<PartGroup, List<SidekickPartPreset>> m_PresetsByGroup;

    private List<SkinnedMeshRenderer> m_CreatedMeshes = new List<SkinnedMeshRenderer>();
    private Transform m_OriginalRootBone;
    private Dictionary<string, Transform> m_OriginalBoneMap = new Dictionary<string, Transform>();

    public List<SkinnedMeshRenderer> SidekickMeshes => m_CreatedMeshes;

    private void Start()
    {
        // Use Start instead of Awake so Opsive components are initialized first
        if (m_SwapOnStart)
        {
            SwapToSidekick();
        }
    }

    /// <summary>
    /// Initialize the Sidekick runtime.
    /// </summary>
    private bool Initialize()
    {
        if (m_Runtime != null) return true;

        // Load base model
        GameObject baseModel = m_BaseModelOverride;
        if (baseModel == null)
        {
            baseModel = Resources.Load<GameObject>("Meshes/SK_BaseModel");
        }

        if (baseModel == null)
        {
            Debug.LogError("[SidekickMeshSwapper] Could not load SK_BaseModel!");
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
            Debug.LogError("[SidekickMeshSwapper] Could not load M_BaseMaterial!");
            return false;
        }

        // Check if we should use WebGL mode (skip SQLite)
        if (SidekickWebGLLoader.ShouldUseWebGLLoader)
        {
            // WebGL mode: Don't initialize database or runtime
            m_DbManager = null;
            m_Runtime = null;
            m_PartLibrary = null;
            m_PresetsByGroup = new Dictionary<PartGroup, List<SidekickPartPreset>>();

            if (m_DebugLog)
            {
                Debug.Log("[SidekickMeshSwapper] WebGL mode: Skipping SQLite initialization");
            }

            return true;
        }

        // Standard mode: Initialize with database
        m_DbManager = new DatabaseManager();
        m_Runtime = new SidekickRuntime(baseModel, baseMaterial, null, m_DbManager);
        _ = SidekickRuntime.PopulateToolData(m_Runtime);
        m_PartLibrary = m_Runtime.MappedPartDictionary;

        // Load presets
        m_PresetsByGroup = new Dictionary<PartGroup, List<SidekickPartPreset>>();
        foreach (PartGroup group in System.Enum.GetValues(typeof(PartGroup)))
        {
            m_PresetsByGroup[group] = SidekickPartPreset.GetAllByGroup(m_DbManager, group)
                .Where(p => p.HasAllPartsAvailable(m_DbManager))
                .ToList();
        }

        if (m_DebugLog)
        {
            Debug.Log($"[SidekickMeshSwapper] Initialized with {m_Runtime.PartCount} parts");
        }

        return true;
    }

    /// <summary>
    /// Find the original model root if not assigned.
    /// </summary>
    private void FindOriginalModelRoot()
    {
        if (m_OriginalModelRoot != null) return;

        // Look for common character model names
        string[] modelPatterns = { "SM_Chr_", "Character", "Model" };

        foreach (Transform child in transform)
        {
            foreach (var pattern in modelPatterns)
            {
                if (child.name.Contains(pattern))
                {
                    // Check if it has skinned mesh renderers (actual model, not just container)
                    if (child.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                    {
                        m_OriginalModelRoot = child;
                        if (m_DebugLog)
                        {
                            Debug.Log($"[SidekickMeshSwapper] Auto-found original model root: {child.name}");
                        }
                        return;
                    }
                }
            }
        }

        // If no model found, look in Models container
        var modelsContainer = transform.Find("Models");
        if (modelsContainer != null)
        {
            foreach (Transform child in modelsContainer)
            {
                if (child.GetComponentInChildren<SkinnedMeshRenderer>() != null)
                {
                    m_OriginalModelRoot = child;
                    if (m_DebugLog)
                    {
                        Debug.Log($"[SidekickMeshSwapper] Found model in Models container: {child.name}");
                    }
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Build a map of all bones in the original skeleton.
    /// </summary>
    private void BuildBoneMap()
    {
        m_OriginalBoneMap.Clear();

        if (m_OriginalModelRoot == null)
        {
            Debug.LogError("[SidekickMeshSwapper] No original model root found!");
            return;
        }

        // Find the root bone (usually "Root" or first bone in hierarchy)
        var originalMesh = m_OriginalModelRoot.GetComponentInChildren<SkinnedMeshRenderer>();
        if (originalMesh != null && originalMesh.rootBone != null)
        {
            m_OriginalRootBone = originalMesh.rootBone;
        }
        else
        {
            // Try to find Root bone manually
            m_OriginalRootBone = FindChildRecursive(m_OriginalModelRoot, "Root");
            if (m_OriginalRootBone == null)
            {
                m_OriginalRootBone = FindChildRecursive(m_OriginalModelRoot, "Hips");
            }
        }

        if (m_OriginalRootBone == null)
        {
            Debug.LogError("[SidekickMeshSwapper] Could not find root bone in original model!");
            return;
        }

        // Build map of all bones
        BuildBoneMapRecursive(m_OriginalRootBone);

        if (m_DebugLog)
        {
            Debug.Log($"[SidekickMeshSwapper] Built bone map with {m_OriginalBoneMap.Count} bones. Root: {m_OriginalRootBone.name}");
        }
    }

    private void BuildBoneMapRecursive(Transform bone)
    {
        // Store by exact name
        if (!m_OriginalBoneMap.ContainsKey(bone.name))
        {
            m_OriginalBoneMap[bone.name] = bone;
        }

        // Also store normalized name (lowercase, no underscores)
        string normalized = NormalizeBoneName(bone.name);
        if (!m_OriginalBoneMap.ContainsKey(normalized))
        {
            m_OriginalBoneMap[normalized] = bone;
        }

        foreach (Transform child in bone)
        {
            BuildBoneMapRecursive(child);
        }
    }

    private string NormalizeBoneName(string name)
    {
        return name.ToLower().Replace("_", "").Replace(" ", "").Replace(".", "");
    }

    /// <summary>
    /// Swap the original mesh with Sidekick character.
    /// </summary>
    public void SwapToSidekick()
    {
        if (!Initialize())
        {
            Debug.LogError("[SidekickMeshSwapper] Failed to initialize!");
            return;
        }

        // Find original model if not assigned
        FindOriginalModelRoot();

        if (m_OriginalModelRoot == null)
        {
            Debug.LogError("[SidekickMeshSwapper] Could not find original model root! Please assign it in the inspector.");
            return;
        }

        // Build bone map from original skeleton
        BuildBoneMap();

        if (m_OriginalRootBone == null)
        {
            Debug.LogError("[SidekickMeshSwapper] Could not find skeleton bones!");
            return;
        }

        // Load saved character data
        CharacterSaveData saveData = LoadCharacterData();

        // Find presets
        SidekickPartPreset headPreset = FindPreset(PartGroup.Head, saveData?.headPresetName);
        SidekickPartPreset upperPreset = FindPreset(PartGroup.UpperBody, saveData?.upperBodyPresetName);
        SidekickPartPreset lowerPreset = FindPreset(PartGroup.LowerBody, saveData?.lowerBodyPresetName);

        // Collect parts
        List<SkinnedMeshRenderer> partsToUse = new List<SkinnedMeshRenderer>();
        AddPartsFromPreset(headPreset, partsToUse);
        AddPartsFromPreset(upperPreset, partsToUse);
        AddPartsFromPreset(lowerPreset, partsToUse);

        if (partsToUse.Count == 0)
        {
            Debug.LogError("[SidekickMeshSwapper] No parts found for character!");
            return;
        }

        // Set body shape
        if (saveData != null)
        {
            m_Runtime.BodyTypeBlendValue = saveData.bodyType;
            m_Runtime.MusclesBlendValue = saveData.muscles;
            m_Runtime.BodySizeHeavyBlendValue = saveData.bodySize > 0 ? saveData.bodySize : 0;
            m_Runtime.BodySizeSkinnyBlendValue = saveData.bodySize < 0 ? -saveData.bodySize : 0;
        }

        // HIDE original meshes (but keep skeleton!)
        HideOriginalMeshes();

        // Create Sidekick meshes and rebind to original skeleton
        CreateAndRebindMeshes(partsToUse);

        if (m_DebugLog)
        {
            Debug.Log($"[SidekickMeshSwapper] Swapped to Sidekick with {m_CreatedMeshes.Count} meshes");
        }
    }

    private void HideOriginalMeshes()
    {
        if (m_OriginalModelRoot == null) return;

        // Hide all renderers in original model but KEEP the skeleton transforms active
        var skinnedRenderers = m_OriginalModelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var renderer in skinnedRenderers)
        {
            renderer.enabled = false;
            if (m_DebugLog)
            {
                Debug.Log($"[SidekickMeshSwapper] Disabled renderer: {renderer.name}");
            }
        }

        var meshRenderers = m_OriginalModelRoot.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var renderer in meshRenderers)
        {
            renderer.enabled = false;
        }
    }

    private void CreateAndRebindMeshes(List<SkinnedMeshRenderer> sidekickParts)
    {
        m_CreatedMeshes.Clear();

        // Create a container for Sidekick meshes next to original model
        GameObject sidekickContainer = new GameObject("SidekickMeshes");
        sidekickContainer.transform.SetParent(m_OriginalModelRoot.parent ?? transform);
        sidekickContainer.transform.localPosition = Vector3.zero;
        sidekickContainer.transform.localRotation = Quaternion.identity;
        sidekickContainer.transform.localScale = Vector3.one;

        foreach (var sourceMesh in sidekickParts)
        {
            // Create new GameObject for this mesh part
            GameObject meshObj = new GameObject(sourceMesh.name + "_Rebound");
            meshObj.transform.SetParent(sidekickContainer.transform);
            meshObj.transform.localPosition = Vector3.zero;
            meshObj.transform.localRotation = Quaternion.identity;
            meshObj.transform.localScale = Vector3.one;

            // Create new SkinnedMeshRenderer
            SkinnedMeshRenderer newRenderer = meshObj.AddComponent<SkinnedMeshRenderer>();

            // Copy mesh data
            newRenderer.sharedMesh = sourceMesh.sharedMesh;
            newRenderer.sharedMaterials = sourceMesh.sharedMaterials;

            // REBIND to original skeleton
            RebindToOriginalSkeleton(newRenderer, sourceMesh);

            m_CreatedMeshes.Add(newRenderer);
        }

        if (m_DebugLog)
        {
            Debug.Log($"[SidekickMeshSwapper] Created {m_CreatedMeshes.Count} rebound meshes");
        }
    }

    private void RebindToOriginalSkeleton(SkinnedMeshRenderer newRenderer, SkinnedMeshRenderer sourceMesh)
    {
        // Get original bone array from source mesh
        Transform[] sourceBones = sourceMesh.bones;
        Transform[] newBones = new Transform[sourceBones.Length];

        int matchedBones = 0;
        int unmatchedBones = 0;

        for (int i = 0; i < sourceBones.Length; i++)
        {
            if (sourceBones[i] == null)
            {
                newBones[i] = null;
                continue;
            }

            string boneName = sourceBones[i].name;
            Transform matchedBone = FindMatchingBone(boneName);

            if (matchedBone != null)
            {
                newBones[i] = matchedBone;
                matchedBones++;
            }
            else
            {
                // Fallback to root bone if no match found
                newBones[i] = m_OriginalRootBone;
                unmatchedBones++;

                if (m_DebugLog)
                {
                    Debug.LogWarning($"[SidekickMeshSwapper] Could not find bone '{boneName}', using root bone");
                }
            }
        }

        // Set the bones array
        newRenderer.bones = newBones;
        newRenderer.rootBone = m_OriginalRootBone;

        // Copy bounds
        newRenderer.localBounds = sourceMesh.localBounds;

        if (m_DebugLog)
        {
            Debug.Log($"[SidekickMeshSwapper] Rebound mesh {sourceMesh.name}: {matchedBones} matched, {unmatchedBones} unmatched bones");
        }
    }

    // Complete mapping from Sidekick bone names to Polygon Apocalypse bone names
    private static readonly Dictionary<string, string> SidekickToPolygonBoneMap = new Dictionary<string, string>
    {
        // Root/Core
        { "root", "Root" },
        { "pelvis", "Hips" },

        // Spine
        { "spine_01", "Spine_01" },
        { "spine_02", "Spine_02" },
        { "spine_03", "Spine_03" },
        { "neck_01", "Neck" },
        { "head", "Head" },

        // Left Leg
        { "thigh_l", "UpperLeg_L" },
        { "thigh_twist_01_l", "UpperLeg_L" }, // No twist bone in Polygon, use parent
        { "calf_l", "LowerLeg_L" },
        { "calf_twist_01_l", "LowerLeg_L" },
        { "foot_l", "Ankle_L" },
        { "ball_l", "Ball_L" },

        // Right Leg
        { "thigh_r", "UpperLeg_R" },
        { "thigh_twist_01_r", "UpperLeg_R" },
        { "calf_r", "LowerLeg_R" },
        { "calf_twist_01_r", "LowerLeg_R" },
        { "foot_r", "Ankle_R" },
        { "ball_r", "Ball_R" },

        // Left Arm
        { "clavicle_l", "Clavicle_L" },
        { "upperarm_l", "Shoulder_L" },
        { "upperarm_twist_01_l", "Shoulder_L" },
        { "lowerarm_l", "Elbow_L" },
        { "lowerarm_twist_01_l", "Elbow_L" },
        { "hand_l", "Hand_L" },

        // Right Arm
        { "clavicle_r", "Clavicle_R" },
        { "upperarm_r", "Shoulder_R" },
        { "upperarm_twist_01_r", "Shoulder_R" },
        { "lowerarm_r", "Elbow_R" },
        { "lowerarm_twist_01_r", "Elbow_R" },
        { "hand_r", "Hand_R" },

        // Left Hand Fingers
        { "thumb_01_l", "Thumb_01_L" },
        { "thumb_02_l", "Thumb_02_L" },
        { "thumb_03_l", "Thumb_03_L" },
        { "index_01_l", "IndexFinger_01_L" },
        { "index_02_l", "IndexFinger_02_L" },
        { "index_03_l", "IndexFinger_03_L" },
        { "middle_01_l", "MiddleFinger_01_L" },
        { "middle_02_l", "MiddleFinger_02_L" },
        { "middle_03_l", "MiddleFinger_03_L" },
        { "ring_01_l", "RingFinger_01_L" },
        { "ring_02_l", "RingFinger_02_L" },
        { "ring_03_l", "RingFinger_03_L" },
        { "pinky_01_l", "PinkyFinger_01_L" },
        { "pinky_02_l", "PinkyFinger_02_L" },
        { "pinky_03_l", "PinkyFinger_03_L" },

        // Right Hand Fingers
        { "thumb_01_r", "Thumb_01_R" },
        { "thumb_02_r", "Thumb_02_R" },
        { "thumb_03_r", "Thumb_03_R" },
        { "index_01_r", "IndexFinger_01_R" },
        { "index_02_r", "IndexFinger_02_R" },
        { "index_03_r", "IndexFinger_03_R" },
        { "middle_01_r", "MiddleFinger_01_R" },
        { "middle_02_r", "MiddleFinger_02_R" },
        { "middle_03_r", "MiddleFinger_03_R" },
        { "ring_01_r", "RingFinger_01_R" },
        { "ring_02_r", "RingFinger_02_R" },
        { "ring_03_r", "RingFinger_03_R" },
        { "pinky_01_r", "PinkyFinger_01_R" },
        { "pinky_02_r", "PinkyFinger_02_R" },
        { "pinky_03_r", "PinkyFinger_03_R" },
    };

    private Transform FindMatchingBone(string sidekickBoneName)
    {
        // First, try the explicit Sidekick -> Polygon mapping
        string lowerName = sidekickBoneName.ToLower();
        if (SidekickToPolygonBoneMap.TryGetValue(lowerName, out string polygonBoneName))
        {
            if (m_OriginalBoneMap.TryGetValue(polygonBoneName, out Transform mapped))
            {
                return mapped;
            }
            // Also try normalized version
            if (m_OriginalBoneMap.TryGetValue(NormalizeBoneName(polygonBoneName), out Transform mappedNorm))
            {
                return mappedNorm;
            }
        }

        // Try exact match
        if (m_OriginalBoneMap.TryGetValue(sidekickBoneName, out Transform exact))
        {
            return exact;
        }

        // Try normalized name
        string normalized = NormalizeBoneName(sidekickBoneName);
        if (m_OriginalBoneMap.TryGetValue(normalized, out Transform normalizedMatch))
        {
            return normalizedMatch;
        }

        // Skip IK and attachment bones - they don't exist in Polygon skeleton
        if (lowerName.StartsWith("ik_") || lowerName.Contains("attach"))
        {
            return null; // Will use root bone as fallback
        }

        return null;
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name || child.name.Contains(name))
            {
                return child;
            }
            var found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private CharacterSaveData LoadCharacterData()
    {
        string json = PlayerPrefs.GetString(m_SaveKey, "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                return JsonUtility.FromJson<CharacterSaveData>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SidekickMeshSwapper] Failed to load character data: {e.Message}");
            }
        }
        return null;
    }

    private SidekickPartPreset FindPreset(PartGroup group, string presetName)
    {
        if (!m_PresetsByGroup.ContainsKey(group) || m_PresetsByGroup[group].Count == 0)
            return null;

        var presets = m_PresetsByGroup[group];

        // Try to find by name
        if (!string.IsNullOrEmpty(presetName))
        {
            var preset = presets.FirstOrDefault(p => p.Name == presetName);
            if (preset != null) return preset;
        }

        // Return first available
        return presets.FirstOrDefault();
    }

    private void AddPartsFromPreset(SidekickPartPreset preset, List<SkinnedMeshRenderer> partsList)
    {
        if (preset == null) return;

        var rows = SidekickPartPresetRow.GetAllByPreset(m_DbManager, preset);
        foreach (var row in rows)
        {
            if (string.IsNullOrEmpty(row.PartName)) continue;

            try
            {
                CharacterPartType type = System.Enum.Parse<CharacterPartType>(
                    CharacterPartTypeUtils.GetTypeNameFromShortcode(row.PartType));

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
            catch (System.Exception e)
            {
                if (m_DebugLog)
                {
                    Debug.LogWarning($"[SidekickMeshSwapper] Failed to load part {row.PartName}: {e.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Show the original meshes and remove Sidekick meshes (revert swap).
    /// </summary>
    public void RevertToOriginal()
    {
        // Re-enable original meshes
        if (m_OriginalModelRoot != null)
        {
            var skinnedRenderers = m_OriginalModelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var renderer in skinnedRenderers)
            {
                renderer.enabled = true;
            }

            var meshRenderers = m_OriginalModelRoot.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var renderer in meshRenderers)
            {
                renderer.enabled = true;
            }
        }

        // Destroy Sidekick meshes
        foreach (var mesh in m_CreatedMeshes)
        {
            if (mesh != null)
            {
                Destroy(mesh.gameObject);
            }
        }
        m_CreatedMeshes.Clear();

        // Destroy container
        var container = transform.Find("SidekickMeshes");
        if (container != null)
        {
            Destroy(container.gameObject);
        }
    }

    /// <summary>
    /// Update character appearance with new presets.
    /// </summary>
    public void UpdateAppearance(string headPreset, string upperPreset, string lowerPreset)
    {
        // Save the new preset names
        CharacterSaveData saveData = LoadCharacterData() ?? new CharacterSaveData();
        saveData.headPresetName = headPreset;
        saveData.upperBodyPresetName = upperPreset;
        saveData.lowerBodyPresetName = lowerPreset;

        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(m_SaveKey, json);
        PlayerPrefs.Save();

        // Revert and re-swap
        RevertToOriginal();
        SwapToSidekick();
    }
}
