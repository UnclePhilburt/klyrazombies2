using UnityEngine;
using Opsive.Shared.Events;

/// <summary>
/// Relays animation events from the Sidekick visual character to the main Opsive character.
/// Add this to the SidekickCharacter GameObject that has the Animator.
/// </summary>
public class AnimationEventRelay : MonoBehaviour
{
    [Tooltip("The main character GameObject that should receive the events. If null, will try to find parent with UltimateCharacterLocomotion.")]
    [SerializeField] private GameObject m_TargetCharacter;

    private void Awake()
    {
        if (m_TargetCharacter == null)
        {
            // Try to find the parent character
            var parent = transform.parent;
            while (parent != null)
            {
                if (parent.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>() != null)
                {
                    m_TargetCharacter = parent.gameObject;
                    break;
                }
                parent = parent.parent;
            }
        }

        if (m_TargetCharacter == null)
        {
            Debug.LogWarning($"[AnimationEventRelay] No target character found for {gameObject.name}");
        }
    }

    /// <summary>
    /// Called by Opsive animation events.
    /// </summary>
    public void ExecuteEvent(string eventName)
    {
        if (m_TargetCharacter != null)
        {
            EventHandler.ExecuteEvent(m_TargetCharacter, eventName);
        }
    }

    /// <summary>
    /// Called by Opsive animation events with a slot parameter.
    /// </summary>
    public void ExecuteEvent(AnimationEvent animationEvent)
    {
        if (m_TargetCharacter != null)
        {
            // The animation event stringParameter contains the event name
            // The intParameter often contains the slot ID
            if (!string.IsNullOrEmpty(animationEvent.stringParameter))
            {
                if (animationEvent.intParameter >= 0)
                {
                    EventHandler.ExecuteEvent(m_TargetCharacter, animationEvent.stringParameter, animationEvent.intParameter);
                }
                else
                {
                    EventHandler.ExecuteEvent(m_TargetCharacter, animationEvent.stringParameter);
                }
            }
        }
    }

    /// <summary>
    /// Foot IK event - can be ignored for visual-only characters.
    /// </summary>
    public void FootIK(int foot) { }

    /// <summary>
    /// Footstep event - can be ignored or forwarded.
    /// </summary>
    public void Footstep(int foot) { }

    /// <summary>
    /// Item use event.
    /// </summary>
    public void ItemUseComplete(int slotID)
    {
        if (m_TargetCharacter != null)
        {
            EventHandler.ExecuteEvent(m_TargetCharacter, "OnAnimatorItemUseComplete", slotID);
        }
    }

    /// <summary>
    /// Item use complete event.
    /// </summary>
    public void ItemUseCompleteSlot(int slotID)
    {
        if (m_TargetCharacter != null)
        {
            EventHandler.ExecuteEvent(m_TargetCharacter, "OnAnimatorItemUseCompleteSlot", slotID);
        }
    }
}
