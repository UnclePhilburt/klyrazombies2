using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Synty.SidekickCharacters.API;
using Synty.SidekickCharacters.Database;
using Synty.SidekickCharacters.Database.DTO;
using Synty.SidekickCharacters.Enums;
using Synty.SidekickCharacters.Utils;

/// <summary>
/// Spawns the player character using Sidekick system based on saved character data.
/// Use this in your game scene to spawn the player with their customized appearance.
/// </summary>
[DefaultExecutionOrder(100)] // Run after LoadingScreen
public class SidekickCharacterSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Where to spawn the player")]
    [SerializeField] private Transform m_SpawnPoint;

    [Header("Player Prefab")]
    [Tooltip("Base player prefab that the Sidekick character will be added to. Should have Opsive components but NO character model.")]
    [SerializeField] private GameObject m_PlayerPrefabBase;

    [Header("Sidekick Resources")]
    [Tooltip("Leave empty to load from Resources")]
    [SerializeField] private GameObject m_BaseModelOverride;
    [SerializeField] private Material m_BaseMaterialOverride;
    [SerializeField] private RuntimeAnimatorController m_AnimatorController;
    [Tooltip("Humanoid avatar for the character. Find it inside SK_BaseModel.fbx")]
    [SerializeField] private Avatar m_CharacterAvatar;

    [Header("Character Save")]
    [SerializeField] private string m_SaveKey = "PlayerCharacter";

    [Header("Loading Screen")]
    [Tooltip("If true, creates a loading screen automatically")]
    [SerializeField] private bool m_UseLoadingScreen = false; // Disabled for testing
    [Tooltip("Optional existing loading screen prefab")]
    [SerializeField] private LoadingScreen m_LoadingScreenPrefab;

    [Header("Debug")]
    [SerializeField] private bool m_SpawnOnAwake = true;
    [SerializeField] private bool m_DebugLog = false;

    // Sidekick
    private SidekickRuntime m_Runtime;
    private DatabaseManager m_DbManager;
    private Dictionary<CharacterPartType, Dictionary<string, SidekickPart>> m_PartLibrary;
    private Dictionary<PartGroup, List<SidekickPartPreset>> m_PresetsByGroup;

    private GameObject m_SpawnedPlayer;
    private LoadingScreen m_LoadingScreen;

    public GameObject SpawnedPlayer => m_SpawnedPlayer;

    private void Awake()
    {
        if (m_SpawnOnAwake)
        {
            // Spawn synchronously (original behavior - loading screen disabled for now)
            SpawnPlayerInternal();
            SetupCameraTarget();
        }
    }

    private IEnumerator SpawnAfterLoadingScreen()
    {
        // Wait one frame so loading screen can render
        yield return null;

        UpdateLoadingProgress(0.5f, "Spawning player...");

        // Spawn the player
        SpawnPlayerInternal();

        // Setup camera to follow new player
        SetupCameraTarget();

        // Wait for everything to settle, then hide loading screen
        yield return null;
        UpdateLoadingProgress(0.9f, "Finalizing...");
        yield return null;
        UpdateLoadingProgress(1f, "Ready!");
        yield return new WaitForSeconds(0.2f);

        HideLoadingScreen();
    }

    /// <summary>
    /// Find and setup the camera to follow the spawned player.
    /// </summary>
    private void SetupCameraTarget()
    {
        if (m_SpawnedPlayer == null) return;

        // Try Opsive Camera Controller
        var cameraController = FindFirstObjectByType<Opsive.UltimateCharacterController.Camera.CameraController>();
        if (cameraController != null)
        {
            // Use the Character property to assign the target
            cameraController.Character = m_SpawnedPlayer;
            Debug.Log($"[SidekickCharacterSpawner] Set Opsive camera target to {m_SpawnedPlayer.name}");
            return;
        }

        Debug.LogWarning("[SidekickCharacterSpawner] No Opsive CameraController found! Camera may not follow player.");
    }

    private void ShowLoadingScreen()
    {
        // Use existing instance if available
        if (LoadingScreen.Instance != null)
        {
            m_LoadingScreen = LoadingScreen.Instance;
            return;
        }

        // Create from prefab or auto-create
        if (m_LoadingScreenPrefab != null)
        {
            m_LoadingScreen = Instantiate(m_LoadingScreenPrefab);
        }
        else
        {
            // Auto-create loading screen
            GameObject loadingObj = new GameObject("LoadingScreen");
            m_LoadingScreen = loadingObj.AddComponent<LoadingScreen>();
        }
    }

    private void HideLoadingScreen()
    {
        if (m_LoadingScreen != null)
        {
            m_LoadingScreen.Hide();
        }
    }

    private void UpdateLoadingProgress(float progress, string message)
    {
        if (m_LoadingScreen != null)
        {
            m_LoadingScreen.SetProgress(progress);
            m_LoadingScreen.SetMessage(message);
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
            Debug.LogError("[SidekickCharacterSpawner] Could not load SK_BaseModel!");
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
            Debug.LogError("[SidekickCharacterSpawner] Could not load M_BaseMaterial!");
            return false;
        }

        // Check if we should use WebGL mode (skip SQLite)
        if (SidekickWebGLLoader.ShouldUseWebGLLoader)
        {
            // WebGL mode: Don't initialize database or runtime
            // SidekickPlayerController will handle everything
            m_DbManager = null;
            m_Runtime = null;
            m_PartLibrary = null;
            m_PresetsByGroup = new Dictionary<PartGroup, List<SidekickPartPreset>>();

            if (m_DebugLog)
            {
                Debug.Log("[SidekickCharacterSpawner] WebGL mode: Skipping SQLite initialization");
            }

            return true;
        }

        // Standard mode: Initialize with database
        m_DbManager = new DatabaseManager();
        m_Runtime = new SidekickRuntime(baseModel, baseMaterial, m_AnimatorController, m_DbManager);
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
            Debug.Log($"[SidekickCharacterSpawner] Initialized with {m_Runtime.PartCount} parts");
        }

        return true;
    }

    /// <summary>
    /// Spawn the player with saved character appearance (public API).
    /// </summary>
    public GameObject SpawnPlayer()
    {
        if (m_UseLoadingScreen)
        {
            ShowLoadingScreen();
            StartCoroutine(SpawnAfterLoadingScreen());
            return m_SpawnedPlayer; // Will be set by coroutine
        }

        return SpawnPlayerInternal();
    }

    /// <summary>
    /// Internal spawn logic.
    /// Uses SidekickPlayerController to swap meshes onto the existing prefab skeleton.
    /// </summary>
    private GameObject SpawnPlayerInternal()
    {
        Debug.Log("[SidekickCharacterSpawner] SpawnPlayerInternal called");

        if (m_PlayerPrefabBase == null)
        {
            Debug.LogError("[SidekickCharacterSpawner] No player prefab assigned!");
            return null;
        }

        // Determine spawn position
        Vector3 spawnPos = m_SpawnPoint != null ? m_SpawnPoint.position : transform.position;
        Quaternion spawnRot = m_SpawnPoint != null ? m_SpawnPoint.rotation : transform.rotation;

        Debug.Log($"[SidekickCharacterSpawner] Spawning at {spawnPos}");

        // Spawn the player prefab
        m_SpawnedPlayer = Instantiate(m_PlayerPrefabBase, spawnPos, spawnRot);
        m_SpawnedPlayer.name = "Player";
        m_SpawnedPlayer.tag = "Player";

        Debug.Log($"[SidekickCharacterSpawner] Instantiated player prefab: {m_SpawnedPlayer.name}");

        // Get or add SidekickPlayerController - it will handle character creation
        var controller = m_SpawnedPlayer.GetComponent<SidekickPlayerController>();
        if (controller == null)
        {
            Debug.Log("[SidekickCharacterSpawner] Adding SidekickPlayerController to player");
            controller = m_SpawnedPlayer.AddComponent<SidekickPlayerController>();
        }
        else
        {
            Debug.Log("[SidekickCharacterSpawner] SidekickPlayerController already exists on prefab");
        }

        // Use build-from-scratch mode (same as main menu preview which works correctly)
        controller.SetBuildFromScratch(true);
        controller.SetBuildOnStart(false);

        // Initialize and load the saved appearance
        // This will swap meshes onto the prefab's existing skeleton
        Debug.Log("[SidekickCharacterSpawner] Calling Initialize()...");
        if (controller.Initialize())
        {
            Debug.Log("[SidekickCharacterSpawner] Initialize succeeded, calling LoadAppearance()...");
            controller.LoadAppearance();
            Debug.Log("[SidekickCharacterSpawner] LoadAppearance completed");
        }
        else
        {
            Debug.LogError("[SidekickCharacterSpawner] Failed to initialize SidekickPlayerController!");
        }

        return m_SpawnedPlayer;
    }

    /// <summary>
    /// Setup bone references that Opsive components need.
    /// </summary>
    private void SetupOpsiveBoneReferences(GameObject player, GameObject characterMesh)
    {
        // Find the skeleton root in the Sidekick character
        Transform skeletonRoot = characterMesh.transform.Find("Root");
        if (skeletonRoot == null)
        {
            // Try to find any armature/skeleton
            var animator = characterMesh.GetComponent<Animator>();
            if (animator != null && animator.avatar != null)
            {
                // Animator has an avatar, bones should be findable
                if (m_DebugLog)
                {
                    Debug.Log("[SidekickCharacterSpawner] Character has Animator with avatar");
                }
            }
        }

        // CharacterFootEffects should NOT be on the prefab - we add it here after skeleton exists
        // This avoids the Awake() crash when no feet are configured
        SetupCharacterFootEffects(player, characterMesh);
    }

    /// <summary>
    /// Add and configure CharacterFootEffects after the skeleton exists.
    /// </summary>
    private void SetupCharacterFootEffects(GameObject player, GameObject characterMesh)
    {
        // Find foot bones in the Sidekick skeleton
        Transform leftFoot = FindBoneRecursive(characterMesh.transform, "Foot_L", "LeftFoot", "foot_l", "L_Foot", "FootL", "L Foot", "Foot.L");
        Transform rightFoot = FindBoneRecursive(characterMesh.transform, "Foot_R", "RightFoot", "foot_r", "R_Foot", "FootR", "R Foot", "Foot.R");

        if (leftFoot == null && rightFoot == null)
        {
            if (m_DebugLog)
            {
                Debug.LogWarning("[SidekickCharacterSpawner] Could not find foot bones. CharacterFootEffects not added.");
            }
            return;
        }

        // Add the component fresh - this way we can set up m_Feet BEFORE Awake runs
        // Actually, AddComponent calls Awake immediately too...
        // So we need to set up the feet via serialized field before adding

        // For now, skip CharacterFootEffects - it requires feet to be set before Awake
        // The user can manually add it in the prefab with proper foot references later
        if (m_DebugLog)
        {
            Debug.Log($"[SidekickCharacterSpawner] Found foot bones: L={leftFoot?.name}, R={rightFoot?.name}. CharacterFootEffects must be configured manually in prefab for now.");
        }
    }

    /// <summary>
    /// Recursively search for a bone by possible names.
    /// </summary>
    private Transform FindBoneRecursive(Transform root, params string[] possibleNames)
    {
        foreach (var name in possibleNames)
        {
            var found = FindChildRecursive(root, name);
            if (found != null) return found;
        }
        return null;
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name) || child.name.ToLower().Contains(name.ToLower()))
            {
                return child;
            }
            var found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private Transform FindModelContainer(Transform root)
    {
        // Look for common container names
        string[] containerNames = { "Models", "Model", "Visual", "Character", "Mesh" };

        foreach (var name in containerNames)
        {
            var container = root.Find(name);
            if (container != null) return container;
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
                Debug.LogWarning($"[SidekickCharacterSpawner] Failed to load character data: {e.Message}");
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

    private void AddPartsFromPreset(SidekickPartPreset preset, List<SkinnedMeshRenderer> partsList, HashSet<CharacterPartType> skipTypes = null)
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

                // Skip this part type if we have a custom selection for it
                if (skipTypes != null && skipTypes.Contains(type))
                {
                    if (m_DebugLog)
                    {
                        Debug.Log($"[SidekickCharacterSpawner] Skipping {type} from preset (custom selection)");
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
            catch (System.Exception e)
            {
                if (m_DebugLog)
                {
                    Debug.LogWarning($"[SidekickCharacterSpawner] Failed to load part {row.PartName}: {e.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Add an individual part by name (for face customization).
    /// </summary>
    private void AddIndividualPart(CharacterPartType partType, string partName, List<SkinnedMeshRenderer> partsList)
    {
        if (string.IsNullOrEmpty(partName))
        {
            Debug.LogWarning($"[SidekickCharacterSpawner] AddIndividualPart called with empty name for {partType}");
            return;
        }

        if (!m_PartLibrary.TryGetValue(partType, out var partDict))
        {
            Debug.LogWarning($"[SidekickCharacterSpawner] Part library has no entry for {partType}");
            return;
        }

        Debug.Log($"[SidekickCharacterSpawner] Looking for {partType} part named '{partName}' in library with {partDict.Count} parts");

        if (partDict.TryGetValue(partName, out var part))
        {
            try
            {
                GameObject partModel = part.GetPartModel();
                if (partModel != null)
                {
                    var mesh = partModel.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (mesh != null)
                    {
                        partsList.Add(mesh);
                        Debug.Log($"[SidekickCharacterSpawner] SUCCESS: Added individual part '{partName}' ({partType}), mesh: {mesh.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"[SidekickCharacterSpawner] Part '{partName}' model has no SkinnedMeshRenderer!");
                    }
                }
                else
                {
                    Debug.LogWarning($"[SidekickCharacterSpawner] Part '{partName}' returned null model!");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SidekickCharacterSpawner] Failed to load individual part {partName}: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"[SidekickCharacterSpawner] Part '{partName}' NOT FOUND in {partType} library!");
            // Log available parts for debugging
            Debug.Log($"[SidekickCharacterSpawner] Available {partType} parts: {string.Join(", ", partDict.Keys.Take(10))}...");
        }
    }

    /// <summary>
    /// Get the spawned player instance.
    /// </summary>
    public GameObject GetSpawnedPlayer() => m_SpawnedPlayer;

    /// <summary>
    /// Apply saved colors to the character.
    /// </summary>
    private void ApplyColors(CharacterSaveData saveData)
    {
        if (m_Runtime == null) return;

        // Get color properties
        var colorProperties = SidekickColorProperty.GetAll(m_DbManager);

        // Apply skin color
        if (!string.IsNullOrEmpty(saveData.skinColorHex))
        {
            Color skinColor = ColorSaveHelper.FromHex(saveData.skinColorHex, Color.white);
            ApplyColor(colorProperties, "Skin", skinColor);
        }

        // Apply hair color
        if (!string.IsNullOrEmpty(saveData.hairColorHex))
        {
            Color hairColor = ColorSaveHelper.FromHex(saveData.hairColorHex, Color.black);
            ApplyColor(colorProperties, "Hair", hairColor);
        }

        // Apply eye color
        if (!string.IsNullOrEmpty(saveData.eyeColorHex))
        {
            Color eyeColor = ColorSaveHelper.FromHex(saveData.eyeColorHex, new Color(0.25f, 0.15f, 0.08f));
            ApplyColor(colorProperties, "Eye", eyeColor);
        }

        if (m_DebugLog)
        {
            Debug.Log($"[SidekickCharacterSpawner] Applied colors - Skin: {saveData.skinColorHex}, Hair: {saveData.hairColorHex}, Eyes: {saveData.eyeColorHex}");
        }
    }

    private void ApplyColor(List<SidekickColorProperty> properties, string propertyName, Color color)
    {
        var property = properties.FirstOrDefault(p => p.Name.Contains(propertyName));
        if (property == null) return;

        var colorRow = new SidekickColorRow
        {
            ColorProperty = property,
            NiceColor = color
        };

        m_Runtime.UpdateColor(ColorType.MainColor, colorRow);
    }
}
