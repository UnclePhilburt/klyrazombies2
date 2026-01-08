using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// Prevents browser default key behavior in WebGL builds.
/// Specifically fixes Tab key being intercepted by browser instead of going to game.
/// </summary>
public class WebGLInputHandler : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void PreventTabDefault();

    [DllImport("__Internal")]
    private static extern void PreventKeysDefault();
#endif

    [SerializeField] private bool m_PreventTabOnly = false;

    private void Awake()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (m_PreventTabOnly)
        {
            PreventTabDefault();
            Debug.Log("[WebGLInputHandler] Tab key default behavior disabled");
        }
        else
        {
            PreventKeysDefault();
            Debug.Log("[WebGLInputHandler] All reserved keys default behavior disabled");
        }
#endif
    }
}
