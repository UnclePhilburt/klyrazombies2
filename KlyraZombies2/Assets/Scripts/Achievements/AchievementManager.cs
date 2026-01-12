using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages achievement progress, unlocks, and persistence.
/// Auto-initializes and persists across scenes.
/// </summary>
public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [Header("Achievements")]
    [SerializeField] private List<AchievementData> m_Achievements = new List<AchievementData>();

    [Header("Debug")]
    [SerializeField] private bool m_DebugMode = false;

    // Events
    public static event Action<AchievementData> OnAchievementUnlocked;
    public static event Action<AchievementType, int> OnProgressUpdated;

    // Progress tracking
    private Dictionary<string, bool> m_UnlockedAchievements = new Dictionary<string, bool>();
    private Dictionary<AchievementType, int> m_Progress = new Dictionary<AchievementType, int>();

    // PlayerPrefs keys
    private const string UNLOCKED_KEY = "UnlockedAchievements";
    private const string PROGRESS_KEY_PREFIX = "AchievementProgress_";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            Debug.Log("[AchievementManager] Auto-initializing...");
            GameObject managerObj = new GameObject("AchievementManager");
            managerObj.AddComponent<AchievementManager>();
            DontDestroyOnLoad(managerObj);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadProgress();
        LoadAchievements();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void LoadAchievements()
    {
        // Load all achievements from Resources if not assigned
        if (m_Achievements.Count == 0)
        {
            m_Achievements.AddRange(Resources.LoadAll<AchievementData>("Achievements"));
            Debug.Log($"[AchievementManager] Loaded {m_Achievements.Count} achievements from Resources/Achievements");

            if (m_Achievements.Count == 0)
            {
                Debug.LogWarning("[AchievementManager] No achievements found! Run 'Project Klyra > Achievements > Generate Default Achievements' in the editor.");
            }
        }
    }

    private void SubscribeToEvents()
    {
        // Subscribe to zombie death events
        ZombieHealth.OnAnyZombieDeath += OnZombieDeath;
        Debug.Log("[AchievementManager] Subscribed to ZombieHealth.OnAnyZombieDeath");
    }

    private void UnsubscribeFromEvents()
    {
        ZombieHealth.OnAnyZombieDeath -= OnZombieDeath;
    }

    private void OnZombieDeath(bool wasHeadshot)
    {
        Debug.Log($"[AchievementManager] OnZombieDeath received! Headshot: {wasHeadshot}");

        // Increment zombie kills
        AddProgress(AchievementType.ZombieKills, 1);

        // Increment headshot kills if applicable
        if (wasHeadshot)
        {
            AddProgress(AchievementType.HeadshotKills, 1);
        }

        int kills = GetProgress(AchievementType.ZombieKills);
        Debug.Log($"[AchievementManager] Total zombie kills: {kills}");
    }

    /// <summary>
    /// Add progress to an achievement type.
    /// </summary>
    public void AddProgress(AchievementType type, int amount)
    {
        if (!m_Progress.ContainsKey(type))
            m_Progress[type] = 0;

        m_Progress[type] += amount;
        SaveProgress(type);

        OnProgressUpdated?.Invoke(type, m_Progress[type]);

        // Check for achievement unlocks
        CheckAchievements(type);
    }

    /// <summary>
    /// Set progress to a specific value (for things like "current" stats).
    /// </summary>
    public void SetProgress(AchievementType type, int value)
    {
        m_Progress[type] = value;
        SaveProgress(type);

        OnProgressUpdated?.Invoke(type, m_Progress[type]);
        CheckAchievements(type);
    }

    /// <summary>
    /// Get current progress for an achievement type.
    /// </summary>
    public int GetProgress(AchievementType type)
    {
        return m_Progress.TryGetValue(type, out int value) ? value : 0;
    }

    private void CheckAchievements(AchievementType type)
    {
        Debug.Log($"[AchievementManager] Checking {m_Achievements.Count} achievements for type {type}");

        foreach (var achievement in m_Achievements)
        {
            if (achievement == null) continue;
            if (achievement.type != type) continue;
            if (IsUnlocked(achievement.achievementId)) continue;

            int progress = GetProgress(type);
            Debug.Log($"[AchievementManager] Checking '{achievement.title}': progress={progress}, target={achievement.targetValue}");

            if (progress >= achievement.targetValue)
            {
                UnlockAchievement(achievement);
            }
        }
    }

    private void UnlockAchievement(AchievementData achievement)
    {
        if (IsUnlocked(achievement.achievementId)) return;

        m_UnlockedAchievements[achievement.achievementId] = true;
        SaveUnlocked();

        Debug.Log($"[AchievementManager] === ACHIEVEMENT UNLOCKED: {achievement.title} ===");

        // Show popup
        if (AchievementPopup.Instance != null)
        {
            Debug.Log($"[AchievementManager] Showing popup via existing instance");
            AchievementPopup.Instance.Show(achievement);
        }
        else
        {
            Debug.Log($"[AchievementManager] Creating new AchievementPopup instance");
            // Create popup if it doesn't exist
            GameObject popupObj = new GameObject("AchievementPopup");
            popupObj.AddComponent<AchievementPopup>();
            DontDestroyOnLoad(popupObj);

            // Wait a frame then show
            StartCoroutine(ShowPopupDelayed(achievement));
        }

        OnAchievementUnlocked?.Invoke(achievement);
    }

    private System.Collections.IEnumerator ShowPopupDelayed(AchievementData achievement)
    {
        yield return null;
        if (AchievementPopup.Instance != null)
        {
            AchievementPopup.Instance.Show(achievement);
        }
    }

    public bool IsUnlocked(string achievementId)
    {
        return m_UnlockedAchievements.TryGetValue(achievementId, out bool unlocked) && unlocked;
    }

    public float GetAchievementProgress(AchievementData achievement)
    {
        if (achievement == null) return 0f;
        int progress = GetProgress(achievement.type);
        return Mathf.Clamp01((float)progress / achievement.targetValue);
    }

    public int GetUnlockedCount()
    {
        int count = 0;
        foreach (var kvp in m_UnlockedAchievements)
        {
            if (kvp.Value) count++;
        }
        return count;
    }

    public int GetTotalCount()
    {
        return m_Achievements.Count;
    }

    #region Persistence

    private void LoadProgress()
    {
        // Load unlocked achievements
        string unlockedJson = PlayerPrefs.GetString(UNLOCKED_KEY, "");
        if (!string.IsNullOrEmpty(unlockedJson))
        {
            string[] ids = unlockedJson.Split(',');
            foreach (string id in ids)
            {
                if (!string.IsNullOrEmpty(id))
                    m_UnlockedAchievements[id] = true;
            }
        }

        // Load progress for each type
        foreach (AchievementType type in Enum.GetValues(typeof(AchievementType)))
        {
            string key = PROGRESS_KEY_PREFIX + type.ToString();
            m_Progress[type] = PlayerPrefs.GetInt(key, 0);
        }

        if (m_DebugMode)
            Debug.Log($"[AchievementManager] Loaded progress. Zombie kills: {GetProgress(AchievementType.ZombieKills)}");
    }

    private void SaveProgress(AchievementType type)
    {
        string key = PROGRESS_KEY_PREFIX + type.ToString();
        PlayerPrefs.SetInt(key, m_Progress[type]);
        PlayerPrefs.Save();
    }

    private void SaveUnlocked()
    {
        List<string> ids = new List<string>();
        foreach (var kvp in m_UnlockedAchievements)
        {
            if (kvp.Value)
                ids.Add(kvp.Key);
        }
        PlayerPrefs.SetString(UNLOCKED_KEY, string.Join(",", ids));
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Reset all achievement progress and unlocks.
    /// </summary>
    public void ResetAll()
    {
        m_UnlockedAchievements.Clear();
        m_Progress.Clear();

        PlayerPrefs.DeleteKey(UNLOCKED_KEY);
        foreach (AchievementType type in Enum.GetValues(typeof(AchievementType)))
        {
            PlayerPrefs.DeleteKey(PROGRESS_KEY_PREFIX + type.ToString());
        }
        PlayerPrefs.Save();

        Debug.Log("[AchievementManager] All achievements reset");
    }

    #endregion
}
