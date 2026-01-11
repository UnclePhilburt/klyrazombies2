using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Synty.SidekickCharacters.Enums;
using System.Collections;

/// <summary>
/// UI controller for character creation screen.
/// Allows players to customize their Sidekick character's face parts, body shape, and colors.
/// Characters start in underwear only - clothing is found in-game.
/// </summary>
public class CharacterCreationUI : MonoBehaviour
{
    [Header("Character Controller")]
    [SerializeField] private SidekickPlayerController m_CharacterController;

    [Header("Body Shape Sliders")]
    [SerializeField] private Slider m_BodyTypeSlider; // Masculine/Feminine
    [SerializeField] private Slider m_MusclesSlider;
    [SerializeField] private Slider m_BodySizeSlider; // Skinny/Heavy

    [Header("Body Shape Labels")]
    [SerializeField] private TextMeshProUGUI m_BodyTypeLabel;
    [SerializeField] private TextMeshProUGUI m_MusclesLabel;
    [SerializeField] private TextMeshProUGUI m_BodySizeLabel;

    [Header("Hair Navigation")]
    [SerializeField] private Button m_PrevHairButton;
    [SerializeField] private Button m_NextHairButton;
    [SerializeField] private TextMeshProUGUI m_HairLabel;

    [Header("Eyebrows Navigation")]
    [SerializeField] private Button m_PrevEyebrowsButton;
    [SerializeField] private Button m_NextEyebrowsButton;
    [SerializeField] private TextMeshProUGUI m_EyebrowsLabel;

    [Header("Nose Navigation")]
    [SerializeField] private Button m_PrevNoseButton;
    [SerializeField] private Button m_NextNoseButton;
    [SerializeField] private TextMeshProUGUI m_NoseLabel;

    [Header("Ears Navigation")]
    [SerializeField] private Button m_PrevEarsButton;
    [SerializeField] private Button m_NextEarsButton;
    [SerializeField] private TextMeshProUGUI m_EarsLabel;

    [Header("Facial Hair Navigation")]
    [SerializeField] private Button m_PrevFacialHairButton;
    [SerializeField] private Button m_NextFacialHairButton;
    [SerializeField] private TextMeshProUGUI m_FacialHairLabel;
    [SerializeField] private Button m_ClearFacialHairButton;

    [Header("Color Pickers")]
    [SerializeField] private Image m_SkinColorPreview;
    [SerializeField] private Button m_PrevSkinColorButton;
    [SerializeField] private Button m_NextSkinColorButton;

    [SerializeField] private Image m_HairColorPreview;
    [SerializeField] private Button m_PrevHairColorButton;
    [SerializeField] private Button m_NextHairColorButton;

    [SerializeField] private Image m_EyeColorPreview;
    [SerializeField] private Button m_PrevEyeColorButton;
    [SerializeField] private Button m_NextEyeColorButton;

    [Header("Action Buttons")]
    [SerializeField] private Button m_RandomizeButton;
    [SerializeField] private Button m_ConfirmButton;
    [SerializeField] private Button m_BackButton;


    [Header("Camera")]
    [SerializeField] private Transform m_CharacterSpawnPoint;
    [SerializeField] private float m_RotationSpeed = 100f;

    [Header("Scene Loading")]
    [SerializeField] private string m_GameSceneName = "MainMap";

    [Header("Save Key")]
    [SerializeField] private string m_SaveKey = "PlayerCharacter";

    // Rotation
    private bool m_IsDragging = false;
    private float m_CurrentRotation = 180f; // Start facing camera

    // Color presets
    private Color[] m_SkinColorPresets = new Color[]
    {
        new Color(1f, 0.87f, 0.77f),      // Light
        new Color(0.96f, 0.80f, 0.69f),   // Fair
        new Color(0.87f, 0.72f, 0.53f),   // Medium
        new Color(0.76f, 0.57f, 0.42f),   // Olive
        new Color(0.55f, 0.38f, 0.28f),   // Brown
        new Color(0.36f, 0.25f, 0.20f)    // Dark
    };

