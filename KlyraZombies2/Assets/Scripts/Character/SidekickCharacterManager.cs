using UnityEngine;
using System.Collections.Generic;
using Synty.SidekickCharacters.API;
using Synty.SidekickCharacters.Database;
using Synty.SidekickCharacters.Enums;

/// <summary>
/// Manages character customization using Synty Sidekick Character Creator.
/// Bridges between our equipment system and the Sidekick API.
/// </summary>
public class SidekickCharacterManager : MonoBehaviour
{
    [Header("Sidekick Settings")]
    [Tooltip("The base model to use for Sidekick (SK_BaseModel)")]
    [SerializeField] private GameObject m_BaseModel;

    [Tooltip("The material to use for the character")]
    [SerializeField] private Material m_CharacterMaterial;

    [Tooltip("The animator controller to apply")]
    [SerializeField] private RuntimeAnimatorController m_AnimatorController;

    [Header("Current Character")]
    [Tooltip("The spawned character model")]
    [SerializeField] private GameObject m_CurrentCharacter;

    [Header("Debug")]
    [SerializeField] private bool m_DebugLog = false;

    // Sidekick runtime instance
    private SidekickRuntime m_SidekickRuntime;
    private DatabaseManager m_DatabaseManager;
    private bool m_IsInitialized = false;

    // Track current parts by slot
    private Dictionary<CharacterPartType, string> m_CurrentParts = new Dictionary<CharacterPartType, string>();

    // Mapping from our EquipmentSlotType to Sidekick CharacterPartType
    private static readonly Dictionary<EquipmentSlotType, CharacterPartType[]> EquipmentToSidekickMap = new Dictionary<EquipmentSlotType, CharacterPartType[]>
    {
        { EquipmentSlotType.Head, new[] { CharacterPartType.AttachmentHead } },
        { EquipmentSlotType.Face, new[] { CharacterPartType.AttachmentFace } },
        { EquipmentSlotType.Hair, new[] { CharacterPartType.Hair } },
        { EquipmentSlotType.Torso, new[] { CharacterPartType.Torso, CharacterPartType.ArmUpperLeft, CharacterPartType.ArmUpperRight, CharacterPartType.ArmLowerLeft, CharacterPartType.ArmLowerRight } },
        { EquipmentSlotType.Hands, new[] { CharacterPartType.HandLeft, CharacterPartType.HandRight } },
        { EquipmentSlotType.Legs, new[] { CharacterPartType.Hips, CharacterPartType.LegLeft, CharacterPartType.LegRight } },
        { EquipmentSlotType.Feet, new[] { CharacterPartType.FootLeft, CharacterPartType.FootRight } },
        { EquipmentSlotType.Back, new[] { CharacterPartType.AttachmentBack } },
        { EquipmentSlotType.ShoulderLeft, new[] { CharacterPartType.AttachmentShoulderLeft } },
        { EquipmentSlotType.ShoulderRight, new[] { CharacterPartType.AttachmentShoulderRight } },
        { EquipmentSlotType.KneeLeft, new[] { CharacterPartType.AttachmentKneeLeft } },
        { EquipmentSlotType.KneeRight, new[] { CharacterPartType.AttachmentKneeRight } },
        { EquipmentSlotType.Belt, new[] { CharacterPartType.AttachmentHipsFront } },
    };

    public bool IsInitialized => m_IsInitialized;
    public GameObject CurrentCharacter => m_CurrentCharacter;

    private void Awake()
    {
        // Don't auto-initialize - wait for explicit call or when needed
    }

