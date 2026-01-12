using UnityEngine;
using Opsive.UltimateCharacterController.Traits;
using Opsive.Shared.Events;

/// <summary>
/// Manages player skills that affect various character stats.
/// Skills reset on death/respawn for roguelike progression.
/// </summary>
public class PlayerSkills : MonoBehaviour
{
    public static PlayerSkills Instance { get; private set; }

    [Header("Skill Levels")]
    [SerializeField] [Range(1, 10)] private int m_EnduranceLevel = 1;

    [Header("Endurance Settings")]
    [SerializeField] private float m_BaseMaxStamina = 100f;
    [SerializeField] private float m_StaminaPerLevel = 15f; // +15 max stamina per level

    [Header("Reset Settings")]
    [SerializeField] private bool m_ResetOnDeath = true;
    [SerializeField] private bool m_ResetOnRespawn = true;

    [Header("Debug")]
    [SerializeField] private bool m_LogChanges = true;

    // References
    private AttributeManager m_AttributeManager;
    private Opsive.UltimateCharacterController.Traits.Attribute m_StaminaAttribute;
    private bool m_EventsRegistered = false;

    // Properties
    public int EnduranceLevel
    {
        get => m_EnduranceLevel;
        set
        {
            int newLevel = Mathf.Clamp(value, 1, 10);
            if (newLevel != m_EnduranceLevel)
            {
                bool isLevelUp = newLevel > m_EnduranceLevel;
                m_EnduranceLevel = newLevel;
                ApplyEnduranceEffect();

                if (m_LogChanges)
                    Debug.Log($"[PlayerSkills] Endurance level changed to {m_EnduranceLevel}");

                // Show notification on level up (not on reset)
                if (isLevelUp)
                {
                    ShowEnduranceLevelUpNotification();
                }
            }
        }
    }

    /// <summary>
    /// Show the level up notification UI.
    /// </summary>
    private void ShowEnduranceLevelUpNotification()
    {
        // Try to find or create the notification UI
        var notificationUI = SkillNotificationUI.Instance;
        if (notificationUI == null)
        {
            // Auto-create if it doesn't exist
            GameObject notifObj = new GameObject("SkillNotificationUI");
            notificationUI = notifObj.AddComponent<SkillNotificationUI>();
        }

        notificationUI.ShowEnduranceLevelUp(m_EnduranceLevel, MaxStamina);
    }

