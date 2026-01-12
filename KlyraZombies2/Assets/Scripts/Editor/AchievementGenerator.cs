#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor tool to generate default achievements.
/// </summary>
public class AchievementGenerator : EditorWindow
{
    [MenuItem("Project Klyra/Achievements/Generate Default Achievements")]
    public static void GenerateDefaultAchievements()
    {
        string path = "Assets/Resources/Achievements";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        int created = 0;

        // Zombie Kill Achievements
        created += CreateAchievementIfNotExists(path, "First Blood", "Kill your first zombie", AchievementType.ZombieKills, 1);
        created += CreateAchievementIfNotExists(path, "Getting Started", "Kill 10 zombies", AchievementType.ZombieKills, 10);
        created += CreateAchievementIfNotExists(path, "Zombie Slayer", "Kill 25 zombies", AchievementType.ZombieKills, 25);
        created += CreateAchievementIfNotExists(path, "Undead Hunter", "Kill 50 zombies", AchievementType.ZombieKills, 50);
        created += CreateAchievementIfNotExists(path, "Death Dealer", "Kill 100 zombies", AchievementType.ZombieKills, 100);
        created += CreateAchievementIfNotExists(path, "Zombie Apocalypse", "Kill 250 zombies", AchievementType.ZombieKills, 250);
        created += CreateAchievementIfNotExists(path, "Extinction Event", "Kill 500 zombies", AchievementType.ZombieKills, 500);
        created += CreateAchievementIfNotExists(path, "One Man Army", "Kill 1000 zombies", AchievementType.ZombieKills, 1000);

        // Headshot Achievements
        created += CreateAchievementIfNotExists(path, "Clean Shot", "Get your first headshot kill", AchievementType.HeadshotKills, 1);
        created += CreateAchievementIfNotExists(path, "Sharpshooter", "Get 10 headshot kills", AchievementType.HeadshotKills, 10);
        created += CreateAchievementIfNotExists(path, "Marksman", "Get 25 headshot kills", AchievementType.HeadshotKills, 25);
        created += CreateAchievementIfNotExists(path, "Dead Eye", "Get 50 headshot kills", AchievementType.HeadshotKills, 50);
        created += CreateAchievementIfNotExists(path, "Sniper Elite", "Get 100 headshot kills", AchievementType.HeadshotKills, 100);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[AchievementGenerator] Created {created} new achievements");
        EditorUtility.DisplayDialog("Achievements Generated", $"Created {created} new achievements in\n{path}", "OK");
    }

    [MenuItem("Project Klyra/Achievements/Reset All Progress")]
    public static void ResetAllProgress()
    {
        if (EditorUtility.DisplayDialog("Reset Achievements",
            "This will reset all achievement progress and unlocks from PlayerPrefs.\n\nContinue?",
            "Reset", "Cancel"))
        {
            // Clear unlocked achievements
            PlayerPrefs.DeleteKey("UnlockedAchievements");

            // Clear all progress
            foreach (AchievementType type in System.Enum.GetValues(typeof(AchievementType)))
            {
                PlayerPrefs.DeleteKey("AchievementProgress_" + type.ToString());
            }

            PlayerPrefs.Save();
            Debug.Log("[AchievementGenerator] All achievement progress reset");
        }
    }

    [MenuItem("Project Klyra/Achievements/Open Achievements Folder")]
    public static void OpenAchievementsFolder()
    {
        string path = "Assets/Resources/Achievements";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
        EditorUtility.RevealInFinder(path);
    }

    private static int CreateAchievementIfNotExists(string folder, string title, string description, AchievementType type, int target)
    {
        string filename = title.Replace(" ", "_");
        string assetPath = $"{folder}/{filename}.asset";

        // Check if already exists
        if (File.Exists(assetPath))
        {
            return 0;
        }

        AchievementData achievement = ScriptableObject.CreateInstance<AchievementData>();
        achievement.achievementId = filename.ToLower();
        achievement.title = title;
        achievement.description = description;
        achievement.type = type;
        achievement.targetValue = target;

        // Set accent color based on type
        if (type == AchievementType.HeadshotKills)
        {
            achievement.accentColor = new Color(1f, 0.3f, 0.3f, 1f); // Red for headshots
        }
        else
        {
            achievement.accentColor = new Color(1f, 0.84f, 0f, 1f); // Gold for kills
        }

        AssetDatabase.CreateAsset(achievement, assetPath);
        return 1;
    }
}
#endif
