using UnityEngine;

/// <summary>
/// ScriptableObject defining an achievement.
/// </summary>
[CreateAssetMenu(fileName = "New Achievement", menuName = "Game/Achievement")]
public class AchievementData : ScriptableObject
{
    [Header("Basic Info")]
    public string achievementId;
    public string title = "Achievement";
    public string description = "";

    [Header("Requirements")]
    public AchievementType type = AchievementType.ZombieKills;
    public int targetValue = 1;

    [Header("Display")]
    public Sprite icon;
    public Color accentColor = new Color(1f, 0.84f, 0f, 1f); // Gold

    [Header("Rewards (Optional)")]
    public int xpReward = 0;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(achievementId) && !string.IsNullOrEmpty(title))
        {
            achievementId = title.Replace(" ", "_").ToLower();
        }
    }
}

public enum AchievementType
{
    ZombieKills,        // Total zombie kills
    HeadshotKills,      // Headshot kills
    KillsInSession,     // Kills in one session
    DaysSurvived,       // Days survived
    ItemsLooted,        // Items picked up
    DistanceTraveled,   // Distance walked
    POIsDiscovered,     // POIs found
    Custom              // For special achievements
}
