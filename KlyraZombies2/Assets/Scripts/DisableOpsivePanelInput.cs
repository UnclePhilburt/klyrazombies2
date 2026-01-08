using UnityEngine;
using System.Reflection;

/// <summary>
/// Disables Opsive's DisplayPanelManagerHandler "Open Panel" input at runtime
/// so only our custom inventory responds to Tab.
/// Add this to any GameObject in the scene.
/// </summary>
public class DisableOpsivePanelInput : MonoBehaviour
{
    [SerializeField] private bool m_DisableOnAwake = true;

    private void Awake()
    {
        if (m_DisableOnAwake)
        {
            DisableOpsiveOpenPanelInput();
        }
    }

    [ContextMenu("Disable Opsive Open Panel Input")]
    public void DisableOpsiveOpenPanelInput()
    {
        // Find all DisplayPanelManagerHandler components
        var handlers = FindObjectsOfType<MonoBehaviour>();

        foreach (var handler in handlers)
        {
            if (handler.GetType().Name != "DisplayPanelManagerHandler")
                continue;

            // Use reflection to clear the m_OpenTogglePanelInput array
            var field = handler.GetType().GetField("m_OpenTogglePanelInput",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (field != null)
            {
                // Get the array and set it to empty
                var array = field.GetValue(handler) as System.Array;
                if (array != null && array.Length > 0)
                {
                    // Create empty array of same type
                    var emptyArray = System.Array.CreateInstance(array.GetType().GetElementType(), 0);
                    field.SetValue(handler, emptyArray);
                    Debug.Log($"[DisableOpsivePanelInput] Disabled Open Panel input on {handler.gameObject.name}");
                }
            }
        }
    }
}
