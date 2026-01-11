using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor tool to set up equipment slots and attachment points on a character.
/// </summary>
public class EquipmentSlotSetup : EditorWindow
{
    private GameObject m_Character;
    private Transform m_SelectedBone;
    private EquipmentSlotType m_SelectedSlotType;
    private Vector2 m_ScrollPos;

    // Common bone names for each slot type
    private static readonly Dictionary<EquipmentSlotType, string[]> SlotBoneHints = new Dictionary<EquipmentSlotType, string[]>
    {
        { EquipmentSlotType.Head, new[] { "head", "Head", "HEAD" } },
        { EquipmentSlotType.Face, new[] { "head", "Head", "HEAD" } },
        { EquipmentSlotType.Hair, new[] { "head", "Head", "HEAD" } },
        { EquipmentSlotType.Torso, new[] { "spine", "Spine", "chest", "Chest" } },
        { EquipmentSlotType.Hands, new[] { "hand", "Hand", "wrist", "Wrist" } },
        { EquipmentSlotType.Legs, new[] { "hips", "Hips", "pelvis", "Pelvis" } },
        { EquipmentSlotType.Feet, new[] { "foot", "Foot", "ankle", "Ankle" } },
        { EquipmentSlotType.Back, new[] { "spine", "Spine", "chest", "Chest" } },
        { EquipmentSlotType.ShoulderLeft, new[] { "shoulder_l", "shoulder.L", "LeftShoulder" } },
        { EquipmentSlotType.ShoulderRight, new[] { "shoulder_r", "shoulder.R", "RightShoulder" } },
        { EquipmentSlotType.KneeLeft, new[] { "leg_l", "leg.L", "LeftLeg", "calf_l" } },
        { EquipmentSlotType.KneeRight, new[] { "leg_r", "leg.R", "RightLeg", "calf_r" } },
        { EquipmentSlotType.Belt, new[] { "hips", "Hips", "pelvis", "Pelvis" } },
    };

    [MenuItem("Project Klyra/Equipment/Equipment Slot Setup")]
    public static void ShowWindow()
    {
        GetWindow<EquipmentSlotSetup>("Equipment Slot Setup");
    }