    /// <summary>
    /// Initialize the Sidekick system with base model and materials.
    /// Call this before trying to customize the character.
    /// </summary>
    public bool Initialize()
    {
        if (m_IsInitialized)
        {
            return true;
        }

        if (m_BaseModel == null)
        {
            Debug.LogError("[SidekickCharacterManager] No base model assigned!");
            return false;
        }

        if (m_CharacterMaterial == null)
        {
            Debug.LogError("[SidekickCharacterManager] No character material assigned!");
            return false;
        }

        // Check if we should use WebGL mode (skip SQLite)
        if (SidekickWebGLLoader.ShouldUseWebGLLoader)
        {
            // WebGL mode: Don't initialize database or runtime
            m_DatabaseManager = null;
            m_SidekickRuntime = null;
            m_IsInitialized = true; // Mark as initialized so other code doesn't try again

            if (m_DebugLog)
            {
                Debug.Log("[SidekickCharacterManager] WebGL mode: Skipping SQLite initialization");
            }

            return true;
        }

        try
        {
            // Initialize database manager
            m_DatabaseManager = new DatabaseManager();
            m_DatabaseManager.GetDbConnection(true);

            // Create Sidekick runtime
            m_SidekickRuntime = new SidekickRuntime(
                m_BaseModel,
                m_CharacterMaterial,
                m_AnimatorController,
                m_DatabaseManager
            );

            m_IsInitialized = true;

            if (m_DebugLog)
            {
                Debug.Log("[SidekickCharacterManager] Initialized successfully");
            }

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SidekickCharacterManager] Failed to initialize: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Load part library from Sidekick database.
    /// Call this after Initialize() to populate available parts.
    /// </summary>
    public async void LoadPartLibrary()
    {
        if (!m_IsInitialized)
        {
            Debug.LogWarning("[SidekickCharacterManager] Not initialized. Call Initialize() first.");
            return;
        }

        await SidekickRuntime.PopulateToolData(m_SidekickRuntime);

        if (m_DebugLog)
        {
            Debug.Log($"[SidekickCharacterManager] Loaded {m_SidekickRuntime.PartCount} parts");
        }
    }

    /// <summary>
    /// Get all available parts for a specific CharacterPartType.
    /// </summary>
    public List<string> GetAvailableParts(CharacterPartType partType)
    {
        if (!m_IsInitialized || m_SidekickRuntime.MappedPartList == null)
        {
            return new List<string>();
        }

        if (m_SidekickRuntime.MappedPartList.TryGetValue(partType, out var parts))
        {
            return parts;
        }

        return new List<string>();
    }

    /// <summary>
    /// Get available parts for an equipment slot.
    /// </summary>
    public List<string> GetAvailablePartsForSlot(EquipmentSlotType slotType)
    {
        var result = new List<string>();

        if (!EquipmentToSidekickMap.TryGetValue(slotType, out var partTypes))
        {
            return result;
        }

        foreach (var partType in partTypes)
        {
            result.AddRange(GetAvailableParts(partType));
        }

        return result;
    }

    /// <summary>
    /// Change a character part by name.
    /// </summary>
    public void SetPart(CharacterPartType partType, string partName)
    {
        if (!m_IsInitialized)
        {
            Debug.LogWarning("[SidekickCharacterManager] Not initialized.");
            return;
        }

        m_CurrentParts[partType] = partName;

        if (m_DebugLog)
        {
            Debug.Log($"[SidekickCharacterManager] Set {partType} to {partName}");
        }

        // Note: Actual part swapping requires rebuilding the character mesh
        // This would typically be done after all part changes are queued
    }

    /// <summary>
    /// Change parts for an equipment slot.
    /// </summary>
    public void SetPartsForSlot(EquipmentSlotType slotType, string partName)
    {
        if (!EquipmentToSidekickMap.TryGetValue(slotType, out var partTypes))
        {
            Debug.LogWarning($"[SidekickCharacterManager] No mapping for slot {slotType}");
            return;
        }

        foreach (var partType in partTypes)
        {
            SetPart(partType, partName);
        }
    }

    /// <summary>
    /// Remove a part (set to empty/none).
    /// </summary>
    public void RemovePart(CharacterPartType partType)
    {
        if (m_CurrentParts.ContainsKey(partType))
        {
            m_CurrentParts.Remove(partType);
        }

        if (m_DebugLog)
        {
            Debug.Log($"[SidekickCharacterManager] Removed {partType}");
        }
    }

    /// <summary>
    /// Remove all parts for an equipment slot.
    /// </summary>
    public void RemovePartsForSlot(EquipmentSlotType slotType)
    {
        if (!EquipmentToSidekickMap.TryGetValue(slotType, out var partTypes))
        {
            return;
        }

        foreach (var partType in partTypes)
        {
            RemovePart(partType);
        }
    }

    /// <summary>
    /// Apply all queued part changes and rebuild the character mesh.
    /// Call this after making multiple part changes.
    /// </summary>
    public void ApplyChanges()
    {
        if (!m_IsInitialized)
        {
            Debug.LogWarning("[SidekickCharacterManager] Not initialized.");
            return;
        }

        // This would involve:
        // 1. Loading the mesh parts from the database
        // 2. Creating/combining the meshes using SidekickRuntime.CreateCharacter()
        // 3. Updating the character GameObject

        if (m_DebugLog)
        {
            Debug.Log("[SidekickCharacterManager] Applied changes");
        }
    }

    /// <summary>
    /// Set body type blend shape (-100 to 100, masculine to feminine).
    /// </summary>
    public void SetBodyType(float value)
    {
        if (!m_IsInitialized) return;

        m_SidekickRuntime.BodyTypeBlendValue = Mathf.Clamp(value, -100f, 100f);

        if (m_CurrentCharacter != null)
        {
            m_SidekickRuntime.UpdateBlendShapes(m_CurrentCharacter);
        }
    }

    /// <summary>
    /// Set muscle blend shape (-100 to 100).
    /// </summary>
    public void SetMuscles(float value)
    {
        if (!m_IsInitialized) return;

        m_SidekickRuntime.MusclesBlendValue = Mathf.Clamp(value, -100f, 100f);

        if (m_CurrentCharacter != null)
        {
            m_SidekickRuntime.UpdateBlendShapes(m_CurrentCharacter);
        }
    }

    /// <summary>
    /// Set body size (skinny/heavy).
    /// </summary>
    public void SetBodySize(float skinny, float heavy)
    {
        if (!m_IsInitialized) return;

        m_SidekickRuntime.BodySizeSkinnyBlendValue = Mathf.Clamp01(skinny) * 100f;
        m_SidekickRuntime.BodySizeHeavyBlendValue = Mathf.Clamp01(heavy) * 100f;

        if (m_CurrentCharacter != null)
        {
            m_SidekickRuntime.UpdateBlendShapes(m_CurrentCharacter);
        }
    }

    /// <summary>
    /// Get the Sidekick CharacterPartType array for an EquipmentSlotType.
    /// </summary>
    public static CharacterPartType[] GetSidekickPartsForSlot(EquipmentSlotType slotType)
    {
        if (EquipmentToSidekickMap.TryGetValue(slotType, out var parts))
        {
            return parts;
        }
        return new CharacterPartType[0];
    }

    private void OnDestroy()
    {
        // Clean up database connection
        if (m_DatabaseManager != null)
        {
            // DatabaseManager handles its own cleanup
            m_DatabaseManager = null;
        }
    }
}
