using UnityEngine;

/// <summary>
/// Marker component - objects with this will NOT be split by the ChunkSplitter.
/// The entire hierarchy will stay together in one chunk.
/// Add this to building prefabs, complex props, etc. that should never be split.
/// </summary>
public class ChunkKeepTogether : MonoBehaviour
{
    [Tooltip("If true, this object and all its children will be kept together as one unit")]
    public bool keepTogether = true;

    private void OnDrawGizmosSelected()
    {
        if (keepTogether)
        {
            // Draw a green wireframe box around the object to show it's protected
            Gizmos.color = Color.green;

            // Get bounds from all renderers
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
            else
            {
                // No renderers, draw around transform
                Gizmos.DrawWireSphere(transform.position, 2f);
            }
        }
    }
}
