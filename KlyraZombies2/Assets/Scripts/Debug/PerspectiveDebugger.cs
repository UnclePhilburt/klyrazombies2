using UnityEngine;
using Opsive.UltimateCharacterController.Character;
using Opsive.Shared.Events;

/// <summary>
/// Debug script to track perspective changes and find why weapons are hidden.
/// Add this to the player character to see what's happening.
/// </summary>
public class PerspectiveDebugger : MonoBehaviour
{
    private UltimateCharacterLocomotion m_Locomotion;

    private void Start()
    {
        m_Locomotion = GetComponent<UltimateCharacterLocomotion>();

        Debug.Log($"[PerspectiveDebugger] Start - FirstPersonPerspective: {m_Locomotion?.FirstPersonPerspective}");

        EventHandler.RegisterEvent<bool>(gameObject, "OnCharacterChangePerspectives", OnCharacterChangePerspectives);
        EventHandler.RegisterEvent<bool>(gameObject, "OnCameraChangePerspectives", OnCameraChangePerspectives);
    }

    private void OnCharacterChangePerspectives(bool firstPerson)
    {
        Debug.Log($"[PerspectiveDebugger] OnCharacterChangePerspectives: firstPerson={firstPerson}");
        Debug.Log($"[PerspectiveDebugger] Stack trace:\n{System.Environment.StackTrace}");
    }

    private void OnCameraChangePerspectives(bool firstPerson)
    {
        Debug.Log($"[PerspectiveDebugger] OnCameraChangePerspectives: firstPerson={firstPerson}");
    }

    private void Update()
    {
        // Press P to check current state
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log($"[PerspectiveDebugger] Current FirstPersonPerspective: {m_Locomotion?.FirstPersonPerspective}");
        }
    }

    private void OnDestroy()
    {
        EventHandler.UnregisterEvent<bool>(gameObject, "OnCharacterChangePerspectives", OnCharacterChangePerspectives);
        EventHandler.UnregisterEvent<bool>(gameObject, "OnCameraChangePerspectives", OnCameraChangePerspectives);
    }
}