    private Color[] m_HairColorPresets = new Color[]
    {
        new Color(0.1f, 0.05f, 0.02f),    // Black
        new Color(0.25f, 0.15f, 0.08f),   // Dark brown
        new Color(0.45f, 0.30f, 0.15f),   // Brown
        new Color(0.65f, 0.45f, 0.25f),   // Light brown
        new Color(0.85f, 0.65f, 0.35f),   // Blonde
        new Color(0.95f, 0.85f, 0.55f),   // Light blonde
        new Color(0.55f, 0.15f, 0.08f),   // Auburn
        new Color(0.75f, 0.25f, 0.10f),   // Red
        new Color(0.5f, 0.5f, 0.5f),      // Gray
        new Color(0.9f, 0.9f, 0.95f)      // White
    };

    private Color[] m_EyeColorPresets = new Color[]
    {
        new Color(0.25f, 0.15f, 0.08f),   // Brown
        new Color(0.45f, 0.30f, 0.15f),   // Light brown / Hazel
        new Color(0.35f, 0.55f, 0.35f),   // Green
        new Color(0.30f, 0.45f, 0.65f),   // Blue
        new Color(0.50f, 0.55f, 0.65f),   // Gray-blue
        new Color(0.20f, 0.20f, 0.25f)    // Dark
    };

    private int m_CurrentSkinColorIndex = 2;
    private int m_CurrentHairColorIndex = 0;
    private int m_CurrentEyeColorIndex = 0;

    private bool m_Initialized = false;

    private void Start()
    {
        InitializeUI();
    }

    private void OnEnable()
    {
        // Force layout rebuild when panel becomes visible
        StartCoroutine(ForceLayoutRebuild());

        // Initialize if not done yet
        if (!m_Initialized)
        {
            InitializeUI();
        }
    }

    private IEnumerator ForceLayoutRebuild()
    {
        yield return null; // Wait one frame

        // Force all layout groups to rebuild
        var layoutGroups = GetComponentsInChildren<LayoutGroup>(true);
        foreach (var lg in layoutGroups)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(lg.GetComponent<RectTransform>());
        }

