using UnityEngine;
using UnityEditor;
using Opsive.UltimateCharacterController.ThirdPersonController.Items;

/// <summary>
/// Editor window to adjust weapon position in real-time during Play mode.
/// Changes are applied immediately so you can see the result.
/// </summary>
public class WeaponPositionAdjuster : EditorWindow
{
    private ThirdPersonPerspectiveItem m_SelectedItem;
    private Vector3 m_Position;
    private Vector3 m_Rotation;
    private string m_CopiedValues = "";

    [MenuItem("Tools/Weapon Position Adjuster")]
    public static void ShowWindow()
    {
        GetWindow<WeaponPositionAdjuster>("Weapon Position");
    }

    private void OnGUI()
    {
        GUILayout.Label("Weapon Position Adjuster", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play mode and equip a weapon, then select it in the Hierarchy.", MessageType.Info);
            return;
        }

        // Auto-detect selected weapon
        if (Selection.activeGameObject != null)
        {
            var item = Selection.activeGameObject.GetComponentInParent<ThirdPersonPerspectiveItem>();
            if (item != null && item != m_SelectedItem)
            {
                m_SelectedItem = item;
                m_Position = item.LocalSpawnPosition;
                m_Rotation = item.LocalSpawnRotation;
            }
        }

        if (m_SelectedItem == null)
        {
            EditorGUILayout.HelpBox("Select a weapon in the Hierarchy (or any child of it).", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Selected:", m_SelectedItem.gameObject.name);
        GUILayout.Space(10);

        // Position
        GUILayout.Label("Local Spawn Position", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        m_Position = EditorGUILayout.Vector3Field("", m_Position);
        if (EditorGUI.EndChangeCheck())
        {
            ApplyPosition();
        }

        // Quick adjust buttons for position
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("X-")) { m_Position.x -= 0.01f; ApplyPosition(); }
        if (GUILayout.Button("X+")) { m_Position.x += 0.01f; ApplyPosition(); }
        if (GUILayout.Button("Y-")) { m_Position.y -= 0.01f; ApplyPosition(); }
        if (GUILayout.Button("Y+")) { m_Position.y += 0.01f; ApplyPosition(); }
        if (GUILayout.Button("Z-")) { m_Position.z -= 0.01f; ApplyPosition(); }
        if (GUILayout.Button("Z+")) { m_Position.z += 0.01f; ApplyPosition(); }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Rotation
        GUILayout.Label("Local Spawn Rotation (Euler)", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        m_Rotation = EditorGUILayout.Vector3Field("", m_Rotation);
        if (EditorGUI.EndChangeCheck())
        {
            ApplyRotation();
        }

        // Quick adjust buttons for rotation
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("X-")) { m_Rotation.x -= 5f; ApplyRotation(); }
        if (GUILayout.Button("X+")) { m_Rotation.x += 5f; ApplyRotation(); }
        if (GUILayout.Button("Y-")) { m_Rotation.y -= 5f; ApplyRotation(); }
        if (GUILayout.Button("Y+")) { m_Rotation.y += 5f; ApplyRotation(); }
        if (GUILayout.Button("Z-")) { m_Rotation.z -= 5f; ApplyRotation(); }
        if (GUILayout.Button("Z+")) { m_Rotation.z += 5f; ApplyRotation(); }
        GUILayout.EndHorizontal();

        GUILayout.Space(20);

        // Reset button
        if (GUILayout.Button("Reset to Zero"))
        {
            m_Position = Vector3.zero;
            m_Rotation = Vector3.zero;
            ApplyPosition();
            ApplyRotation();
        }

        GUILayout.Space(10);

        // Copy values button
        if (GUILayout.Button("Copy Values to Clipboard"))
        {
            m_CopiedValues = $"Position: ({m_Position.x:F4}, {m_Position.y:F4}, {m_Position.z:F4})\n" +
                           $"Rotation: ({m_Rotation.x:F2}, {m_Rotation.y:F2}, {m_Rotation.z:F2})";
            EditorGUIUtility.systemCopyBuffer = m_CopiedValues;
            Debug.Log($"[WeaponPositionAdjuster] Copied:\n{m_CopiedValues}");
        }

        if (!string.IsNullOrEmpty(m_CopiedValues))
        {
            GUILayout.Space(5);
            EditorGUILayout.HelpBox(m_CopiedValues, MessageType.None);
        }
    }

    private void ApplyPosition()
    {
        if (m_SelectedItem == null) return;

        // Use reflection to set the private field
        var field = typeof(ThirdPersonPerspectiveItem).GetField("m_LocalSpawnPosition",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(m_SelectedItem, m_Position);
        }

        // Apply to the actual object transform
        if (m_SelectedItem.Object != null)
        {
            m_SelectedItem.Object.transform.localPosition = m_Position;
        }
    }

    private void ApplyRotation()
    {
        if (m_SelectedItem == null) return;

        // Use reflection to set the private field
        var field = typeof(ThirdPersonPerspectiveItem).GetField("m_LocalSpawnRotation",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(m_SelectedItem, m_Rotation);
        }

        // Apply to the actual object transform
        if (m_SelectedItem.Object != null)
        {
            m_SelectedItem.Object.transform.localRotation = Quaternion.Euler(m_Rotation);
        }
    }

    private void OnInspectorUpdate()
    {
        // Repaint to keep UI updated
        Repaint();
    }
}
