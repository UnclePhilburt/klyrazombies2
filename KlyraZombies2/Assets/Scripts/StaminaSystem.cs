using UnityEngine;
using Opsive.Shared.Events;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities;
using Opsive.UltimateCharacterController.Traits;

/// <summary>
/// Manages stamina drain during sprinting using Opsive's Attribute system.
/// Requires an AttributeManager with a "Stamina" attribute on the character.
/// </summary>
public class StaminaSystem : MonoBehaviour
{
    [Header("Stamina Settings")]
    [SerializeField] private string m_StaminaAttributeName = "Stamina";
    [SerializeField] private float m_DrainRate = 15f; // Stamina per second while sprinting
    [SerializeField] private float m_MinStaminaToSprint = 5f; // Minimum stamina needed to start sprinting

    [Header("Endurance Leveling")]
    [SerializeField] private float m_BaseXPToLevel = 200f; // Stamina drained to reach level 2
    [SerializeField] private float m_XPMultiplierPerLevel = 1.5f; // Each level needs 1.5x more
    [SerializeField] private bool m_ShowXPProgress = true;

    [Header("Heavy Breathing")]
    [SerializeField] private AudioClip m_HeavyBreathingClip;
    [SerializeField] private float m_BreathingVolume = 0.7f;
    [SerializeField] private float m_StartBreathingThreshold = 0.1f; // Start breathing below 10% stamina
    [SerializeField] private float m_StopBreathingThreshold = 0.5f; // Stop breathing when stamina reaches 50%

    [Header("References")]
    [SerializeField] private AttributeManager m_AttributeManager;

    private UltimateCharacterLocomotion m_Locomotion;
    private SpeedChange m_SprintAbility;
    private Attribute m_StaminaAttribute;
    private bool m_IsSprinting;
    private AudioSource m_BreathingAudioSource;
    private bool m_IsBreathingHeavily;

    // Endurance XP tracking
    private float m_CurrentXP = 0f;
    private float m_XPToNextLevel;
    private PlayerSkills m_PlayerSkills;

    // Public access for UI
    public float CurrentXP => m_CurrentXP;
    public float XPToNextLevel => m_XPToNextLevel;
    public float XPProgress => m_XPToNextLevel > 0 ? m_CurrentXP / m_XPToNextLevel : 0f;

    private void Start()
    {
        StartCoroutine(Initialize());
    }

