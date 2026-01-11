using UnityEngine;

/// <summary>
/// Simple runtime controller for baked Sidekick characters in WebGL.
/// Just keeps all meshes visible - no clothing system for now.
/// </summary>
public class BakedCharacterController : MonoBehaviour
{
    private void Awake()
    {
        // Ensure all skinned mesh renderers are enabled
        var meshes = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var mesh in meshes)
        {
            mesh.gameObject.SetActive(true);
        }

        Debug.Log($"[BakedCharacterController] Initialized with {meshes.Length} meshes");
    }
}
