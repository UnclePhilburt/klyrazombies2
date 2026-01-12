using UnityEngine;

/// <summary>
/// Trigger zone that shows a discovery popup when the player enters.
/// Place this in the world at points of interest.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class POITrigger : MonoBehaviour
{
    [Header("POI Data")]
    [SerializeField] private POIData m_POIData;

    [Header("Trigger Settings")]
    [SerializeField] private float m_TriggerRadius = 15f;
    [SerializeField] private bool m_ShowOnlyOnce = true;

    [Header("Gizmo")]
    [SerializeField] private bool m_ShowGizmo = true;
    [SerializeField] private Color m_GizmoColor = new Color(0f, 1f, 0.5f, 0.3f);

    private SphereCollider m_Collider;
    private bool m_HasTriggered = false;

    private void Awake()
    {
        SetupCollider();
    }

    private void Reset()
    {
        // Called when component is first added
        SetupCollider();
    }

    private void SetupCollider()
    {
        m_Collider = GetComponent<SphereCollider>();
        if (m_Collider == null)
            m_Collider = gameObject.AddComponent<SphereCollider>();

        m_Collider.isTrigger = true;
        m_Collider.radius = m_TriggerRadius;
    }

    private void OnValidate()
    {
        // Update collider radius when changed in inspector
        if (m_Collider != null)
            m_Collider.radius = m_TriggerRadius;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[POITrigger] OnTriggerEnter: {other.name} entered {gameObject.name}");

        // Check if this is the player
        if (!IsPlayer(other))
        {
            Debug.Log($"[POITrigger] {other.name} is not the player, ignoring");
            return;
        }

        Debug.Log($"[POITrigger] Player detected! HasTriggered={m_HasTriggered}, ShowOnlyOnce={m_ShowOnlyOnce}");

        // Check if already triggered
        if (m_ShowOnlyOnce && m_HasTriggered)
        {
            Debug.Log($"[POITrigger] Already triggered this session, skipping");
            return;
        }

        // Show the discovery
        if (m_POIData != null)
        {
            // Create popup instance if it doesn't exist
            if (POIDiscoveryPopup.Instance == null)
            {
                Debug.Log($"[POITrigger] Creating POIDiscoveryPopup instance");
                CreatePopupInstance();
            }

            if (POIDiscoveryPopup.Instance != null)
            {
                bool alreadyDiscovered = POIDiscoveryPopup.Instance.IsDiscovered(m_POIData.poiId);
                Debug.Log($"[POITrigger] POI '{m_POIData.displayName}' (id={m_POIData.poiId}) alreadyDiscovered={alreadyDiscovered}");

                bool shown = POIDiscoveryPopup.Instance.Show(m_POIData);
                if (shown)
                {
                    m_HasTriggered = true;
                    Debug.Log($"[POITrigger] SUCCESS - Showing discovery popup for: {m_POIData.displayName}");
                }
                else
                {
                    Debug.Log($"[POITrigger] Popup not shown (already discovered in PlayerPrefs)");
                }
            }
            else
            {
                Debug.LogError($"[POITrigger] Failed to create POIDiscoveryPopup instance!");
            }
        }
        else
        {
            Debug.LogWarning($"[POITrigger] No POI data assigned to {gameObject.name}");
        }
    }

    private bool IsPlayer(Collider other)
    {
        // Check for player tag
        if (other.CompareTag("Player")) return true;

        // Check for Opsive character locomotion
        var locomotion = other.GetComponentInParent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();
        if (locomotion != null) return true;

        // Check parent for player tag
        if (other.transform.root.CompareTag("Player")) return true;

        return false;
    }

    private void CreatePopupInstance()
    {
        GameObject popupObj = new GameObject("POIDiscoveryPopup");
        popupObj.AddComponent<POIDiscoveryPopup>();
        DontDestroyOnLoad(popupObj);
    }

    /// <summary>
    /// Resets the trigger so it can fire again.
    /// </summary>
    public void ResetTrigger()
    {
        m_HasTriggered = false;
    }

    /// <summary>
    /// Sets the POI data at runtime.
    /// </summary>
    public void SetPOIData(POIData data)
    {
        m_POIData = data;
    }

    private void OnDrawGizmos()
    {
        if (!m_ShowGizmo) return;

        Gizmos.color = m_GizmoColor;
        Gizmos.DrawSphere(transform.position, m_TriggerRadius);

        // Draw wireframe
        Gizmos.color = new Color(m_GizmoColor.r, m_GizmoColor.g, m_GizmoColor.b, 1f);
        Gizmos.DrawWireSphere(transform.position, m_TriggerRadius);

        // Draw label if POI data assigned
        if (m_POIData != null)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, m_POIData.displayName);
#endif
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Brighter when selected
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.5f);
        Gizmos.DrawSphere(transform.position, m_TriggerRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, m_TriggerRadius);
    }

    public POIData POIData => m_POIData;
    public float TriggerRadius => m_TriggerRadius;
    public bool HasTriggered => m_HasTriggered;
}