    private System.Collections.IEnumerator Initialize()
    {
        // Wait a frame for Opsive to initialize
        yield return null;

        m_Locomotion = GetComponent<UltimateCharacterLocomotion>();

        if (m_AttributeManager == null)
        {
            // Try multiple ways to find the AttributeManager
            m_AttributeManager = GetComponent<AttributeManager>();
            if (m_AttributeManager == null)
                m_AttributeManager = GetComponentInChildren<AttributeManager>();
            if (m_AttributeManager == null)
                m_AttributeManager = GetComponentInParent<AttributeManager>();
        }

        if (m_AttributeManager == null)
        {
            Debug.LogError("StaminaSystem: No AttributeManager found! Add one to the character.");
            enabled = false;
            yield break;
        }

        // Try to get attribute
        m_StaminaAttribute = m_AttributeManager.GetAttribute(m_StaminaAttributeName);

        // If Stamina doesn't exist, create it
        if (m_StaminaAttribute == null)
        {
            Debug.Log("StaminaSystem: Creating Stamina attribute at runtime...");

            // Create new Stamina attribute with just name and value
            var newAttribute = new Attribute(m_StaminaAttributeName, 100);

            // Initialize FIRST before setting any properties (required for events)
            newAttribute.Initialize(gameObject);

            // Now set properties using reflection to avoid triggering events
            var type = typeof(Attribute);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            type.GetField("m_MinValue", flags)?.SetValue(newAttribute, 0f);
            type.GetField("m_MaxValue", flags)?.SetValue(newAttribute, 100f);
            type.GetField("m_Value", flags)?.SetValue(newAttribute, 100f);
            type.GetField("m_AutoUpdateValueType", flags)?.SetValue(newAttribute, Attribute.AutoUpdateValue.Increase);
            type.GetField("m_AutoUpdateStartDelay", flags)?.SetValue(newAttribute, 1f);
            type.GetField("m_AutoUpdateInterval", flags)?.SetValue(newAttribute, 0.1f);
            type.GetField("m_AutoUpdateAmount", flags)?.SetValue(newAttribute, 5f);

            // Add to the AttributeManager's array
            var currentAttributes = m_AttributeManager.Attributes;
            var newAttributes = new Attribute[currentAttributes.Length + 1];
            for (int i = 0; i < currentAttributes.Length; i++)
            {
                newAttributes[i] = currentAttributes[i];
            }
            newAttributes[currentAttributes.Length] = newAttribute;

            // Use reflection to set the private field
            var field = typeof(AttributeManager).GetField("m_Attributes", flags);
            if (field != null)
            {
                field.SetValue(m_AttributeManager, newAttributes);
            }

            // Rebuild the name map
            var mapField = typeof(AttributeManager).GetField("m_NameAttributeMap", flags);
            if (mapField != null)
            {
                var map = mapField.GetValue(m_AttributeManager) as System.Collections.Generic.Dictionary<string, Attribute>;
                if (map != null && !map.ContainsKey(m_StaminaAttributeName))
                {
                    map.Add(m_StaminaAttributeName, newAttribute);
                }
            }

            m_StaminaAttribute = newAttribute;
        }

        if (m_StaminaAttribute == null)
        {
            Debug.LogError("StaminaSystem: Failed to create Stamina attribute!");
            enabled = false;
            yield break;
        }

        Debug.Log($"StaminaSystem: Found Stamina attribute with value {m_StaminaAttribute.Value}");

        if (m_Locomotion != null)
        {
            m_SprintAbility = m_Locomotion.GetAbility<SpeedChange>();
        }

        // Register for sprint ability events
        EventHandler.RegisterEvent<Ability, bool>(gameObject, "OnCharacterAbilityActive", OnAbilityActive);

        // Setup breathing audio source
        SetupBreathingAudio();

        // Find PlayerSkills and setup XP tracking
        m_PlayerSkills = GetComponent<PlayerSkills>();
        if (m_PlayerSkills == null)
            m_PlayerSkills = GetComponentInChildren<PlayerSkills>();

        CalculateXPToNextLevel();

        // Register for respawn to reset XP
        EventHandler.RegisterEvent(gameObject, "OnRespawn", OnRespawn);
    }

    /// <summary>
    /// Calculate XP needed for next endurance level.
    /// </summary>
    private void CalculateXPToNextLevel()
    {
        int currentLevel = m_PlayerSkills != null ? m_PlayerSkills.EnduranceLevel : 1;

        if (currentLevel >= 10)
        {
            m_XPToNextLevel = 0; // Max level
            return;
        }

        // XP needed scales with level: base * multiplier^(level-1)
        m_XPToNextLevel = m_BaseXPToLevel * Mathf.Pow(m_XPMultiplierPerLevel, currentLevel - 1);
    }

    /// <summary>
    /// Called when player respawns - reset XP progress.
    /// </summary>
    private void OnRespawn()
    {
        m_CurrentXP = 0f;
        CalculateXPToNextLevel();
        Debug.Log("[StaminaSystem] XP reset on respawn");
    }

    private void SetupBreathingAudio()
    {
        if (m_HeavyBreathingClip == null) return;

        m_BreathingAudioSource = gameObject.AddComponent<AudioSource>();
        m_BreathingAudioSource.clip = m_HeavyBreathingClip;
        m_BreathingAudioSource.loop = true;
        m_BreathingAudioSource.playOnAwake = false;
        m_BreathingAudioSource.volume = m_BreathingVolume;
        m_BreathingAudioSource.spatialBlend = 0f; // 2D sound (player's own breathing)
    }

    private void OnDestroy()
    {
        EventHandler.UnregisterEvent<Ability, bool>(gameObject, "OnCharacterAbilityActive", OnAbilityActive);
        EventHandler.UnregisterEvent(gameObject, "OnRespawn", OnRespawn);
    }