    private void OnEnable()
    {
        // Try to find character in selection
        if (Selection.activeGameObject != null)
        {
            var animator = Selection.activeGameObject.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                m_Character = Selection.activeGameObject;
            }
        }
    }

    private void OnGUI()
    {
        m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

        EditorGUILayout.LabelField("Equipment Slot Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This tool helps you set up equipment attachment points on your character.\n\n" +
            "1. Select your character\n" +
            "2. Choose a slot type\n" +
            "3. Find or create the attachment point\n" +
            "4. Add EquipmentVisualHandler to the character",
            MessageType.Info);

        EditorGUILayout.Space();

        // Character selection
        m_Character = (GameObject)EditorGUILayout.ObjectField(
            "Character", m_Character, typeof(GameObject), true);

        if (m_Character == null)
        {
            EditorGUILayout.HelpBox("Please select a character GameObject.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        // Check for animator
        var animator = m_Character.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            EditorGUILayout.HelpBox("Character needs an Animator component.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Slot Configuration", EditorStyles.boldLabel);

        // Slot type selection
        m_SelectedSlotType = (EquipmentSlotType)EditorGUILayout.EnumPopup("Slot Type", m_SelectedSlotType);

        // Show suggested bones
        if (SlotBoneHints.TryGetValue(m_SelectedSlotType, out var hints))
        {
            EditorGUILayout.LabelField($"Suggested bones: {string.Join(", ", hints)}", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space();

        // Bone selection
        m_SelectedBone = (Transform)EditorGUILayout.ObjectField(
            "Attachment Bone", m_SelectedBone, typeof(Transform), true);

        EditorGUILayout.Space();

        // Auto-find bone button
        if (GUILayout.Button("Auto-Find Bone"))
        {
            AutoFindBone(animator.transform);
        }

        EditorGUILayout.Space();

        // List all bones
        EditorGUILayout.LabelField("Character Bones:", EditorStyles.boldLabel);
        DrawBoneList(animator.transform);

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        // Create attachment point
        EditorGUILayout.LabelField("Create Attachment Point", EditorStyles.boldLabel);

        if (m_SelectedBone == null)
        {
            EditorGUILayout.HelpBox("Select a bone first to create an attachment point.", MessageType.Info);
        }
        else
        {
            if (GUILayout.Button($"Create '{m_SelectedSlotType}Slot' at {m_SelectedBone.name}", GUILayout.Height(30)))
            {
                CreateAttachmentPoint();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        // Add EquipmentVisualHandler button
        EditorGUILayout.LabelField("Setup Handler", EditorStyles.boldLabel);

        var existingHandler = m_Character.GetComponent<EquipmentVisualHandler>();
        if (existingHandler != null)
        {
            EditorGUILayout.LabelField("EquipmentVisualHandler already exists.", EditorStyles.miniLabel);
            if (GUILayout.Button("Select Handler Component"))
            {
                Selection.activeGameObject = m_Character;
            }
        }
        else
        {
            if (GUILayout.Button("Add EquipmentVisualHandler", GUILayout.Height(30)))
            {
                Undo.AddComponent<EquipmentVisualHandler>(m_Character);
                Debug.Log("[EquipmentSlotSetup] Added EquipmentVisualHandler to " + m_Character.name);
            }
        }

        EditorGUILayout.Space();

        // Find existing attachment points
        EditorGUILayout.LabelField("Existing Attachment Points:", EditorStyles.boldLabel);
        DrawExistingAttachments();

        EditorGUILayout.EndScrollView();
    }

    private void AutoFindBone(Transform root)
    {
        if (!SlotBoneHints.TryGetValue(m_SelectedSlotType, out var hints))
        {
            return;
        }

        var allTransforms = root.GetComponentsInChildren<Transform>();

        foreach (var hint in hints)
        {
            foreach (var t in allTransforms)
            {
                if (t.name.ToLower().Contains(hint.ToLower()))
                {
                    m_SelectedBone = t;
                    Debug.Log($"[EquipmentSlotSetup] Found bone: {t.name}");
                    return;
                }
            }
        }

        Debug.LogWarning($"[EquipmentSlotSetup] Could not find bone for {m_SelectedSlotType}");
    }

    private void DrawBoneList(Transform root)
    {
        var allTransforms = root.GetComponentsInChildren<Transform>();
        int shown = 0;
        int max = 15;

        EditorGUI.indentLevel++;

        foreach (var t in allTransforms)
        {
            if (t == root) continue;

            // Only show bones (transforms that are likely part of the skeleton)
            bool isBone = t.name.Contains("bone") || t.name.Contains("Bone") ||
                          t.name.Contains("spine") || t.name.Contains("Spine") ||
                          t.name.Contains("head") || t.name.Contains("Head") ||
                          t.name.Contains("arm") || t.name.Contains("Arm") ||
                          t.name.Contains("hand") || t.name.Contains("Hand") ||
                          t.name.Contains("leg") || t.name.Contains("Leg") ||
                          t.name.Contains("foot") || t.name.Contains("Foot") ||
                          t.name.Contains("hip") || t.name.Contains("Hip") ||
                          t.name.Contains("shoulder") || t.name.Contains("Shoulder") ||
                          t.name.Contains("root") || t.name.Contains("Root") ||
                          t.name.Contains("pelvis") || t.name.Contains("Pelvis") ||
                          t.name.Contains("neck") || t.name.Contains("Neck") ||
                          t.name.Contains("clavicle") || t.name.Contains("Clavicle") ||
                          t.parent?.name == "root";

            if (!isBone) continue;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(t.name, GUILayout.MinWidth(200));
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                m_SelectedBone = t;
            }
            EditorGUILayout.EndHorizontal();

            shown++;
            if (shown >= max)
            {
                EditorGUILayout.LabelField($"... and {allTransforms.Length - max - 1} more transforms");
                break;
            }
        }

        EditorGUI.indentLevel--;
    }

    private void DrawExistingAttachments()
    {
        if (m_Character == null) return;

        var allTransforms = m_Character.GetComponentsInChildren<Transform>();
        bool found = false;

        EditorGUI.indentLevel++;

        foreach (var t in allTransforms)
        {
            if (t.name.EndsWith("Slot"))
            {
                found = true;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(t.name + " (parent: " + t.parent?.name + ")");
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeGameObject = t.gameObject;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        if (!found)
        {
            EditorGUILayout.LabelField("No attachment points found.", EditorStyles.miniLabel);
        }

        EditorGUI.indentLevel--;
    }

    private void CreateAttachmentPoint()
    {
        if (m_SelectedBone == null) return;

        string slotName = m_SelectedSlotType.ToString() + "Slot";

        // Check if already exists
        var existing = m_SelectedBone.Find(slotName);
        if (existing != null)
        {
            EditorUtility.DisplayDialog("Already Exists",
                $"Attachment point '{slotName}' already exists under {m_SelectedBone.name}.",
                "OK");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // Create the attachment point
        var attachPoint = new GameObject(slotName);
        Undo.RegisterCreatedObjectUndo(attachPoint, "Create Attachment Point");

        attachPoint.transform.SetParent(m_SelectedBone);
        attachPoint.transform.localPosition = Vector3.zero;
        attachPoint.transform.localRotation = Quaternion.identity;
        attachPoint.transform.localScale = Vector3.one;

        Selection.activeGameObject = attachPoint;
        EditorGUIUtility.PingObject(attachPoint);

        Debug.Log($"[EquipmentSlotSetup] Created attachment point: {slotName} under {m_SelectedBone.name}");

        EditorUtility.DisplayDialog("Attachment Point Created",
            $"Created '{slotName}' as child of {m_SelectedBone.name}.\n\n" +
            "Adjust the position and rotation as needed.",
            "OK");
    }
}
