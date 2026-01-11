using UnityEngine;
using UnityEditor;
using System.Reflection;

/// <summary>
/// Editor tool for adjusting holster positions in real-time during Play mode.
/// Allows fine-tuning of pistol and rifle holster positions with immediate visual feedback.
/// </summary>
public class HolsterPositionAdjusterWindow : EditorWindow
{
    private enum HolsterType
    {
        Pistol,
        RifleDefault,
        RifleSmallBackpack,
        RifleMediumBackpack,
        RifleLargeBackpack
    }

    private SidekickPlayerController m_Controller;
    private RifleHolsterManager m_RifleHolsterManager;
    private HolsterType m_SelectedHolster = HolsterType.RifleDefault;

    // Cached transforms
    private Transform m_RifleHolsterTransform;
    private Transform m_PistolHolsterTransform;
    private Transform m_DefaultSpot;
    private Transform m_SmallBPSpot;
    private Transform m_MediumBPSpot;
    private Transform m_LargeBPSpot;

    // Rifle weapon transform (to see where it actually is)
    private Transform m_RifleWeapon;

    // Current values being edited
    private Vector3 m_Position;
    private Vector3 m_Rotation;

    // Adjustment amounts
    private float m_PosStep = 0.01f;
    private float m_RotStep = 1f;

    // Scroll position
    private Vector2 m_ScrollPos;

    // Auto-update
    private bool m_AutoUpdate = true;

    [MenuItem("Tools/Holster Position Adjuster")]
    public static void ShowWindow()
    {
        var window = GetWindow<HolsterPositionAdjusterWindow>("Holster Adjuster");
        window.minSize = new Vector2(380, 600);
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        ClearReferences();
        Repaint();
    }

    private void ClearReferences()
    {
        m_Controller = null;
        m_RifleHolsterManager = null;
        m_RifleHolsterTransform = null;
        m_PistolHolsterTransform = null;
        m_DefaultSpot = null;
        m_SmallBPSpot = null;
        m_MediumBPSpot = null;
        m_LargeBPSpot = null;
    }

    private void OnEditorUpdate()
    {
        if (Application.isPlaying && m_AutoUpdate)
        {
            Repaint();
        }
    }

