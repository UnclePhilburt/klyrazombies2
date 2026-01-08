using UnityEngine;
using Opsive.Shared.Events;
using Opsive.UltimateCharacterController.Items.Actions;

/// <summary>
/// Add this to weapon prefabs to report gunshots when fired.
/// More reliable than character-level detection.
/// </summary>
public class WeaponGunshotReporter : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool m_IsSilenced = false;
    [SerializeField] private float m_NoiseRange = 50f;
    [SerializeField] private bool m_DebugMode = true;

    private ShootableAction m_ShootableAction;
    private CharacterItemAction m_ItemAction;
    private GameObject m_Character;
    private bool m_WasUsing = false;

    private void Awake()
    {
        m_ShootableAction = GetComponent<ShootableAction>();
        m_ItemAction = GetComponent<CharacterItemAction>();
    }

    private void Start()
    {
        // Find the character this weapon belongs to
        if (m_ItemAction != null && m_ItemAction.CharacterItem != null)
        {
            m_Character = m_ItemAction.CharacterItem.Character;
        }

        // Register for fire events on this weapon
        EventHandler.RegisterEvent(gameObject, "OnShootableWeaponFire", OnFire);
        EventHandler.RegisterEvent<bool>(gameObject, "OnUseStart", OnUseStart);

        if (m_DebugMode)
            Debug.Log($"[WeaponGunshotReporter] Initialized on weapon: {gameObject.name}");
    }

    private void OnDestroy()
    {
        EventHandler.UnregisterEvent(gameObject, "OnShootableWeaponFire", OnFire);
        EventHandler.UnregisterEvent<bool>(gameObject, "OnUseStart", OnUseStart);
    }

    private void OnUseStart(bool start)
    {
        if (start)
        {
            OnFire();
        }
    }

    private void OnFire()
    {
        Vector3 position = transform.position;

        // Try to get character position instead
        if (m_Character != null)
        {
            position = m_Character.transform.position;
        }

        if (m_DebugMode)
            Debug.Log($"[WeaponGunshotReporter] {gameObject.name} fired! Position: {position}");

        if (ZombieManager.Instance != null)
        {
            ZombieManager.FireGunshot(position, m_IsSilenced);
        }
    }

    // Called by ShootableAction when it fires (if using Unity Events)
    public void OnWeaponFired()
    {
        OnFire();
    }
}
