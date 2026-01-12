using UnityEngine;
using Opsive.UltimateCharacterController.Camera;
using Opsive.UltimateCharacterController.Camera.ViewTypes;
using Opsive.Shared.Events;

/// <summary>
/// Unified camera controller for smooth transitions between hip fire and ADS.
/// This script is the SINGLE SOURCE OF TRUTH for camera offsets.
/// Opsive's zoom preset should only handle FOV, not LookOffset.
/// </summary>
public class SmoothCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CameraController m_CameraController;

    [Header("Camera Positions")]
    [Tooltip("Camera offset when NOT aiming (hip fire)")]
    [SerializeField] private Vector3 m_HipFireOffset = new Vector3(0.5f, -0.3f, -1.8f);

    [Tooltip("Camera offset when aiming down sights")]
    [SerializeField] private Vector3 m_AdsOffset = new Vector3(0.5f, 0.1f, -1.3f);

    [Header("Smoothing Settings")]
    [Tooltip("Camera smoothing when NOT aiming (higher = smoother/laggier)")]
    [Range(0f, 0.5f)]
    [SerializeField] private float m_HipFireSmoothing = 0.15f;

    [Tooltip("Camera smoothing when aiming (lower = snappier)")]
    [Range(0f, 0.1f)]
    [SerializeField] private float m_AdsSmoothing = 0.02f;

    [Header("Transition Settings")]
    [Tooltip("Smooth time for camera position transition (lower = faster)")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float m_OffsetSmoothTime = 0.15f;

    [Tooltip("Smooth time for smoothing value transition")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float m_SmoothingSmoothTime = 0.1f;

    private ViewType m_ActiveViewType;
    private bool m_IsAiming;
    private float m_TargetSmoothing;
    private float m_CurrentSmoothing;

    // Offset interpolation
    private Vector3 m_CurrentLookOffset;
    private Vector3 m_TargetLookOffset;

    // SmoothDamp velocities
    private Vector3 m_OffsetVelocity;
    private float m_SmoothingVelocity;

    // Reflection cache
    private System.Reflection.FieldInfo m_LookOffsetSmoothingField;
    private System.Reflection.FieldInfo m_LookOffsetField;

    private void Start()
    {
        if (m_CameraController == null)
            m_CameraController = GetComponent<CameraController>();

        if (m_CameraController == null)
        {
            Debug.LogError("[SmoothCamera] No CameraController found!");
            enabled = false;
            return;
        }

        // Cache the active view type
        m_ActiveViewType = m_CameraController.ActiveViewType;

        // Get fields via reflection (they're protected)
        CacheViewTypeFields();

        // Initialize to hip fire state
        m_CurrentSmoothing = m_HipFireSmoothing;
        m_TargetSmoothing = m_HipFireSmoothing;
        m_CurrentLookOffset = m_HipFireOffset;
        m_TargetLookOffset = m_HipFireOffset;

        // Apply initial offset immediately
        if (m_LookOffsetField != null)
        {
            m_LookOffsetField.SetValue(m_ActiveViewType, m_HipFireOffset);
        }

        // Listen for aim events from Opsive
        if (m_CameraController.Character != null)
        {
            EventHandler.RegisterEvent<bool>(m_CameraController.Character, "OnAimAbilityAim", OnAim);
        }
    }

    private void CacheViewTypeFields()
    {
        if (m_ActiveViewType == null) return;

        var viewType = m_ActiveViewType.GetType();

        // Get smoothing field
        m_LookOffsetSmoothingField = viewType.GetField("m_LookOffsetSmoothing",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (m_LookOffsetSmoothingField != null)
        {
            m_CurrentSmoothing = (float)m_LookOffsetSmoothingField.GetValue(m_ActiveViewType);
        }

        // Get look offset field
        m_LookOffsetField = viewType.GetField("m_LookOffset",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    }

    private void OnDestroy()
    {
        if (m_CameraController != null && m_CameraController.Character != null)
        {
            EventHandler.UnregisterEvent<bool>(m_CameraController.Character, "OnAimAbilityAim", OnAim);
        }
    }

    private void OnAim(bool isAiming)
    {
        m_IsAiming = isAiming;
        m_TargetSmoothing = isAiming ? m_AdsSmoothing : m_HipFireSmoothing;
        m_TargetLookOffset = isAiming ? m_AdsOffset : m_HipFireOffset;
    }

    private void LateUpdate()
    {
        if (m_ActiveViewType == null)
            return;

        // Check if view type changed
        if (m_CameraController.ActiveViewType != m_ActiveViewType)
        {
            m_ActiveViewType = m_CameraController.ActiveViewType;
            CacheViewTypeFields();
        }

        // Use SmoothDamp for buttery smooth transitions
        m_CurrentSmoothing = Mathf.SmoothDamp(m_CurrentSmoothing, m_TargetSmoothing, ref m_SmoothingVelocity, m_SmoothingSmoothTime);
        m_CurrentLookOffset = Vector3.SmoothDamp(m_CurrentLookOffset, m_TargetLookOffset, ref m_OffsetVelocity, m_OffsetSmoothTime);

        // Apply smoothing to view type
        if (m_LookOffsetSmoothingField != null)
        {
            m_LookOffsetSmoothingField.SetValue(m_ActiveViewType, m_CurrentSmoothing);
        }

        // Apply offset to view type (this overrides any Opsive preset changes)
        if (m_LookOffsetField != null)
        {
            m_LookOffsetField.SetValue(m_ActiveViewType, m_CurrentLookOffset);
        }
    }

    // Manual control for testing
    public void SetAiming(bool aiming)
    {
        OnAim(aiming);
    }
}