        // Also rebuild content size fitters
        var fitters = GetComponentsInChildren<ContentSizeFitter>(true);
        foreach (var f in fitters)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(f.GetComponent<RectTransform>());
        }

        Debug.Log($"[CharacterCreationUI] Rebuilt {layoutGroups.Length} layout groups and {fitters.Length} content fitters");
    }

    private void InitializeUI()
    {
        if (m_Initialized) return;
        m_Initialized = true;

        Debug.Log("[CharacterCreationUI] Initializing...");

        SetupUI();

        // Initialize character controller first before using it
        if (m_CharacterController != null)
        {
            // Ensure controller is initialized
            if (!m_CharacterController.IsInitialized)
            {
                m_CharacterController.Initialize();
            }

            // Set underwear mode
            m_CharacterController.SetUnderwearOnly(true);
        }

        LoadSavedCharacter();

        Debug.Log("[CharacterCreationUI] Initialization complete");
    }

    private void SetupUI()
    {
        // Body shape sliders
        SetupSlider(m_BodyTypeSlider, -100f, 100f, 0f, OnBodyTypeChanged);
        SetupSlider(m_MusclesSlider, 0f, 100f, 50f, OnMusclesChanged);
        SetupSlider(m_BodySizeSlider, -100f, 100f, 0f, OnBodySizeChanged);

        // Part navigation buttons
        SetupNavButtons(m_PrevHairButton, m_NextHairButton, OnPrevHair, OnNextHair);
        SetupNavButtons(m_PrevEyebrowsButton, m_NextEyebrowsButton, OnPrevEyebrows, OnNextEyebrows);
        SetupNavButtons(m_PrevNoseButton, m_NextNoseButton, OnPrevNose, OnNextNose);
        SetupNavButtons(m_PrevEarsButton, m_NextEarsButton, OnPrevEars, OnNextEars);
        SetupNavButtons(m_PrevFacialHairButton, m_NextFacialHairButton, OnPrevFacialHair, OnNextFacialHair);

        if (m_ClearFacialHairButton != null)
            m_ClearFacialHairButton.onClick.AddListener(OnClearFacialHair);

        // Color navigation buttons
        SetupNavButtons(m_PrevSkinColorButton, m_NextSkinColorButton, OnPrevSkinColor, OnNextSkinColor);
        SetupNavButtons(m_PrevHairColorButton, m_NextHairColorButton, OnPrevHairColor, OnNextHairColor);
        SetupNavButtons(m_PrevEyeColorButton, m_NextEyeColorButton, OnPrevEyeColor, OnNextEyeColor);

        // Action buttons
        if (m_RandomizeButton != null)
            m_RandomizeButton.onClick.AddListener(OnRandomize);

        if (m_ConfirmButton != null)
            m_ConfirmButton.onClick.AddListener(OnConfirm);

        if (m_BackButton != null)
            m_BackButton.onClick.AddListener(OnBack);

        // Subscribe to character built event
        if (m_CharacterController != null)
        {
            m_CharacterController.OnCharacterBuilt += OnCharacterBuilt;
        }

        UpdateAllLabels();
        UpdateColorPreviews();
    }

    private void SetupSlider(Slider slider, float min, float max, float defaultValue, UnityEngine.Events.UnityAction<float> callback)
    {
        if (slider != null)
        {
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = defaultValue;
            slider.onValueChanged.AddListener(callback);
        }
    }

    private void SetupNavButtons(Button prev, Button next, UnityEngine.Events.UnityAction onPrev, UnityEngine.Events.UnityAction onNext)
    {
        if (prev != null) prev.onClick.AddListener(onPrev);
        if (next != null) next.onClick.AddListener(onNext);
    }

    private void Update()
    {
        // Rotate character with mouse drag - only when NOT over UI
        if (m_CharacterController?.SpawnedCharacter != null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                // Only start dragging if not clicking on UI
                if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    m_IsDragging = true;
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                m_IsDragging = false;
            }

            if (m_IsDragging)
            {
                float delta = Input.GetAxis("Mouse X") * m_RotationSpeed * Time.deltaTime;
                m_CurrentRotation -= delta;
                m_CharacterController.SpawnedCharacter.transform.rotation = Quaternion.Euler(0, m_CurrentRotation, 0);
            }
        }
    }

    private void OnCharacterBuilt(GameObject character)
    {
        // Position character at spawn point
        if (m_CharacterSpawnPoint != null && character != null)
        {
            character.transform.position = m_CharacterSpawnPoint.position;
            character.transform.rotation = Quaternion.Euler(0, m_CurrentRotation, 0);
        }

        UpdateAllLabels();
    }

    #region Label Updates

    private void UpdateAllLabels()
    {
        UpdateBodyShapeLabels();
        UpdatePartLabels();
    }

    private void UpdateBodyShapeLabels()
    {
        if (m_BodyTypeLabel != null)
        {
            float val = m_BodyTypeSlider != null ? m_BodyTypeSlider.value : 0;
            string typeText = val < -30 ? "Masculine" : val > 30 ? "Feminine" : "Neutral";
            m_BodyTypeLabel.text = $"Body Type: {typeText}";
        }

        if (m_MusclesLabel != null)
        {
            float val = m_MusclesSlider != null ? m_MusclesSlider.value : 50;
            string muscleText = val < 30 ? "Slim" : val > 70 ? "Muscular" : "Average";
            m_MusclesLabel.text = $"Build: {muscleText}";
        }

        if (m_BodySizeLabel != null)
        {
            float val = m_BodySizeSlider != null ? m_BodySizeSlider.value : 0;
            string sizeText = val < -30 ? "Thin" : val > 30 ? "Heavy" : "Average";
            m_BodySizeLabel.text = $"Size: {sizeText}";
        }
    }

    private void UpdatePartLabels()
    {
        if (m_CharacterController == null) return;

        UpdatePartLabel(m_HairLabel, CharacterPartType.Hair, "Hair");
        UpdatePartLabel(m_EyebrowsLabel, CharacterPartType.EyebrowLeft, "Eyebrows");
        UpdatePartLabel(m_NoseLabel, CharacterPartType.Nose, "Nose");
        UpdatePartLabel(m_EarsLabel, CharacterPartType.EarLeft, "Ears");
        UpdatePartLabel(m_FacialHairLabel, CharacterPartType.FacialHair, "Facial Hair");
    }

    private void UpdatePartLabel(TextMeshProUGUI label, CharacterPartType partType, string partName)
    {
        if (label == null || m_CharacterController == null) return;

        var parts = m_CharacterController.GetPartsForType(partType);
        int currentIndex = m_CharacterController.GetCurrentPartIndex(partType);
        var currentPart = m_CharacterController.GetCurrentPart(partType);

        if (currentPart == null)
        {
            label.text = $"{partName}: None";
        }
        else
        {
            label.text = $"{partName}: {currentIndex + 1}/{parts.Count}";
        }
    }

    private void UpdateColorPreviews()
    {
        if (m_SkinColorPreview != null)
            m_SkinColorPreview.color = m_SkinColorPresets[m_CurrentSkinColorIndex];

        if (m_HairColorPreview != null)
            m_HairColorPreview.color = m_HairColorPresets[m_CurrentHairColorIndex];

        if (m_EyeColorPreview != null)
            m_EyeColorPreview.color = m_EyeColorPresets[m_CurrentEyeColorIndex];
    }

    #endregion

    #region Body Shape Handlers

    private void OnBodyTypeChanged(float value)
    {
        UpdateBodyShape();
    }

    private void OnMusclesChanged(float value)
    {
        UpdateBodyShape();
    }

    private void OnBodySizeChanged(float value)
    {
        UpdateBodyShape();
    }

    private void UpdateBodyShape()
    {
        if (m_CharacterController == null) return;

        float bodyType = m_BodyTypeSlider != null ? m_BodyTypeSlider.value : 0;
        float muscles = m_MusclesSlider != null ? m_MusclesSlider.value : 50;
        float bodySize = m_BodySizeSlider != null ? m_BodySizeSlider.value : 0;

        m_CharacterController.SetBodyShape(bodyType, muscles, bodySize);
        UpdateBodyShapeLabels();
    }

    #endregion

    #region Part Navigation Handlers

    private void OnPrevHair()
    {
        m_CharacterController?.PreviousPart(CharacterPartType.Hair);
        UpdatePartLabels();
    }

    private void OnNextHair()
    {
        m_CharacterController?.NextPart(CharacterPartType.Hair);
        UpdatePartLabels();
    }

    private void OnPrevEyebrows()
    {
        m_CharacterController?.PreviousPart(CharacterPartType.EyebrowLeft);
        UpdatePartLabels();
    }

    private void OnNextEyebrows()
    {
        m_CharacterController?.NextPart(CharacterPartType.EyebrowLeft);
        UpdatePartLabels();
    }

    private void OnPrevNose()
    {
        m_CharacterController?.PreviousPart(CharacterPartType.Nose);
        UpdatePartLabels();
    }

    private void OnNextNose()
    {
        m_CharacterController?.NextPart(CharacterPartType.Nose);
        UpdatePartLabels();
    }

    private void OnPrevEars()
    {
        m_CharacterController?.PreviousPart(CharacterPartType.EarLeft);
        UpdatePartLabels();
    }

    private void OnNextEars()
    {
        m_CharacterController?.NextPart(CharacterPartType.EarLeft);
        UpdatePartLabels();
    }

    private void OnPrevFacialHair()
    {
        m_CharacterController?.PreviousPart(CharacterPartType.FacialHair);
        UpdatePartLabels();
    }

    private void OnNextFacialHair()
    {
        m_CharacterController?.NextPart(CharacterPartType.FacialHair);
        UpdatePartLabels();
    }

    private void OnClearFacialHair()
    {
        m_CharacterController?.ClearPart(CharacterPartType.FacialHair);
        UpdatePartLabels();
    }

    #endregion

    #region Color Navigation Handlers

    private void OnPrevSkinColor()
    {
        m_CurrentSkinColorIndex--;
        if (m_CurrentSkinColorIndex < 0) m_CurrentSkinColorIndex = m_SkinColorPresets.Length - 1;
        ApplySkinColor();
    }

    private void OnNextSkinColor()
    {
        m_CurrentSkinColorIndex = (m_CurrentSkinColorIndex + 1) % m_SkinColorPresets.Length;
        ApplySkinColor();
    }

    private void ApplySkinColor()
    {
        m_CharacterController?.SetSkinColor(m_SkinColorPresets[m_CurrentSkinColorIndex]);
        UpdateColorPreviews();
    }

    private void OnPrevHairColor()
    {
        m_CurrentHairColorIndex--;
        if (m_CurrentHairColorIndex < 0) m_CurrentHairColorIndex = m_HairColorPresets.Length - 1;
        ApplyHairColor();
    }

    private void OnNextHairColor()
    {
        m_CurrentHairColorIndex = (m_CurrentHairColorIndex + 1) % m_HairColorPresets.Length;
        ApplyHairColor();
    }

    private void ApplyHairColor()
    {
        m_CharacterController?.SetHairColor(m_HairColorPresets[m_CurrentHairColorIndex]);
        UpdateColorPreviews();
    }

    private void OnPrevEyeColor()
    {
        m_CurrentEyeColorIndex--;
        if (m_CurrentEyeColorIndex < 0) m_CurrentEyeColorIndex = m_EyeColorPresets.Length - 1;
        ApplyEyeColor();
    }

    private void OnNextEyeColor()
    {
        m_CurrentEyeColorIndex = (m_CurrentEyeColorIndex + 1) % m_EyeColorPresets.Length;
        ApplyEyeColor();
    }

    private void ApplyEyeColor()
    {
        m_CharacterController?.SetEyeColor(m_EyeColorPresets[m_CurrentEyeColorIndex]);
        UpdateColorPreviews();
    }

    #endregion

    #region Action Handlers

    private void OnRandomize()
    {
        // Randomize everything including individual parts and colors
        m_CharacterController?.RandomizeAll();

        // Update slider values to match
        var saveData = m_CharacterController?.GetSaveData();
        if (saveData != null)
        {
            if (m_BodyTypeSlider != null) m_BodyTypeSlider.value = saveData.bodyType;
            if (m_MusclesSlider != null) m_MusclesSlider.value = saveData.muscles;
            if (m_BodySizeSlider != null) m_BodySizeSlider.value = saveData.bodySize;

            // Find closest color indices
            m_CurrentSkinColorIndex = FindClosestColorIndex(
                ColorSaveHelper.FromHex(saveData.skinColorHex, m_SkinColorPresets[2]),
                m_SkinColorPresets);
            m_CurrentHairColorIndex = FindClosestColorIndex(
                ColorSaveHelper.FromHex(saveData.hairColorHex, m_HairColorPresets[0]),
                m_HairColorPresets);
            m_CurrentEyeColorIndex = FindClosestColorIndex(
                ColorSaveHelper.FromHex(saveData.eyeColorHex, m_EyeColorPresets[0]),
                m_EyeColorPresets);
        }

        UpdateAllLabels();
        UpdateColorPreviews();
    }

    private int FindClosestColorIndex(Color color, Color[] presets)
    {
        int closest = 0;
        float minDist = float.MaxValue;

        for (int i = 0; i < presets.Length; i++)
        {
            float dist = ColorDistance(color, presets[i]);
            if (dist < minDist)
            {
                minDist = dist;
                closest = i;
            }
        }

        return closest;
    }

    private float ColorDistance(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
    }

    private void OnConfirm()
    {
        SaveCharacter();

        Debug.Log("[CharacterCreationUI] Character confirmed and saved! Loading game...");

        // Load the game scene
        if (!string.IsNullOrEmpty(m_GameSceneName))
        {
            SceneManager.LoadScene(m_GameSceneName);
        }
    }

    private void OnBack()
    {
        // Go back to main menu
        Debug.Log("[CharacterCreationUI] Returning to main menu");
        gameObject.SetActive(false);
    }

    #endregion

    #region Save/Load

    /// <summary>
    /// Save the current character to PlayerPrefs.
    /// </summary>
    public void SaveCharacter()
    {
        if (m_CharacterController == null) return;

        var saveData = m_CharacterController.GetSaveData();
        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(m_SaveKey, json);
        PlayerPrefs.Save();

        Debug.Log($"[CharacterCreationUI] Saved character: {json}");
    }

    /// <summary>
    /// Load saved character from PlayerPrefs.
    /// </summary>
    public void LoadSavedCharacter()
    {
        if (m_CharacterController == null) return;

        // Ensure controller is initialized
        if (!m_CharacterController.IsInitialized)
        {
            if (!m_CharacterController.Initialize())
            {
                Debug.LogError("[CharacterCreationUI] Failed to initialize character controller");
                return;
            }
        }

        bool loadedSaveData = false;
        string json = PlayerPrefs.GetString(m_SaveKey, "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                var saveData = JsonUtility.FromJson<CharacterSaveData>(json);
                m_CharacterController.LoadSaveData(saveData);
                loadedSaveData = true;

                // Update sliders
                if (m_BodyTypeSlider != null) m_BodyTypeSlider.value = saveData.bodyType;
                if (m_MusclesSlider != null) m_MusclesSlider.value = saveData.muscles;
                if (m_BodySizeSlider != null) m_BodySizeSlider.value = saveData.bodySize;

                // Update color indices
                if (!string.IsNullOrEmpty(saveData.skinColorHex))
                {
                    m_CurrentSkinColorIndex = FindClosestColorIndex(
                        ColorSaveHelper.FromHex(saveData.skinColorHex, m_SkinColorPresets[2]),
                        m_SkinColorPresets);
                }
                if (!string.IsNullOrEmpty(saveData.hairColorHex))
                {
                    m_CurrentHairColorIndex = FindClosestColorIndex(
                        ColorSaveHelper.FromHex(saveData.hairColorHex, m_HairColorPresets[0]),
                        m_HairColorPresets);
                }
                if (!string.IsNullOrEmpty(saveData.eyeColorHex))
                {
                    m_CurrentEyeColorIndex = FindClosestColorIndex(
                        ColorSaveHelper.FromHex(saveData.eyeColorHex, m_EyeColorPresets[0]),
                        m_EyeColorPresets);
                }

                Debug.Log($"[CharacterCreationUI] Loaded saved character");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CharacterCreationUI] Failed to load saved character: {e.Message}");
            }
        }

        // If no saved data was loaded, apply default appearance
        // LoadSaveData calls ApplyAppearance internally, but we need to call it
        // when there's no saved data to build the character with defaults
        if (!loadedSaveData)
        {
            Debug.Log("[CharacterCreationUI] No saved character, applying default appearance");
            m_CharacterController.ApplyAppearance();
        }

        UpdateAllLabels();
        UpdateColorPreviews();
    }

    #endregion

    private void OnDestroy()
    {
        if (m_CharacterController != null)
        {
            m_CharacterController.OnCharacterBuilt -= OnCharacterBuilt;
        }
    }
}