    /// <summary>
    /// Get the current max stamina based on endurance level.
    /// </summary>
    public float MaxStamina => m_BaseMaxStamina + (m_EnduranceLevel - 1) * m_StaminaPerLevel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        FindReferences();
        RegisterEvents();
        ResetSkills(); // Start fresh each session
        ApplyAllSkillEffects();
    }

    private void OnDestroy()
    {
        UnregisterEvents();
    }

    /// <summary>
    /// Register for Opsive death/respawn events.
    /// </summary>
    private void RegisterEvents()
    {
        if (m_EventsRegistered) return;

        EventHandler.RegisterEvent<Vector3, Vector3, GameObject>(gameObject, "OnDeath", OnDeath);
        EventHandler.RegisterEvent(gameObject, "OnRespawn", OnRespawn);

        m_EventsRegistered = true;

        if (m_LogChanges)
            Debug.Log("[PlayerSkills] Registered for death/respawn events");
    }

    /// <summary>
    /// Unregister from events.
    /// </summary>
    private void UnregisterEvents()
    {
        if (!m_EventsRegistered) return;

        EventHandler.UnregisterEvent<Vector3, Vector3, GameObject>(gameObject, "OnDeath", OnDeath);
        EventHandler.UnregisterEvent(gameObject, "OnRespawn", OnRespawn);

        m_EventsRegistered = false;
    }

    /// <summary>
    /// Called when player dies.
    /// </summary>
    private void OnDeath(Vector3 position, Vector3 force, GameObject attacker)
    {
        if (m_LogChanges)
            Debug.Log($"[PlayerSkills] Player died! Attacker: {(attacker != null ? attacker.name : "none")}");

        if (m_ResetOnDeath)
        {
            ResetSkills();
        }
    }

    /// <summary>
    /// Called when player respawns.
    /// </summary>
    private void OnRespawn()
    {
        if (m_LogChanges)
            Debug.Log("[PlayerSkills] Player respawned!");

        if (m_ResetOnRespawn)
        {
            ResetSkills();
            ApplyAllSkillEffects();
        }
    }

    private void FindReferences()
    {
        // Find AttributeManager on this GameObject or player
        m_AttributeManager = GetComponent<AttributeManager>();
        if (m_AttributeManager == null)
        {
            m_AttributeManager = GetComponentInChildren<AttributeManager>();
        }

        if (m_AttributeManager != null)
        {
            m_StaminaAttribute = m_AttributeManager.GetAttribute("Stamina");
            if (m_StaminaAttribute == null)
            {
                Debug.LogWarning("[PlayerSkills] Stamina attribute not found on AttributeManager");
            }
        }
        else
        {
            Debug.LogWarning("[PlayerSkills] AttributeManager not found");
        }
    }

    /// <summary>
    /// Apply all skill effects. Called on start and when skills change.
    /// </summary>
    public void ApplyAllSkillEffects()
    {
        ApplyEnduranceEffect();
    }

    /// <summary>
    /// Apply the endurance skill effect (max stamina).
    /// </summary>
    private void ApplyEnduranceEffect()
    {
        if (m_StaminaAttribute == null)
        {
            // Try to find it again
            FindReferences();
            if (m_StaminaAttribute == null) return;
        }

        float newMaxStamina = MaxStamina;
        float oldMaxStamina = m_StaminaAttribute.MaxValue;

        // Use reflection to set MaxValue (same pattern as StaminaSystem)
        var type = typeof(Opsive.UltimateCharacterController.Traits.Attribute);
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var maxValueField = type.GetField("m_MaxValue", flags);

        if (maxValueField != null)
        {
            maxValueField.SetValue(m_StaminaAttribute, newMaxStamina);

            // If current value is higher than old max, scale it proportionally
            if (oldMaxStamina > 0 && m_StaminaAttribute.Value > 0)
            {
                float ratio = m_StaminaAttribute.Value / oldMaxStamina;
                var valueField = type.GetField("m_Value", flags);
                if (valueField != null)
                {
                    valueField.SetValue(m_StaminaAttribute, newMaxStamina * ratio);
                }
            }

            if (m_LogChanges)
                Debug.Log($"[PlayerSkills] Max stamina set to {newMaxStamina} (Endurance Lv.{m_EnduranceLevel})");
        }
    }

    /// <summary>
    /// Increase endurance by 1 level.
    /// </summary>
    public void IncreaseEndurance()
    {
        EnduranceLevel++;
    }

    /// <summary>
    /// Decrease endurance by 1 level.
    /// </summary>
    public void DecreaseEndurance()
    {
        EnduranceLevel--;
    }

    /// <summary>
    /// Set endurance to a specific level.
    /// </summary>
    public void SetEndurance(int level)
    {
        EnduranceLevel = level;
    }

    /// <summary>
    /// Reset all skills to level 1. Called on death/respawn.
    /// </summary>
    public void ResetSkills()
    {
        m_EnduranceLevel = 1;

        if (m_LogChanges)
            Debug.Log("[PlayerSkills] Skills reset to level 1");
    }

    /// <summary>
    /// Get skill info for UI display.
    /// </summary>
    public SkillInfo GetEnduranceInfo()
    {
        return new SkillInfo
        {
            name = "Endurance",
            description = "Increases maximum stamina",
            level = m_EnduranceLevel,
            maxLevel = 10,
            currentEffect = $"+{(m_EnduranceLevel - 1) * m_StaminaPerLevel} Max Stamina",
            nextLevelEffect = m_EnduranceLevel < 10 ? $"+{m_StaminaPerLevel} Max Stamina" : "MAX"
        };
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Apply changes when editing in Inspector during play mode
        if (Application.isPlaying && m_StaminaAttribute != null)
        {
            ApplyEnduranceEffect();
        }
    }
#endif
}

/// <summary>
/// Info struct for displaying skill data in UI.
/// </summary>
[System.Serializable]
public struct SkillInfo
{
    public string name;
    public string description;
    public int level;
    public int maxLevel;
    public string currentEffect;
    public string nextLevelEffect;
}
