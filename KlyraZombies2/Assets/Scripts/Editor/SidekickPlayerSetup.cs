using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Synty.SidekickCharacters.API;
using Synty.SidekickCharacters.Database;
using Synty.SidekickCharacters.Database.DTO;
using Synty.SidekickCharacters.Enums;

/// <summary>
/// Editor tool to create a Sidekick character ready for Opsive Character Manager setup.
/// This creates a basic Sidekick character that you can then run through Opsive's Character Manager.
/// </summary>
public class SidekickPlayerSetup : EditorWindow
{
    private SidekickRuntime m_Runtime;
    private DatabaseManager m_DbManager;
    private Dictionary<CharacterPartType, Dictionary<string, SidekickPart>> m_PartLibrary;
    private Dictionary<PartGroup, List<SidekickPartPreset>> m_PresetsByGroup;

    private int m_HeadIndex = 0;
    private int m_UpperBodyIndex = 0;
    private int m_LowerBodyIndex = 0;

    private string[] m_HeadNames;
    private string[] m_UpperNames;
    private string[] m_LowerNames;

    private bool m_Initialized = false;
    private Vector2 m_ScrollPos;

    [MenuItem("Project Klyra/Sidekick/Create Player Character")]
    public static void ShowWindow()
    {
        GetWindow<SidekickPlayerSetup>("Sidekick Player Setup");
    }

    private void OnEnable()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (m_Initialized) return;

        // Load base model
        GameObject baseModel = Resources.Load<GameObject>("Meshes/SK_BaseModel");
        if (baseModel == null)
        {
            // Try alternate paths
            string[] guids = AssetDatabase.FindAssets("SK_BaseModel t:GameObject");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                baseModel = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }

        if (baseModel == null)
        {
            Debug.LogError("[SidekickPlayerSetup] Could not find SK_BaseModel!");
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
            Debug.LogError("[SidekickPlayerSetup] Could not find M_BaseMaterial!");
            return;
        }

        // Initialize runtime
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

        // Build name arrays
        m_HeadNames = m_PresetsByGroup[PartGroup.Head].Select(p => p.Name).ToArray();
        m_UpperNames = m_PresetsByGroup[PartGroup.UpperBody].Select(p => p.Name).ToArray();
        m_LowerNames = m_PresetsByGroup[PartGroup.LowerBody].Select(p => p.Name).ToArray();

        m_Initialized = true;
        Debug.Log($"[SidekickPlayerSetup] Loaded {m_HeadNames.Length} head, {m_UpperNames.Length} upper, {m_LowerNames.Length} lower presets");
    }

    private void OnGUI()
    {
        m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

        EditorGUILayout.LabelField("Sidekick Player Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (!m_Initialized)
        {
            EditorGUILayout.HelpBox(
                "Could not initialize Sidekick. Make sure SK_BaseModel and M_BaseMaterial are in your project.",
                MessageType.Error);

            if (GUILayout.Button("Retry Initialize"))
            {
                m_Initialized = false;
                Initialize();
            }

            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUILayout.HelpBox(
            "This tool creates a Sidekick character in your scene.\n\n" +
            "After creating, use Opsive's Character Manager to add UCC components:\n" +
            "1. Click 'Create Sidekick Player' below\n" +
            "2. Go to Tools > Opsive > Ultimate Character Controller > Character Manager\n" +
            "3. Select the created character\n" +
            "4. Click 'Build Character' to add all Opsive components",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Character Appearance", EditorStyles.boldLabel);

        if (m_HeadNames.Length > 0)
            m_HeadIndex = EditorGUILayout.Popup("Head", m_HeadIndex, m_HeadNames);
        if (m_UpperNames.Length > 0)
            m_UpperBodyIndex = EditorGUILayout.Popup("Upper Body", m_UpperBodyIndex, m_UpperNames);
        if (m_LowerNames.Length > 0)
            m_LowerBodyIndex = EditorGUILayout.Popup("Lower Body", m_LowerBodyIndex, m_LowerNames);

        EditorGUILayout.Space();

        if (GUILayout.Button("Create Sidekick Player", GUILayout.Height(40)))
        {
            CreateSidekickPlayer();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Open Opsive Character Manager"))
        {
            EditorApplication.ExecuteMenuItem("Tools/Opsive/Ultimate Character Controller/Character Manager");
        }

        EditorGUILayout.EndScrollView();
    }

    private void CreateSidekickPlayer()
    {
        // Get selected presets
        SidekickPartPreset headPreset = m_PresetsByGroup[PartGroup.Head][m_HeadIndex];
        SidekickPartPreset upperPreset = m_PresetsByGroup[PartGroup.UpperBody][m_UpperBodyIndex];
        SidekickPartPreset lowerPreset = m_PresetsByGroup[PartGroup.LowerBody][m_LowerBodyIndex];

        // Collect parts
        List<SkinnedMeshRenderer> partsToUse = new List<SkinnedMeshRenderer>();
        AddPartsFromPreset(headPreset, partsToUse);
        AddPartsFromPreset(upperPreset, partsToUse);
        AddPartsFromPreset(lowerPreset, partsToUse);

        if (partsToUse.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No parts found for character!", "OK");
            return;
        }

        // Create the character
        GameObject character = m_Runtime.CreateCharacter("SidekickPlayer", partsToUse, false, true);
        character.transform.position = Vector3.zero;

        // Add Animator with the proper avatar
        Animator animator = character.GetComponent<Animator>();
        if (animator == null)
        {
            animator = character.AddComponent<Animator>();
        }

        // Find the SK_BaseModel avatar
        string[] avatarGuids = AssetDatabase.FindAssets("SK_BaseModelAvatar t:Avatar");
        if (avatarGuids.Length == 0)
        {
            // Try to find it in the FBX
            string[] fbxGuids = AssetDatabase.FindAssets("SK_BaseModel t:Model");
            foreach (var guid in fbxGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var asset in assets)
                {
                    if (asset is Avatar avatar)
                    {
                        animator.avatar = avatar;
                        Debug.Log($"[SidekickPlayerSetup] Found avatar in {path}");
                        break;
                    }
                }
                if (animator.avatar != null) break;
            }
        }
        else
        {
            string avatarPath = AssetDatabase.GUIDToAssetPath(avatarGuids[0]);
            animator.avatar = AssetDatabase.LoadAssetAtPath<Avatar>(avatarPath);
        }

        // Set animator to use humanoid
        animator.applyRootMotion = false;

        // Select the new character
        Selection.activeGameObject = character;

        // Register undo
        Undo.RegisterCreatedObjectUndo(character, "Create Sidekick Player");

        EditorUtility.DisplayDialog("Success",
            "Sidekick character created!\n\n" +
            "Next steps:\n" +
            "1. Keep the character selected\n" +
            "2. Go to Tools > Opsive > Ultimate Character Controller > Character Manager\n" +
            "3. Click 'Build Character' to add Opsive components\n" +
            "4. Choose your desired movement type (Third Person, etc.)",
            "OK");

        Debug.Log($"[SidekickPlayerSetup] Created Sidekick player with {partsToUse.Count} mesh parts");
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
                    Synty.SidekickCharacters.Utils.CharacterPartTypeUtils.GetTypeNameFromShortcode(row.PartType));

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
                Debug.LogWarning($"[SidekickPlayerSetup] Failed to load part {row.PartName}: {e.Message}");
            }
        }
    }
}
