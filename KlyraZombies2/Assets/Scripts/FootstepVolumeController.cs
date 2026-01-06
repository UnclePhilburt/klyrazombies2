using UnityEngine;
using Opsive.Shared.Events;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities;
using Opsive.UltimateCharacterController.SurfaceSystem;
using System.Collections.Generic;

public class FootstepVolumeController : MonoBehaviour
{
    [Header("Volume Multipliers")]
    [SerializeField] [Range(0f, 1f)] private float m_SprintVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float m_WalkVolume = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float m_CrouchVolume = 0.2f;

    [Header("Footstep Effects (auto-populated if empty)")]
    [SerializeField] private List<SurfaceEffect> m_FootstepEffects = new List<SurfaceEffect>();

    private bool m_IsSprinting;
    private bool m_IsCrouching;
    private float m_CurrentMultiplier = 0.5f;

    private Dictionary<SurfaceEffect, float> m_OriginalMinVolumes = new Dictionary<SurfaceEffect, float>();
    private Dictionary<SurfaceEffect, float> m_OriginalMaxVolumes = new Dictionary<SurfaceEffect, float>();

    private void Start()
    {
        // Auto-find footstep effects if not assigned
        if (m_FootstepEffects.Count == 0)
        {
            FindFootstepEffects();
        }

        // Store original volumes
        foreach (var effect in m_FootstepEffects)
        {
            if (effect != null)
            {
                m_OriginalMinVolumes[effect] = effect.MinAudioVolume;
                m_OriginalMaxVolumes[effect] = effect.MaxAudioVolume;
            }
        }

        m_CurrentMultiplier = m_WalkVolume;
        ApplyVolumeMultiplier();

        // Register for ability events
        EventHandler.RegisterEvent<Ability, bool>(gameObject, "OnCharacterAbilityActive", OnAbilityActive);
    }

    private void OnDestroy()
    {
        // Restore original volumes when destroyed
        RestoreOriginalVolumes();
        EventHandler.UnregisterEvent<Ability, bool>(gameObject, "OnCharacterAbilityActive", OnAbilityActive);
    }

    private void OnApplicationQuit()
    {
        // Restore original volumes when quitting
        RestoreOriginalVolumes();
    }

    private void FindFootstepEffects()
    {
        // Load all footstep SurfaceEffects from the Opsive demo folder
        string[] paths = new string[]
        {
            "Assets/Samples/Opsive Ultimate Character Controller/3.3.3/Demo/SurfaceSystem/SurfaceEffects/FootstepOnDirt",
            "Assets/Samples/Opsive Ultimate Character Controller/3.3.3/Demo/SurfaceSystem/SurfaceEffects/FootstepOnGrass",
            "Assets/Samples/Opsive Ultimate Character Controller/3.3.3/Demo/SurfaceSystem/SurfaceEffects/FootstepOnMetal",
            "Assets/Samples/Opsive Ultimate Character Controller/3.3.3/Demo/SurfaceSystem/SurfaceEffects/FootstepOnSand",
            "Assets/Samples/Opsive Ultimate Character Controller/3.3.3/Demo/SurfaceSystem/SurfaceEffects/FootstepOnTile",
            "Assets/Samples/Opsive Ultimate Character Controller/3.3.3/Demo/SurfaceSystem/SurfaceEffects/FootstepOnWater",
            "Assets/Samples/Opsive Ultimate Character Controller/3.3.3/Demo/SurfaceSystem/SurfaceEffects/FootstepOnWood",
        };

        foreach (var path in paths)
        {
            var effect = Resources.Load<SurfaceEffect>(path);
            if (effect != null)
            {
                m_FootstepEffects.Add(effect);
            }
        }

        // If Resources.Load didn't work, try to find them in the scene's SurfaceManager
        if (m_FootstepEffects.Count == 0)
        {
            Debug.LogWarning("FootstepVolumeController: Could not auto-find footstep effects. Please assign them manually.");
        }
    }

    private void OnAbilityActive(Ability ability, bool active)
    {
        bool changed = false;

        if (ability is SpeedChange)
        {
            m_IsSprinting = active;
            changed = true;
        }
        else if (ability is HeightChange)
        {
            m_IsCrouching = active;
            changed = true;
        }

        if (changed)
        {
            UpdateVolumeMultiplier();
        }
    }

    private void UpdateVolumeMultiplier()
    {
        if (m_IsCrouching)
        {
            m_CurrentMultiplier = m_CrouchVolume;
        }
        else if (m_IsSprinting)
        {
            m_CurrentMultiplier = m_SprintVolume;
        }
        else
        {
            m_CurrentMultiplier = m_WalkVolume;
        }

        ApplyVolumeMultiplier();
    }

    private void ApplyVolumeMultiplier()
    {
        foreach (var effect in m_FootstepEffects)
        {
            if (effect != null && m_OriginalMinVolumes.ContainsKey(effect))
            {
                effect.MinAudioVolume = m_OriginalMinVolumes[effect] * m_CurrentMultiplier;
                effect.MaxAudioVolume = m_OriginalMaxVolumes[effect] * m_CurrentMultiplier;
            }
        }
    }

    private void RestoreOriginalVolumes()
    {
        foreach (var effect in m_FootstepEffects)
        {
            if (effect != null && m_OriginalMinVolumes.ContainsKey(effect))
            {
                effect.MinAudioVolume = m_OriginalMinVolumes[effect];
                effect.MaxAudioVolume = m_OriginalMaxVolumes[effect];
            }
        }
    }
}
