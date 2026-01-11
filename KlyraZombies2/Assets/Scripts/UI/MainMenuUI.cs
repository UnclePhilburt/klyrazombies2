using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
    [SerializeField] private string m_GameSceneName = "MainMap";

    [Header("Character Selection")]
    [SerializeField] private CharacterSelectorUI m_CharacterSelector;
    [SerializeField] private Button m_PrevCharacterButton;
    [SerializeField] private Button m_NextCharacterButton;

    private int m_CurrentTrackIndex = 0;
    private int[] m_ShuffledIndices;

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

        // Setup character selector buttons
        if (m_PrevCharacterButton != null && m_CharacterSelector != null)
            m_PrevCharacterButton.onClick.AddListener(m_CharacterSelector.SelectPrevious);

        if (m_NextCharacterButton != null && m_CharacterSelector != null)
            m_NextCharacterButton.onClick.AddListener(m_CharacterSelector.SelectNext);

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
    }

    private void OnPlayClicked()
    {
        Debug.Log("[MainMenu] Loading game...");
        if (!string.IsNullOrEmpty(m_GameSceneName))
        {
            SceneManager.LoadScene(m_GameSceneName);
        }
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
        if (m_MainPanel != null)
            m_MainPanel.SetActive(true);
        if (m_SettingsPanel != null)
            m_SettingsPanel.SetActive(false);
    }

    private void ShowSettingsPanel()
    {
        Debug.Log("[MainMenuUI] ShowSettingsPanel");
        if (m_MainPanel != null)
            m_MainPanel.SetActive(false);
        if (m_SettingsPanel != null)
            m_SettingsPanel.SetActive(true);
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
