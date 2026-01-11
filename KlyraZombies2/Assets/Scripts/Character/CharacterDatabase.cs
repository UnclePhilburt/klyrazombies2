using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Database of all selectable characters
/// </summary>
[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Game/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    [Tooltip("List of all playable characters")]
    public List<CharacterData> characters = new List<CharacterData>();

    [Tooltip("Default character index if none selected")]
    public int defaultCharacterIndex = 0;

    private static CharacterDatabase s_Instance;

    /// <summary>
    /// Get the singleton instance (loads from Resources if needed)
    /// </summary>
    public static CharacterDatabase Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = Resources.Load<CharacterDatabase>("CharacterDatabase");
                if (s_Instance == null)
                {
                    Debug.LogError("[CharacterDatabase] No CharacterDatabase found in Resources folder!");
                }
            }
            return s_Instance;
        }
    }

    /// <summary>
    /// Get character by index with bounds checking
    /// </summary>
    public CharacterData GetCharacter(int index)
    {
        if (characters == null || characters.Count == 0)
            return null;

        index = Mathf.Clamp(index, 0, characters.Count - 1);
        return characters[index];
    }

    /// <summary>
    /// Get the currently selected character from PlayerPrefs
    /// </summary>
    public CharacterData GetSelectedCharacter()
    {
        int selectedIndex = PlayerPrefs.GetInt("SelectedCharacter", defaultCharacterIndex);
        return GetCharacter(selectedIndex);
    }

    /// <summary>
    /// Get the selected character index
    /// </summary>
    public int GetSelectedIndex()
    {
        return PlayerPrefs.GetInt("SelectedCharacter", defaultCharacterIndex);
    }

    /// <summary>
    /// Save the selected character index
    /// </summary>
    public void SetSelectedCharacter(int index)
    {
        PlayerPrefs.SetInt("SelectedCharacter", index);
        PlayerPrefs.Save();
    }

    public int CharacterCount => characters?.Count ?? 0;
}