    private void Update()
    {
        if (m_StaminaAttribute == null) return;

        if (m_IsSprinting)
        {
            // Calculate drain amount
            float drainAmount = m_DrainRate * Time.deltaTime;

            // Drain stamina while sprinting
            m_StaminaAttribute.Value -= drainAmount;

            // Add XP for endurance (amount drained = XP gained)
            AddEnduranceXP(drainAmount);

            // Stop sprinting if stamina is depleted
            if (m_StaminaAttribute.Value <= 0 && m_SprintAbility != null && m_SprintAbility.IsActive)
            {
                m_Locomotion.TryStopAbility(m_SprintAbility);
            }
        }

        // Handle heavy breathing
        UpdateBreathing();

        // Try to find PlayerSkills if we don't have it
        if (m_PlayerSkills == null)
        {
            // Try on same GameObject
            m_PlayerSkills = GetComponent<PlayerSkills>();

            // Try on parent
            if (m_PlayerSkills == null)
                m_PlayerSkills = GetComponentInParent<PlayerSkills>();

            // Try static instance
            if (m_PlayerSkills == null)
                m_PlayerSkills = PlayerSkills.Instance;

            // Try finding any in scene
            if (m_PlayerSkills == null)
                m_PlayerSkills = FindFirstObjectByType<PlayerSkills>();

            // Auto-add if not found anywhere
            if (m_PlayerSkills == null)
            {
                m_PlayerSkills = gameObject.AddComponent<PlayerSkills>();
                Debug.Log("[StaminaSystem] Auto-added PlayerSkills component");
            }

            if (m_PlayerSkills != null)
            {
                CalculateXPToNextLevel();
                Debug.Log($"[StaminaSystem] PlayerSkills ready on {m_PlayerSkills.gameObject.name}");
            }
        }

        // Debug: Press F2 to see stamina status
        if (Input.GetKeyDown(KeyCode.F2))
        {
            float normalized = GetStaminaNormalized();
            int level = m_PlayerSkills != null ? m_PlayerSkills.EnduranceLevel : 1;
            bool sprintAbilityActive = m_SprintAbility != null && m_SprintAbility.IsActive;
            Debug.Log($"[Stamina Debug] Value: {m_StaminaAttribute.Value:F0}/{m_StaminaAttribute.MaxValue:F0}, Endurance Lv.{level}, XP: {m_CurrentXP:F0}/{m_XPToNextLevel:F0} ({XPProgress * 100:F0}%)");
            Debug.Log($"[Stamina Debug] IsSprinting: {m_IsSprinting}, SprintAbility: {(m_SprintAbility != null ? "Found" : "NULL")}, AbilityActive: {sprintAbilityActive}, PlayerSkills: {(m_PlayerSkills != null ? "Found" : "NULL")}");
        }
    }

    /// <summary>
    /// Add XP toward endurance leveling.
    /// </summary>
    private void AddEnduranceXP(float amount)
    {
        // Try to find PlayerSkills if we don't have it yet
        if (m_PlayerSkills == null)
        {
            m_PlayerSkills = GetComponent<PlayerSkills>();
            if (m_PlayerSkills == null)
                m_PlayerSkills = PlayerSkills.Instance;

            if (m_PlayerSkills != null)
            {
                CalculateXPToNextLevel();
                Debug.Log("[StaminaSystem] Found PlayerSkills");
            }
        }

        if (m_PlayerSkills == null || m_PlayerSkills.EnduranceLevel >= 10) return;

        m_CurrentXP += amount;

        // Check for level up
        if (m_CurrentXP >= m_XPToNextLevel && m_XPToNextLevel > 0)
        {
            LevelUpEndurance();
        }
    }

    /// <summary>
    /// Level up endurance skill.
    /// </summary>
    private void LevelUpEndurance()
    {
        if (m_PlayerSkills == null) return;

        // Carry over excess XP
        float excessXP = m_CurrentXP - m_XPToNextLevel;

        // Level up
        m_PlayerSkills.IncreaseEndurance();

        // Reset XP with carryover
        m_CurrentXP = Mathf.Max(0, excessXP);
        CalculateXPToNextLevel();

        if (m_ShowXPProgress)
        {
            Debug.Log($"[StaminaSystem] Endurance leveled up to {m_PlayerSkills.EnduranceLevel}! Next level needs {m_XPToNextLevel:F0} XP");
        }
    }

