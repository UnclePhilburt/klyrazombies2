using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using TMPro;

/// <summary>
/// Main menu controller - handles Play, Settings, Quit buttons
/// Play button loads the game directly.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject m_MainPanel;
    [SerializeField] private GameObject m_SettingsPanel;

    [Header("Main Buttons")]
    [SerializeField] private Button m_PlayButton;
    [SerializeField] private Button m_SettingsButton;
    [SerializeField] private Button m_QuitButton;

    [Header("Settings")]
    [SerializeField] private Slider m_MasterVolumeSlider;
    [SerializeField] private Slider m_SFXVolumeSlider;
    [SerializeField] private Slider m_MusicVolumeSlider;
    [SerializeField] private TMP_Dropdown m_QualityDropdown;
    [SerializeField] private Toggle m_FullscreenToggle;
    [SerializeField] private Button m_BackButton;

    [Header("Audio")]
    [SerializeField] private AudioSource m_MusicSource;
    [SerializeField] private AudioClip[] m_MenuTracks;
    [SerializeField] private bool m_ShuffleTracks = true;

    [Header("Scene Loading")]
    [Tooltip("Name of the gameplay scene to load")]
    [SerializeField] private string m_GameSceneName = "Persistent";

    [Tooltip("Use Addressables to load the game scene (for streaming from remote server)")]
    [SerializeField] private bool m_UseAddressables = true;

    [Header("Loading UI")]
    [SerializeField] private GameObject m_LoadingPanel;
    [SerializeField] private TextMeshProUGUI m_LoadingText;
    [SerializeField] private Slider m_LoadingProgressBar;

    private int m_CurrentTrackIndex = 0;
    private bool m_IsLoading = false;
    private int[] m_ShuffledIndices;
    private bool m_InSettingsPanel = false;

    private void Start()
    {
        Debug.Log("[MainMenuUI] Start() called");

        // Ensure cursor is visible and unlocked
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Setup button listeners
        if (m_PlayButton != null)
        {
            m_PlayButton.onClick.AddListener(OnPlayClicked);
            Debug.Log("[MainMenuUI] Play button connected");
        }
        else
        {
            Debug.LogError("[MainMenuUI] Play button is NULL!");
        }

        if (m_SettingsButton != null)
        {
            m_SettingsButton.onClick.AddListener(OnSettingsClicked);
            Debug.Log("[MainMenuUI] Settings button connected");
        }

        if (m_QuitButton != null)
        {
            m_QuitButton.onClick.AddListener(OnQuitClicked);
            Debug.Log("[MainMenuUI] Quit button connected");
        }

        if (m_BackButton != null)
            m_BackButton.onClick.AddListener(OnBackClicked);

        // Setup settings listeners
        if (m_MasterVolumeSlider != null)
        {
            m_MasterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            m_MasterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (m_SFXVolumeSlider != null)
        {
            m_SFXVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
            m_SFXVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        if (m_MusicVolumeSlider != null)
        {
            m_MusicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
            m_MusicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (m_QualityDropdown != null)
        {
            m_QualityDropdown.ClearOptions();
            m_QualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
            m_QualityDropdown.value = QualitySettings.GetQualityLevel();
            m_QualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        }

        if (m_FullscreenToggle != null)
        {
            m_FullscreenToggle.isOn = Screen.fullScreen;
            m_FullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }

        // Start with main panel visible
        ShowMainPanel();

        // Setup and play music
        SetupMusic();

        // Setup controller navigation
        SetupButtonNavigation();

        // Initialize InputModeManager
        var _ = InputModeManager.Instance;
    }

    private void SetupButtonNavigation()
    {
        // Setup explicit navigation for main menu buttons (vertical list)
        if (m_PlayButton != null && m_SettingsButton != null && m_QuitButton != null)
        {
            // Play button
            var playNav = m_PlayButton.navigation;
            playNav.mode = Navigation.Mode.Explicit;
            playNav.selectOnDown = m_SettingsButton;
            playNav.selectOnUp = m_QuitButton;
            m_PlayButton.navigation = playNav;

            // Settings button
            var settingsNav = m_SettingsButton.navigation;
            settingsNav.mode = Navigation.Mode.Explicit;
            settingsNav.selectOnUp = m_PlayButton;
            settingsNav.selectOnDown = m_QuitButton;
            m_SettingsButton.navigation = settingsNav;

            // Quit button
            var quitNav = m_QuitButton.navigation;
            quitNav.mode = Navigation.Mode.Explicit;
            quitNav.selectOnUp = m_SettingsButton;
            quitNav.selectOnDown = m_PlayButton;
            m_QuitButton.navigation = quitNav;
        }

        // Setup settings panel navigation
        SetupSettingsNavigation();
    }

    private void SetupSettingsNavigation()
    {
        // Build list of navigable settings elements
        var settingsElements = new System.Collections.Generic.List<Selectable>();

        if (m_MasterVolumeSlider != null) settingsElements.Add(m_MasterVolumeSlider);
        if (m_SFXVolumeSlider != null) settingsElements.Add(m_SFXVolumeSlider);
        if (m_MusicVolumeSlider != null) settingsElements.Add(m_MusicVolumeSlider);
        if (m_QualityDropdown != null) settingsElements.Add(m_QualityDropdown);
        if (m_FullscreenToggle != null) settingsElements.Add(m_FullscreenToggle);
        if (m_BackButton != null) settingsElements.Add(m_BackButton);

        // Setup vertical navigation chain
        for (int i = 0; i < settingsElements.Count; i++)
        {
            var nav = settingsElements[i].navigation;
            nav.mode = Navigation.Mode.Explicit;

            if (i > 0)
                nav.selectOnUp = settingsElements[i - 1];
            else
                nav.selectOnUp = settingsElements[settingsElements.Count - 1]; // Wrap to bottom

            if (i < settingsElements.Count - 1)
                nav.selectOnDown = settingsElements[i + 1];
            else
                nav.selectOnDown = settingsElements[0]; // Wrap to top

            settingsElements[i].navigation = nav;
        }
    }

    private void SetupMusic()
    {
        if (m_MenuTracks == null || m_MenuTracks.Length == 0)
            return;

        if (m_MusicSource == null)
        {
            m_MusicSource = gameObject.AddComponent<AudioSource>();
        }

        m_MusicSource.loop = false; // We handle advancement manually
        m_MusicSource.playOnAwake = false;
        m_MusicSource.volume = PlayerPrefs.GetFloat("MusicVolume", 1f);

        // Setup shuffle order
        if (m_ShuffleTracks)
        {
            ShuffleTracks();
        }
        else
        {
            m_ShuffledIndices = new int[m_MenuTracks.Length];
            for (int i = 0; i < m_MenuTracks.Length; i++)
                m_ShuffledIndices[i] = i;
        }

        PlayCurrentTrack();
    }

    private void ShuffleTracks()
    {
        m_ShuffledIndices = new int[m_MenuTracks.Length];
        for (int i = 0; i < m_MenuTracks.Length; i++)
            m_ShuffledIndices[i] = i;

        // Fisher-Yates shuffle
        for (int i = m_ShuffledIndices.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = m_ShuffledIndices[i];
            m_ShuffledIndices[i] = m_ShuffledIndices[j];
            m_ShuffledIndices[j] = temp;
        }
    }

    private void PlayCurrentTrack()
    {
        if (m_MenuTracks == null || m_MenuTracks.Length == 0 || m_MusicSource == null)
            return;

        int trackIndex = m_ShuffledIndices[m_CurrentTrackIndex];
        if (m_MenuTracks[trackIndex] != null)
        {
            m_MusicSource.clip = m_MenuTracks[trackIndex];
            m_MusicSource.Play();
        }
    }

    private void Update()
    {
        // Check if track finished and play next
        if (m_MusicSource != null && !m_MusicSource.isPlaying && m_MenuTracks != null && m_MenuTracks.Length > 0)
        {
            m_CurrentTrackIndex++;
            if (m_CurrentTrackIndex >= m_MenuTracks.Length)
            {
                m_CurrentTrackIndex = 0;
                if (m_ShuffleTracks)
                {
                    ShuffleTracks(); // Reshuffle for next loop
                }
            }
            PlayCurrentTrack();
        }

        // Handle controller B button for back
        if (UIInputActions.IsCancelPressed())
        {
            if (m_InSettingsPanel)
            {
                OnBackClicked();
            }
        }

        // Auto-select first button if using gamepad and nothing selected
        if (InputModeManager.UseGamepadNavigation)
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            {
                SelectFirstButton();
            }
        }
    }

    private void SelectFirstButton()
    {
        if (m_InSettingsPanel)
        {
            if (m_MasterVolumeSlider != null)
                EventSystem.current.SetSelectedGameObject(m_MasterVolumeSlider.gameObject);
        }
        else
        {
            if (m_PlayButton != null)
                EventSystem.current.SetSelectedGameObject(m_PlayButton.gameObject);
        }
    }

    private void OnPlayClicked()
    {
        if (m_IsLoading) return;

        Debug.Log("[MainMenu] Loading game...");
        if (!string.IsNullOrEmpty(m_GameSceneName))
        {
            if (m_UseAddressables)
            {
                StartCoroutine(LoadGameWithAddressables());
            }
            else
            {
                SceneManager.LoadScene(m_GameSceneName);
            }
        }
    }

    private System.Collections.IEnumerator LoadGameWithAddressables()
    {
        m_IsLoading = true;

        // Show loading UI
        if (m_MainPanel != null) m_MainPanel.SetActive(false);
        if (m_SettingsPanel != null) m_SettingsPanel.SetActive(false);
        if (m_LoadingPanel != null) m_LoadingPanel.SetActive(true);

        UpdateLoadingUI("Connecting to server...", 0f);

        // Load the game scene via Addressables
        var handle = Addressables.LoadSceneAsync(m_GameSceneName, LoadSceneMode.Single, true);

        while (!handle.IsDone)
        {
            float progress = handle.PercentComplete;

            // Show download progress
            if (progress < 0.5f)
            {
                UpdateLoadingUI($"Downloading world data... {progress * 200:F0}%", progress * 2f);
            }
            else
            {
                UpdateLoadingUI($"Loading game... {(progress - 0.5f) * 200:F0}%", progress);
            }

            yield return null;
        }

        if (handle.Status == AsyncOperationStatus.Failed)
        {
            Debug.LogError($"[MainMenu] Failed to load game scene: {handle.OperationException}");
            UpdateLoadingUI("Failed to load game. Check your connection.", 0f);

            // Show error and return to menu after delay
            yield return new WaitForSeconds(3f);

            if (m_LoadingPanel != null) m_LoadingPanel.SetActive(false);
            if (m_MainPanel != null) m_MainPanel.SetActive(true);
            m_IsLoading = false;
            yield break;
        }

        UpdateLoadingUI("Starting game...", 1f);
        Debug.Log("[MainMenu] Game scene loaded successfully!");
    }

    private void UpdateLoadingUI(string text, float progress)
    {
        if (m_LoadingText != null)
            m_LoadingText.text = text;

        if (m_LoadingProgressBar != null)
            m_LoadingProgressBar.value = progress;
    }

    private void OnSettingsClicked()
    {
        ShowSettingsPanel();
    }

    private void OnQuitClicked()
    {
        Debug.Log("[MainMenu] Quit requested");

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    private void OnBackClicked()
    {
        ShowMainPanel();
    }

    private void ShowMainPanel()
    {
        Debug.Log("[MainMenuUI] ShowMainPanel");
        m_InSettingsPanel = false;

        if (m_MainPanel != null)
            m_MainPanel.SetActive(true);
        if (m_SettingsPanel != null)
            m_SettingsPanel.SetActive(false);

        // Select first button for gamepad
        if (InputModeManager.UseGamepadNavigation && m_PlayButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(m_PlayButton.gameObject);
        }
    }

    private void ShowSettingsPanel()
    {
        Debug.Log("[MainMenuUI] ShowSettingsPanel");
        m_InSettingsPanel = true;

        if (m_MainPanel != null)
            m_MainPanel.SetActive(false);
        if (m_SettingsPanel != null)
            m_SettingsPanel.SetActive(true);

        // Select first setting for gamepad
        if (InputModeManager.UseGamepadNavigation && m_MasterVolumeSlider != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(m_MasterVolumeSlider.gameObject);
        }
    }

    #region Settings Handlers

    private void OnMasterVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("MasterVolume", value);
        AudioListener.volume = value;
    }

    private void OnSFXVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        // Apply to SFX audio mixer group if you have one
    }

    private void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        if (m_MusicSource != null)
        {
            m_MusicSource.volume = value;
        }
    }

    private void OnQualityChanged(int index)
    {
        QualitySettings.SetQualityLevel(index);
        PlayerPrefs.SetInt("QualityLevel", index);
    }

    private void OnFullscreenChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }

    #endregion

    private void OnDestroy()
    {
        PlayerPrefs.Save();
    }
}
