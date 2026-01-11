using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Simple ragdoll handler for zombie death.
/// Uses existing ragdoll rigidbodies if present on the model.
/// If no ragdoll exists, just disables animator and lets physics take over.
/// </summary>
public class ZombieRagdoll : MonoBehaviour
{
    [Header("Ragdoll Settings")]
    [Tooltip("Force applied in the hit direction on death")]
    [SerializeField] private float m_DeathForce = 50f;

    // Cached components
    private Animator m_Animator;
    private List<Rigidbody> m_RagdollBodies = new List<Rigidbody>();
    private List<Collider> m_RagdollColliders = new List<Collider>();
    private Collider m_MainCollider;
    private Rigidbody m_MainRigidbody;
    private bool m_IsRagdolled = false;

    private void Awake()
    {
        m_Animator = GetComponentInChildren<Animator>();
        m_MainCollider = GetComponent<Collider>();
        m_MainRigidbody = GetComponent<Rigidbody>();

        // Find existing ragdoll rigidbodies on child bones
        FindExistingRagdoll();

        // Disable ragdoll initially (make kinematic)
        SetRagdollEnabled(false);
    }

    private void FindExistingRagdoll()
    {
        var childBodies = GetComponentsInChildren<Rigidbody>();
        foreach (var rb in childBodies)
        {
            // Skip the main rigidbody on root
            if (rb.gameObject == gameObject) continue;

            m_RagdollBodies.Add(rb);

            var col = rb.GetComponent<Collider>();
            if (col != null)
                m_RagdollColliders.Add(col);
        }
    }

    /// <summary>
    /// Activate ragdoll physics
    /// </summary>
    public void EnableRagdoll(Vector3 hitDirection = default, Vector3 hitPoint = default)
    {
        if (m_IsRagdolled) return;
        m_IsRagdolled = true;

        // Disable animator
        if (m_Animator != null)
        {
            m_Animator.enabled = false;
        }

        // If we have existing ragdoll bodies, enable them
        if (m_RagdollBodies.Count > 0)
        {
            SetRagdollEnabled(true);

            // Apply death force to body near hit point
            if (hitDirection != Vector3.zero)
            {
                ApplyDeathForce(hitDirection, hitPoint);
            }
        }

        // Disable main collider
        if (m_MainCollider != null)
        {
            m_MainCollider.enabled = false;
        }
    }

    private void SetRagdollEnabled(bool enabled)
    {
        foreach (var rb in m_RagdollBodies)
        {
            if (rb != null)
            {
                rb.isKinematic = !enabled;
                rb.detectCollisions = enabled;
            }
        }

        foreach (var col in m_RagdollColliders)
        {
            if (col != null)
            {
                col.enabled = enabled;
            }
        }
    }

    private void ApplyDeathForce(Vector3 hitDirection, Vector3 hitPoint)
    {
        // Find the rigidbody closest to the hit point
        Rigidbody closestRb = null;
        float closestDist = float.MaxValue;

        foreach (var rb in m_RagdollBodies)
        {
            if (rb == null) continue;
            float dist = Vector3.Distance(rb.position, hitPoint);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestRb = rb;
            }
        }

        // Apply force to closest bone
        if (closestRb != null)
        {
            Vector3 force = hitDirection.normalized * m_DeathForce;
            closestRb.AddForce(force, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// Check if this zombie has a pre-configured ragdoll
    /// </summary>
    public bool HasRagdoll => m_RagdollBodies.Count > 0;

    /// <summary>
    /// Static helper to enable ragdoll on any zombie
    /// </summary>
    public static void EnableRagdollOnZombie(GameObject zombie, Vector3 hitDirection = default, Vector3 hitPoint = default)
    {
        var ragdoll = zombie.GetComponent<ZombieRagdoll>();
        if (ragdoll == null)
        {
            ragdoll = zombie.AddComponent<ZombieRagdoll>();
        }

        ragdoll.EnableRagdoll(hitDirection, hitPoint);
    }
}
