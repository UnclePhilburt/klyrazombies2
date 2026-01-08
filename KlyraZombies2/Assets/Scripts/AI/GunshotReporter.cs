using UnityEngine;
using Opsive.Shared.Events;
using Opsive.UltimateCharacterController.Items.Actions;

/// <summary>
/// Reports gunshots to the ZombieManager when weapons are fired.
/// Attach to the player character.
/// </summary>
public class GunshotReporter : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool m_ReportGunshots = true;
    [SerializeField] private float m_BaseNoiseRange = 50f;
    [SerializeField] private bool m_DebugMode = true;
    [SerializeField] private float m_MinTimeBetweenReports = 0.1f;

    [Header("Weapon Silencer Detection")]
    [Tooltip("If the equipped weapon has this in its name, treat as silenced")]
    [SerializeField] private string[] m_SilencedKeywords = { "silenced", "suppressed", "quiet" };

    private float m_LastReportTime;

    private void Start()
    {
        // Check if ZombieManager exists
        if (ZombieManager.Instance == null)
        {
            Debug.LogWarning("[GunshotReporter] ZombieManager not found in scene! Add a GameObject with ZombieManager component.");
        }

        // Register for Opsive weapon fire events with correct signatures
        EventHandler.RegisterEvent(gameObject, "OnShootableWeaponFire", OnWeaponFire);
        EventHandler.RegisterEvent<IUsableItem>(gameObject, "OnItemUseComplete", OnItemUseComplete);
        EventHandler.RegisterEvent<bool>(gameObject, "OnAttack", OnAttack);

        if (m_DebugMode)
            Debug.Log($"[GunshotReporter] Initialized on {gameObject.name}");
    }

    private void OnDestroy()
    {
        EventHandler.UnregisterEvent(gameObject, "OnShootableWeaponFire", OnWeaponFire);
        EventHandler.UnregisterEvent<IUsableItem>(gameObject, "OnItemUseComplete", OnItemUseComplete);
        EventHandler.UnregisterEvent<bool>(gameObject, "OnAttack", OnAttack);
    }

    private void OnItemUseComplete(IUsableItem usableItem)
    {
        OnWeaponFire();
    }

    private void OnAttack(bool attack)
    {
        if (attack)
        {
            OnWeaponFire();
        }
    }

    /// <summary>
    /// Called when the player fires a weapon
    /// </summary>
    private void OnWeaponFire()
    {
        if (!m_ReportGunshots) return;

        // Prevent spam - don't report more than once per interval
        if (Time.time - m_LastReportTime < m_MinTimeBetweenReports)
            return;

        m_LastReportTime = Time.time;

        if (m_DebugMode)
            Debug.Log($"[GunshotReporter] Weapon fired! Reporting gunshot at {transform.position}");

        ReportGunshot();
    }

    /// <summary>
    /// Public method - can be called from animation events or other scripts
    /// </summary>
    public void ReportGunshot()
    {
        if (!m_ReportGunshots) return;

        if (ZombieManager.Instance != null)
        {
            ZombieManager.FireGunshot(transform.position, IsSilencedWeaponEquipped());
        }
        else if (m_DebugMode)
        {
            Debug.LogWarning("[GunshotReporter] ZombieManager.Instance is null! Gunshot not reported.");
        }
    }

    private bool IsSilencedWeaponEquipped()
    {
        return false; // Default to not silenced
    }

    // Manual test button in inspector
    [ContextMenu("Test Report Gunshot")]
    private void TestReportGunshot()
    {
        Debug.Log("[GunshotReporter] TEST: Manually reporting gunshot");
        m_DebugMode = true;
        ReportGunshot();
    }
}
