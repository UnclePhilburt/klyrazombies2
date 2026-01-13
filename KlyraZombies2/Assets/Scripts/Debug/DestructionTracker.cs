using UnityEngine;

/// <summary>
/// Debug component to track when an object gets destroyed.
/// </summary>
public class DestructionTracker : MonoBehaviour
{
    public CharacterSpawner spawnerRef;
    private Vector3 m_LastPosition;

    private void Update()
    {
        m_LastPosition = transform.position;

        // Check if falling below kill zone
        if (transform.position.y < -50f)
        {
            Debug.LogError($"[DestructionTracker] Player fell below Y=-50! Position: {transform.position}");
        }
    }

    private void OnDestroy()
    {
        Debug.LogError($"[DestructionTracker] {gameObject.name} DESTROYED at position {m_LastPosition}");

        // Try to get more info
        if (gameObject != null)
        {
            Debug.LogError($"[DestructionTracker] activeInHierarchy={gameObject.activeInHierarchy}, activeSelf={gameObject.activeSelf}");
        }
    }

    private void OnDisable()
    {
        Debug.LogWarning($"[DestructionTracker] {gameObject.name} DISABLED at position {transform.position}");
    }
}