    private void UpdateBreathing()
    {
        if (m_StaminaAttribute == null) return;

        float staminaNormalized = GetStaminaNormalized();

        // Start breathing when stamina is low
        if (staminaNormalized <= m_StartBreathingThreshold && !m_IsBreathingHeavily)
        {
            Debug.Log($"[Breathing] Stamina depleted! Normalized: {staminaNormalized}");
            StartBreathing();
        }
        // Stop breathing when stamina recovers above threshold
        else if (staminaNormalized >= m_StopBreathingThreshold && m_IsBreathingHeavily)
        {
            Debug.Log($"[Breathing] Stamina recovered! Normalized: {staminaNormalized}");
            StopBreathing();
        }
    }

    private void StartBreathing()
    {
        if (m_HeavyBreathingClip == null)
        {
            Debug.LogWarning("[Breathing] No audio clip assigned!");
            return;
        }

        if (m_BreathingAudioSource == null)
        {
            Debug.Log("[Breathing] Creating audio source on demand...");
            SetupBreathingAudio();
        }

        if (m_BreathingAudioSource != null)
        {
            m_IsBreathingHeavily = true;
            m_BreathingAudioSource.loop = true; // Re-enable looping
            m_BreathingAudioSource.Play();
            Debug.Log("[Breathing] Started heavy breathing sound");
        }
    }

    private void StopBreathing()
    {
        if (m_BreathingAudioSource == null) return;

        m_IsBreathingHeavily = false;
        // Disable looping so current clip finishes naturally instead of cutting off
        m_BreathingAudioSource.loop = false;
        Debug.Log("[Breathing] Letting breath finish...");
    }

    private void OnAbilityActive(Ability ability, bool active)
    {
        // Check if this is the sprint ability (SpeedChange)
        if (ability == m_SprintAbility || ability is SpeedChange || ability.GetType().Name.Contains("Speed"))
        {
            Debug.Log($"[Stamina] Sprint ability {(active ? "STARTED" : "STOPPED")}");

            if (active)
            {
                // Check if we have enough stamina to start sprinting
                if (m_StaminaAttribute != null && m_StaminaAttribute.Value < m_MinStaminaToSprint)
                {
                    Debug.Log("[Stamina] Not enough stamina to sprint!");
                    m_Locomotion.TryStopAbility(ability);
                    return;
                }
            }

            m_IsSprinting = active;
        }
    }

    private void LateUpdate()
    {
        if (m_StaminaAttribute == null) return;

        // Try to find locomotion if we don't have it
        if (m_Locomotion == null)
        {
            m_Locomotion = GetComponent<UltimateCharacterLocomotion>();
            if (m_Locomotion != null)
            {
                Debug.Log("[StaminaSystem] Found Locomotion in LateUpdate");
            }
        }

        // Try to find sprint ability if we don't have it
        if (m_SprintAbility == null && m_Locomotion != null)
        {
            m_SprintAbility = m_Locomotion.GetAbility<SpeedChange>();
            if (m_SprintAbility != null)
            {
                Debug.Log("[StaminaSystem] Found SpeedChange ability in LateUpdate");
            }
        }

        // Update sprinting state from ability (if we have it)
        if (m_SprintAbility != null)
        {
            m_IsSprinting = m_SprintAbility.IsActive;

            // Force stop sprint if stamina is depleted
            if (m_IsSprinting && m_StaminaAttribute.Value <= 0)
            {
                m_Locomotion.TryStopAbility(m_SprintAbility, true); // force = true
                m_IsSprinting = false;
            }

            // Prevent starting sprint if stamina too low
            if (m_SprintAbility.IsActive && m_StaminaAttribute.Value < m_MinStaminaToSprint)
            {
                m_Locomotion.TryStopAbility(m_SprintAbility, true);
            }
        }
    }

    /// <summary>
    /// Returns the current stamina as a normalized value (0-1).
    /// </summary>
    public float GetStaminaNormalized()
    {
        if (m_StaminaAttribute == null) return 0;
        return (m_StaminaAttribute.Value - m_StaminaAttribute.MinValue) /
               (m_StaminaAttribute.MaxValue - m_StaminaAttribute.MinValue);
    }
}
