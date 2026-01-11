using UnityEngine;

/// <summary>
/// Switches character model on an existing player based on menu selection.
/// Attach this directly to the player GameObject that's already in the scene.
/// </summary>
public class CharacterModelSwitcher : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Prefix for character model GameObjects")]
    [SerializeField] private string m_CharacterPrefix = "SM_Chr_";

    [Tooltip("Patterns to exclude from toggling (hair, attachments, etc.)")]
    [SerializeField] private string[] m_ExcludePatterns = new string[] { "_Attach_", "_Hair_" };

    [Header("Database")]
    [SerializeField] private CharacterDatabase m_Database;

    [Header("Debug")]
    [SerializeField] private int m_DebugCharacterIndex = -1; // -1 = use saved selection

    private void Awake()
    {
        SwitchToSelectedCharacter();
    }

    public void SwitchToSelectedCharacter()
    {
        // Load database if not assigned
        if (m_Database == null)
        {
            m_Database = CharacterDatabase.Instance;
        }

        if (m_Database == null)
        {
            Debug.LogError("[CharacterModelSwitcher] No CharacterDatabase found! Make sure it exists at Resources/CharacterDatabase.asset");
            Debug.Log("[CharacterModelSwitcher] Keeping current character model as fallback");
            return; // Don't disable anything, just keep current state
        }

        if (m_Database.CharacterCount == 0)
        {
            Debug.LogWarning("[CharacterModelSwitcher] CharacterDatabase is empty! Run Tools > Generate Character Database");
            return; // Don't disable anything
        }

        // Get selected character
        int selectedIndex = m_DebugCharacterIndex >= 0 ? m_DebugCharacterIndex : PlayerPrefs.GetInt("SelectedCharacter", 0);
        Debug.Log($"[CharacterModelSwitcher] Selected index: {selectedIndex}, Database has {m_Database.CharacterCount} characters");

        CharacterData selectedCharacter = m_Database.GetCharacter(selectedIndex);

        if (selectedCharacter == null)
        {
            Debug.LogWarning("[CharacterModelSwitcher] No character at index {selectedIndex}!");
            return; // Don't disable anything
        }

        Debug.Log($"[CharacterModelSwitcher] Selected character: {selectedCharacter.displayName}");

        // Get the prefab name to match
        string targetName = "";
        if (selectedCharacter.characterPrefab != null)
        {
            targetName = selectedCharacter.characterPrefab.name;
        }
        else if (!string.IsNullOrEmpty(selectedCharacter.prefabPath))
        {
            targetName = System.IO.Path.GetFileNameWithoutExtension(selectedCharacter.prefabPath);
        }

        // If still empty, try to construct from display name (e.g., "RiotCop Male" -> "SM_Chr_RiotCop_Male_01")
        if (string.IsNullOrEmpty(targetName))
        {
            targetName = "SM_Chr_" + selectedCharacter.displayName.Replace(" ", "_") + "_01";
            Debug.Log($"[CharacterModelSwitcher] Constructed target name from display name: {targetName}");
        }

        Debug.Log($"[CharacterModelSwitcher] Switching to character: {targetName}");

        // Find and toggle all character models
        bool found = false;
        ToggleModelsRecursive(transform, targetName, ref found);

        // If exact match failed, try partial match
        if (!found)
        {
            Debug.Log($"[CharacterModelSwitcher] Exact match failed, trying partial match...");
            string partialName = selectedCharacter.displayName.Replace(" ", "_");
            ToggleModelsPartialMatch(transform, partialName, ref found);
        }

        if (found)
        {
            Debug.Log($"[CharacterModelSwitcher] Successfully switched to {selectedCharacter.displayName}");
        }
        else
        {
            Debug.LogWarning($"[CharacterModelSwitcher] Could not find model '{targetName}', enabling first character as fallback");
            EnableFirstCharacter(transform);
        }
    }

    private void EnableFirstCharacter(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith(m_CharacterPrefix))
            {
                child.gameObject.SetActive(true);
                Debug.Log($"[CharacterModelSwitcher] Fallback - Enabled first character: {child.name}");
                return;
            }

            EnableFirstCharacter(child);
        }
    }

    private void ToggleModelsRecursive(Transform parent, string targetName, ref bool found)
    {
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith(m_CharacterPrefix))
            {
                // Skip hair and attachments - don't toggle them
                if (ShouldExclude(child.name))
                {
                    ToggleModelsRecursive(child, targetName, ref found);
                    continue;
                }

                bool isTarget = child.name == targetName;

                if (!isTarget)
                {
                    child.gameObject.SetActive(false);
                }
                else
                {
                    child.gameObject.SetActive(true);
                    // Also enable all parents up to the root
                    EnableParents(child);
                    found = true;
                    Debug.Log($"[CharacterModelSwitcher] Enabled: {child.name}");
                }
            }

            ToggleModelsRecursive(child, targetName, ref found);
        }
    }

    private void EnableParents(Transform child)
    {
        Transform current = child.parent;
        while (current != null && current != transform)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
                Debug.Log($"[CharacterModelSwitcher] Enabled parent: {current.name}");
            }
            current = current.parent;
        }
    }

    /// <summary>
    /// Check if an object should be excluded from toggling (hair, attachments, etc.)
    /// </summary>
    private bool ShouldExclude(string objectName)
    {
        foreach (string pattern in m_ExcludePatterns)
        {
            if (objectName.Contains(pattern))
            {
                return true;
            }
        }
        return false;
    }

    private void ToggleModelsPartialMatch(Transform parent, string partialName, ref bool found)
    {
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith(m_CharacterPrefix))
            {
                // Skip hair and attachments - don't toggle them
                if (ShouldExclude(child.name))
                {
                    ToggleModelsPartialMatch(child, partialName, ref found);
                    continue;
                }

                // Check if name contains the partial match (e.g., "RiotCop_Male" in "SM_Chr_RiotCop_Male_01")
                bool isTarget = child.name.Contains(partialName);

                if (isTarget && !found) // Only enable the first match
                {
                    child.gameObject.SetActive(true);
                    found = true;
                    Debug.Log($"[CharacterModelSwitcher] Enabled (partial match): {child.name}");
                }
                else
                {
                    child.gameObject.SetActive(false);
                }
            }

            ToggleModelsPartialMatch(child, partialName, ref found);
        }
    }
}