    private void OnGUI()
    {
        m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

        EditorGUILayout.LabelField("Holster Position Adjuster", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play mode to adjust holster positions in real-time.", MessageType.Info);
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Default Values Reference", EditorStyles.boldLabel);
            DrawDefaultValuesReference();
            EditorGUILayout.EndScrollView();
            return;
        }

        // Find references
        FindReferences();

        if (m_Controller == null)
        {
            EditorGUILayout.HelpBox("No SidekickPlayerController found in scene.", MessageType.Warning);
            if (GUILayout.Button("Refresh"))
            {
                ClearReferences();
                FindReferences();
            }
            EditorGUILayout.EndScrollView();
            return;
        }

        // Status info
        DrawStatusInfo();

        EditorGUILayout.Space(10);

        // Holster type selection
        EditorGUILayout.LabelField("Select Holster", EditorStyles.boldLabel);
        var prevHolster = m_SelectedHolster;
        m_SelectedHolster = (HolsterType)EditorGUILayout.EnumPopup("Holster Type", m_SelectedHolster);

        // Reload values when selection changes
        if (prevHolster != m_SelectedHolster)
        {
            LoadCurrentValuesFromTransform();
        }

        EditorGUILayout.Space(5);
        m_AutoUpdate = EditorGUILayout.Toggle("Auto-Refresh Display", m_AutoUpdate);

        EditorGUILayout.Space(10);

        // Step size controls
        EditorGUILayout.LabelField("Step Sizes", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Position:", GUILayout.Width(60));
        if (GUILayout.Button("0.001")) m_PosStep = 0.001f;
        if (GUILayout.Button("0.01")) m_PosStep = 0.01f;
        if (GUILayout.Button("0.05")) m_PosStep = 0.05f;
        if (GUILayout.Button("0.1")) m_PosStep = 0.1f;
        EditorGUILayout.LabelField($"= {m_PosStep}", GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Rotation:", GUILayout.Width(60));
        if (GUILayout.Button("0.1")) m_RotStep = 0.1f;
        if (GUILayout.Button("1")) m_RotStep = 1f;
        if (GUILayout.Button("5")) m_RotStep = 5f;
        if (GUILayout.Button("15")) m_RotStep = 15f;
        EditorGUILayout.LabelField($"= {m_RotStep}", GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Position controls
        EditorGUILayout.LabelField("Position (Local Offset)", EditorStyles.boldLabel);
        DrawVector3Control("X", ref m_Position.x, m_PosStep);
        DrawVector3Control("Y", ref m_Position.y, m_PosStep);
        DrawVector3Control("Z", ref m_Position.z, m_PosStep);

        EditorGUILayout.Space(10);

        // Rotation controls
        EditorGUILayout.LabelField("Rotation (Euler Angles)", EditorStyles.boldLabel);
        DrawVector3Control("X", ref m_Rotation.x, m_RotStep);
        DrawVector3Control("Y", ref m_Rotation.y, m_RotStep);
        DrawVector3Control("Z", ref m_Rotation.z, m_RotStep);

        EditorGUILayout.Space(10);

        // Action buttons
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Apply", GUILayout.Height(25)))
        {
            ApplyChanges();
        }
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("Read Current", GUILayout.Height(25)))
        {
            LoadCurrentValuesFromTransform();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Current values display (for copying)
        EditorGUILayout.LabelField("Values to Copy to Prefab", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.SelectableLabel($"Position: ({m_Position.x:F3}, {m_Position.y:F3}, {m_Position.z:F3})", GUILayout.Height(18));
        EditorGUILayout.SelectableLabel($"Rotation: ({m_Rotation.x:F2}, {m_Rotation.y:F2}, {m_Rotation.z:F2})", GUILayout.Height(18));
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Copy Position"))
        {
            GUIUtility.systemCopyBuffer = $"{m_Position.x:F3}, {m_Position.y:F3}, {m_Position.z:F3}";
            Debug.Log($"Copied: {GUIUtility.systemCopyBuffer}");
        }
        if (GUILayout.Button("Copy Rotation"))
        {
            GUIUtility.systemCopyBuffer = $"{m_Rotation.x:F2}, {m_Rotation.y:F2}, {m_Rotation.z:F2}";
            Debug.Log($"Copied: {GUIUtility.systemCopyBuffer}");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Live transform info
        DrawLiveHolsterInfo();

        EditorGUILayout.EndScrollView();
    }

    private void FindReferences()
    {
        if (m_Controller == null)
        {
            m_Controller = FindFirstObjectByType<SidekickPlayerController>();
        }

        if (m_Controller == null) return;

        if (m_RifleHolsterManager == null)
        {
            m_RifleHolsterManager = m_Controller.GetComponent<RifleHolsterManager>();
        }

        // Find holster transforms
        var allTransforms = m_Controller.GetComponentsInChildren<Transform>(true);
        foreach (var t in allTransforms)
        {
            switch (t.name)
            {
                case "RifleHolster": m_RifleHolsterTransform = t; break;
                case "PistolHolster": m_PistolHolsterTransform = t; break;
                case "DefaultHolsterSpot": m_DefaultSpot = t; break;
                case "SmallBackpackHolsterSpot": m_SmallBPSpot = t; break;
                case "MediumBackpackHolsterSpot": m_MediumBPSpot = t; break;
                case "LargeBackpackHolsterSpot": m_LargeBPSpot = t; break;
            }
        }
    }

    private void DrawStatusInfo()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

        EditorGUILayout.LabelField($"Controller: {(m_Controller != null ? m_Controller.name : "NOT FOUND")}",
            m_Controller != null ? EditorStyles.miniLabel : GetRedStyle());

        EditorGUILayout.LabelField($"RifleHolsterManager: {(m_RifleHolsterManager != null ? "Found" : "NOT FOUND")}",
            m_RifleHolsterManager != null ? EditorStyles.miniLabel : GetRedStyle());

        EditorGUILayout.LabelField($"RifleHolster: {(m_RifleHolsterTransform != null ? "Found" : "NOT FOUND")}",
            m_RifleHolsterTransform != null ? EditorStyles.miniLabel : GetRedStyle());

        EditorGUILayout.LabelField($"DefaultSpot: {(m_DefaultSpot != null ? "Found" : "NOT FOUND")}",
            m_DefaultSpot != null ? EditorStyles.miniLabel : GetRedStyle());

        EditorGUILayout.EndVertical();
    }

    private GUIStyle GetRedStyle()
    {
        var style = new GUIStyle(EditorStyles.miniLabel);
        style.normal.textColor = Color.red;
        return style;
    }

    private void DrawVector3Control(string label, ref float value, float step)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(20));

        if (GUILayout.Button("--", GUILayout.Width(28))) { value -= step * 5; ApplyChanges(); }
        if (GUILayout.Button("-", GUILayout.Width(28))) { value -= step; ApplyChanges(); }

        float newValue = EditorGUILayout.FloatField(value, GUILayout.Width(70));
        if (newValue != value)
        {
            value = newValue;
            ApplyChanges();
        }

        if (GUILayout.Button("+", GUILayout.Width(28))) { value += step; ApplyChanges(); }
        if (GUILayout.Button("++", GUILayout.Width(28))) { value += step * 5; ApplyChanges(); }

        EditorGUILayout.EndHorizontal();
    }

    private void LoadCurrentValuesFromTransform()
    {
        Transform target = GetTargetTransform();
        if (target != null)
        {
            m_Position = target.localPosition;
            m_Rotation = target.localRotation.eulerAngles;
            Debug.Log($"[HolsterAdjuster] Loaded from {target.name}: Pos={m_Position}, Rot={m_Rotation}");
        }
        else
        {
            Debug.LogWarning($"[HolsterAdjuster] Could not find transform for {m_SelectedHolster}");
        }
    }

    private Transform GetTargetTransform()
    {
        switch (m_SelectedHolster)
        {
            case HolsterType.Pistol: return m_PistolHolsterTransform;
            case HolsterType.RifleDefault: return m_RifleHolsterTransform ?? m_DefaultSpot;
            case HolsterType.RifleSmallBackpack: return m_SmallBPSpot;
            case HolsterType.RifleMediumBackpack: return m_MediumBPSpot;
            case HolsterType.RifleLargeBackpack: return m_LargeBPSpot;
            default: return null;
        }
    }

    private void ApplyChanges()
    {
        if (m_Controller == null) return;

        switch (m_SelectedHolster)
        {
            case HolsterType.Pistol:
                ApplyToTransform(m_PistolHolsterTransform);
                break;

            case HolsterType.RifleDefault:
                // Update both the actual holster AND the default spot
                ApplyToTransform(m_RifleHolsterTransform);
                ApplyToTransform(m_DefaultSpot);
                break;

            case HolsterType.RifleSmallBackpack:
                ApplyToTransform(m_SmallBPSpot);
                // If small backpack is equipped, also update rifle holster directly
                if (m_RifleHolsterManager != null)
                {
                    ApplyToTransform(m_RifleHolsterTransform);
                }
                break;

            case HolsterType.RifleMediumBackpack:
                ApplyToTransform(m_MediumBPSpot);
                if (m_RifleHolsterManager != null)
                {
                    ApplyToTransform(m_RifleHolsterTransform);
                }
                break;

            case HolsterType.RifleLargeBackpack:
                ApplyToTransform(m_LargeBPSpot);
                if (m_RifleHolsterManager != null)
                {
                    ApplyToTransform(m_RifleHolsterTransform);
                }
                break;
        }

        // Also update the serialized fields on SidekickPlayerController
        UpdateControllerFields();

        Debug.Log($"[HolsterAdjuster] Applied {m_SelectedHolster}: Pos={m_Position}, Rot={m_Rotation}");
    }

    private void ApplyToTransform(Transform target)
    {
        if (target == null) return;
        target.localPosition = m_Position;
        target.localRotation = Quaternion.Euler(m_Rotation);
    }

    private void UpdateControllerFields()
    {
        if (m_Controller == null) return;

        var type = m_Controller.GetType();
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;

        switch (m_SelectedHolster)
        {
            case HolsterType.Pistol:
                type.GetField("m_PistolHolsterOffset", flags)?.SetValue(m_Controller, m_Position);
                type.GetField("m_PistolHolsterRotation", flags)?.SetValue(m_Controller, m_Rotation);
                break;
            case HolsterType.RifleDefault:
                type.GetField("m_RifleHolsterDefaultOffset", flags)?.SetValue(m_Controller, m_Position);
                type.GetField("m_RifleHolsterDefaultRotation", flags)?.SetValue(m_Controller, m_Rotation);
                break;
            case HolsterType.RifleSmallBackpack:
                type.GetField("m_RifleHolsterSmallBPOffset", flags)?.SetValue(m_Controller, m_Position);
                type.GetField("m_RifleHolsterSmallBPRotation", flags)?.SetValue(m_Controller, m_Rotation);
                break;
            case HolsterType.RifleMediumBackpack:
                type.GetField("m_RifleHolsterMediumBPOffset", flags)?.SetValue(m_Controller, m_Position);
                type.GetField("m_RifleHolsterMediumBPRotation", flags)?.SetValue(m_Controller, m_Rotation);
                break;
            case HolsterType.RifleLargeBackpack:
                type.GetField("m_RifleHolsterLargeBPOffset", flags)?.SetValue(m_Controller, m_Position);
                type.GetField("m_RifleHolsterLargeBPRotation", flags)?.SetValue(m_Controller, m_Rotation);
                break;
        }
    }

    private void DrawLiveHolsterInfo()
    {
        EditorGUILayout.LabelField("Live Transform Values", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        DrawTransformInfo("RifleHolster", m_RifleHolsterTransform);
        DrawTransformInfo("PistolHolster", m_PistolHolsterTransform);
        DrawTransformInfo("DefaultSpot", m_DefaultSpot);
        DrawTransformInfo("SmallBPSpot", m_SmallBPSpot);
        DrawTransformInfo("MediumBPSpot", m_MediumBPSpot);
        DrawTransformInfo("LargeBPSpot", m_LargeBPSpot);

        EditorGUILayout.EndVertical();
    }

    private void DrawTransformInfo(string label, Transform t)
    {
        if (t != null)
        {
            EditorGUILayout.LabelField($"{label}: ({t.localPosition.x:F2}, {t.localPosition.y:F2}, {t.localPosition.z:F2})", EditorStyles.miniLabel);
        }
        else
        {
            EditorGUILayout.LabelField($"{label}: NOT FOUND", GetRedStyle());
        }
    }

    private void DrawDefaultValuesReference()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("Pistol (pelvis bone)", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField("  Pos: (-0.003, -0.005, 0.201)  Rot: (2.8, 90.54, 165.73)", EditorStyles.miniLabel);

        EditorGUILayout.LabelField("Rifle Default (spine, no backpack)", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField("  Pos: (-0.35, 0.18, 0)  Rot: (2.8, 93.6, 96.32)", EditorStyles.miniLabel);

        EditorGUILayout.LabelField("Rifle Small Backpack", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField("  Pos: (-0.49, 0.33, 0)  Rot: (2.8, 93.6, 96.32)", EditorStyles.miniLabel);

        EditorGUILayout.LabelField("Rifle Medium Backpack", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField("  Pos: (-0.33, 0.3, 0.25)  Rot: (3.1, 92, 182.34)", EditorStyles.miniLabel);

        EditorGUILayout.LabelField("Rifle Large Backpack", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField("  Pos: (-0.38, 0.27, 0.18)  Rot: (2.8, 93.6, 178.9)", EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();
    }
}
