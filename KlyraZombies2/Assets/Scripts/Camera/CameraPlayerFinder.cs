using UnityEngine;
using Opsive.UltimateCharacterController.Camera;

/// <summary>
/// Automatically finds and assigns the player to the Opsive CameraController at runtime.
/// Attach this to the same GameObject as the CameraController.
/// </summary>
public class CameraPlayerFinder : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Tag to search for player")]
    [SerializeField] private string m_PlayerTag = "Player";

    [Tooltip("How often to search for player (seconds)")]
    [SerializeField] private float m_SearchInterval = 0.5f;

    [Tooltip("Stop searching after finding player")]
    [SerializeField] private bool m_StopAfterFound = true;

    private CameraController m_CameraController;
    private float m_LastSearchTime;
    private bool m_Found = false;

    private void Awake()
    {
        m_CameraController = GetComponent<CameraController>();
        if (m_CameraController == null)
        {
            Debug.LogError("[CameraPlayerFinder] No CameraController found on this GameObject!");
            enabled = false;
            return;
        }

        // ALWAYS clear character on load - we'll find the real one
        // This prevents stale references from previous sessions
        m_CameraController.Character = null;
        Debug.Log("[CameraPlayerFinder] Cleared character reference on Awake");
    }

    private void Update()
    {
        if (m_Found && m_StopAfterFound) return;

        // Check if we already have a character assigned
        if (m_CameraController.Character != null)
        {
            if (!m_Found)
            {
                Debug.Log($"[CameraPlayerFinder] Camera already has character: {m_CameraController.Character.name}");
                m_Found = true;
            }
            return;
        }

        // Search periodically
        if (Time.time - m_LastSearchTime < m_SearchInterval) return;
        m_LastSearchTime = Time.time;

        Debug.Log("[CameraPlayerFinder] Searching for player...");
        FindAndAssignPlayer();
    }

    private void LateUpdate()
    {
        // Extra check in LateUpdate in case player spawned this frame
        if (!m_Found && m_CameraController.Character == null)
        {
            FindAndAssignPlayer();
        }
    }

    private void FindAndAssignPlayer()
    {
        // First try by tag
        GameObject player = GameObject.FindGameObjectWithTag(m_PlayerTag);

        if (player != null)
        {
            Debug.Log($"[CameraPlayerFinder] Found object with Player tag: {player.name}");
            if (TryAssignCharacter(player))
                return;
            else
                Debug.Log($"[CameraPlayerFinder] But it has no UltimateCharacterLocomotion!");
        }
        else
        {
            Debug.Log($"[CameraPlayerFinder] No object with tag '{m_PlayerTag}' found");
        }

        // Fallback: find any UltimateCharacterLocomotion in scene
        var character = FindFirstObjectByType<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();
        if (character != null)
        {
            m_CameraController.Character = character.gameObject;
            m_Found = true;
            Debug.Log($"[CameraPlayerFinder] Found character by component search: {character.gameObject.name}");
        }
        else
        {
            Debug.Log("[CameraPlayerFinder] No UltimateCharacterLocomotion found in scene!");
        }
    }

    private bool TryAssignCharacter(GameObject player)
    {
        // Get the character component (Opsive uses UltimateCharacterLocomotion)
        var character = player.GetComponent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();

        if (character != null)
        {
            m_CameraController.Character = character.gameObject;
            m_Found = true;
            Debug.Log($"[CameraPlayerFinder] Found and assigned player: {player.name}");
            return true;
        }

        // Maybe it's on a parent or child
        character = player.GetComponentInParent<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();
        if (character == null)
            character = player.GetComponentInChildren<Opsive.UltimateCharacterController.Character.UltimateCharacterLocomotion>();

        if (character != null)
        {
            m_CameraController.Character = character.gameObject;
            m_Found = true;
            Debug.Log($"[CameraPlayerFinder] Found and assigned player: {character.gameObject.name}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Manually trigger a search for the player.
    /// </summary>
    public void ForceSearch()
    {
        m_Found = false;
        FindAndAssignPlayer();
    }
}
