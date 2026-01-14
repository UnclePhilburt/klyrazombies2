using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Debug helper for dog animation issues.
/// Add this to the dog to see what's wrong.
/// </summary>
public class DogCompanionDebug : MonoBehaviour
{
    private DogCompanion m_Companion;
    private NavMeshAgent m_Agent;
    private Animator m_Animator;

    private void Start()
    {
        m_Companion = GetComponent<DogCompanion>();
        m_Agent = GetComponent<NavMeshAgent>();
        m_Animator = GetComponentInChildren<Animator>();

        Debug.Log("=== DOG COMPANION DEBUG ===");
        Debug.Log($"DogCompanion: {(m_Companion != null ? "✓" : "✗ MISSING")}");
        Debug.Log($"NavMeshAgent: {(m_Agent != null ? "✓" : "✗ MISSING")}");
        Debug.Log($"Animator: {(m_Animator != null ? "✓" : "✗ MISSING")}");

        if (m_Animator != null)
        {
            Debug.Log($"Animator Controller: {(m_Animator.runtimeAnimatorController != null ? m_Animator.runtimeAnimatorController.name : "✗ MISSING")}");
            Debug.Log($"Animator Enabled: {m_Animator.enabled}");
            Debug.Log($"Animator GameObject Active: {m_Animator.gameObject.activeInHierarchy}");

            // List all parameters
            Debug.Log("Animator Parameters:");
            foreach (var param in m_Animator.parameters)
            {
                Debug.Log($"  - {param.name} ({param.type})");
            }
        }

        if (m_Agent != null)
        {
            Debug.Log($"NavMeshAgent On NavMesh: {m_Agent.isOnNavMesh}");
            Debug.Log($"NavMeshAgent Enabled: {m_Agent.enabled}");
        }

        // Check if there's a NavMesh in the scene
        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        Debug.Log($"NavMesh Vertices: {triangulation.vertices.Length}");
        if (triangulation.vertices.Length == 0)
        {
            Debug.LogError("✗ NO NAVMESH FOUND! The dog cannot move. Please bake a NavMesh: Window > AI > Navigation");
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(Screen.width - 310, 10, 300, 400));
        GUILayout.BeginVertical("box");

        GUILayout.Label("=== DOG DEBUG ===", GUI.skin.GetStyle("boldLabel"));

        if (m_Companion != null)
        {
            GUILayout.Label($"State: {m_Companion.CurrentState}");
            GUILayout.Label($"Owner: {(m_Companion.Owner != null ? m_Companion.Owner.name : "NONE")}");
        }

        if (m_Agent != null)
        {
            GUILayout.Label($"On NavMesh: {m_Agent.isOnNavMesh}");
            GUILayout.Label($"Speed: {m_Agent.speed:F2}");
            GUILayout.Label($"Velocity: {m_Agent.velocity.magnitude:F2}");
            GUILayout.Label($"Has Path: {m_Agent.hasPath}");

            if (!m_Agent.isOnNavMesh)
            {
                GUI.color = Color.red;
                GUILayout.Label("✗ NOT ON NAVMESH!");
                GUI.color = Color.white;
            }
        }

        if (m_Animator != null)
        {
            GUILayout.Label($"Controller: {(m_Animator.runtimeAnimatorController != null ? "✓" : "✗")}");
            GUILayout.Label($"Enabled: {m_Animator.enabled}");

            // Show Movement parameter value
            float movement = m_Animator.GetFloat("Movement_f");
            GUILayout.Label($"Movement_f: {movement:F2}");

            if (m_Animator.runtimeAnimatorController == null)
            {
                GUI.color = Color.red;
                GUILayout.Label("✗ NO ANIMATOR CONTROLLER!");
                GUI.color = Color.white;

                if (GUILayout.Button("Try to Fix Animator"))
                {
                    TryFixAnimator();
                }
            }
        }
        else
        {
            GUI.color = Color.red;
            GUILayout.Label("✗ NO ANIMATOR FOUND!");
            GUI.color = Color.white;
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void TryFixAnimator()
    {
        if (m_Animator == null)
        {
            m_Animator = GetComponentInChildren<Animator>();
        }

        if (m_Animator != null && m_Animator.runtimeAnimatorController == null)
        {
            // Try to load the controller from PolygonDog
            var controller = Resources.Load<RuntimeAnimatorController>("PolygonDog/Animations/Animation_Dog");
            if (controller == null)
            {
                // Try direct asset path (editor only)
#if UNITY_EDITOR
                controller = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    "Assets/PolygonDog/Animations/Animation_Dog.controller");
#endif
            }

            if (controller != null)
            {
                m_Animator.runtimeAnimatorController = controller;
                Debug.Log("✓ Fixed animator controller!");
            }
            else
            {
                Debug.LogError("✗ Could not find Animation_Dog controller!");
            }
        }
    }
}
