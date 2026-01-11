using UnityEngine;

/// <summary>
/// Data for a single selectable character
/// </summary>
[CreateAssetMenu(fileName = "NewCharacter", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Display")]
    [Tooltip("Name shown in character select")]
    public string displayName;

    [Tooltip("Character portrait/icon for UI")]
    public Sprite portrait;

    [Tooltip("Short description of the character")]
    [TextArea(2, 4)]
    public string description;

    [Header("Prefab")]
    [Tooltip("The character prefab to spawn (visual model only, not the full UCC character)")]
    public GameObject characterPrefab;

    [Tooltip("Path to prefab in Assets (used if prefab reference is null)")]
    public string prefabPath;

    [Header("Optional Stats")]
    public float healthModifier = 1f;
    public float speedModifier = 1f;
    public float staminaModifier = 1f;

    /// <summary>
    /// Get the prefab, loading from path if direct reference is null
    /// </summary>
    public GameObject GetPrefab()
    {
        if (characterPrefab != null)
            return characterPrefab;

        if (!string.IsNullOrEmpty(prefabPath))
        {
            #if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            #endif
        }

        return null;
    }
}
