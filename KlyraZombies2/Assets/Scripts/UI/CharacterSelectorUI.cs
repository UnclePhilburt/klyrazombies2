using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Character selector UI for the main menu.
/// Allows players to cycle through available characters with prev/next buttons.
/// </summary>
public class CharacterSelectorUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform m_PreviewPoint;
    [SerializeField] private Button m_PrevButton;
    [SerializeField] private Button m_NextButton;
    [SerializeField] private TextMeshProUGUI m_CharacterNameText;
    [SerializeField] private TextMeshProUGUI m_CharacterCountText;

    [Header("Preview Settings")]
    [SerializeField] private float m_RotationSpeed = 20f;
    [SerializeField] private Vector3 m_PreviewOffset = Vector3.zero;
    [SerializeField] private Vector3 m_PreviewRotation = new Vector3(0, 180, 0);
    [SerializeField] private float m_PreviewScale = 1f;

    [Header("Animation")]
    [SerializeField] private string m_IdleAnimationName = "Idle";

    private CharacterDatabase m_Database;
    private int m_CurrentIndex = 0;
    private GameObject m_CurrentPreview;
    private bool m_IsRotating = true;

    private void Awake()
    {
        LoadDatabase();
    }

    private void Start()
    {
        // Setup button listeners
        if (m_PrevButton != null)
            m_PrevButton.onClick.AddListener(SelectPrevious);

        if (m_NextButton != null)
            m_NextButton.onClick.AddListener(SelectNext);

        // Load saved selection
        if (m_Database != null)
        {
            m_CurrentIndex = m_Database.GetSelectedIndex();
            m_CurrentIndex = Mathf.Clamp(m_CurrentIndex, 0, Mathf.Max(0, m_Database.CharacterCount - 1));
        }

        UpdatePreview();
    }

    private void Update()
    {
        // Slowly rotate preview character
        if (m_IsRotating && m_CurrentPreview != null)
        {
            m_CurrentPreview.transform.Rotate(Vector3.up, m_RotationSpeed * Time.deltaTime);
        }
    }

    private void LoadDatabase()
    {
        m_Database = CharacterDatabase.Instance;
        if (m_Database == null)
        {
            Debug.LogError("[CharacterSelectorUI] CharacterDatabase not found in Resources!");
        }
    }

    public void SelectPrevious()
    {
        if (m_Database == null || m_Database.CharacterCount == 0) return;

        m_CurrentIndex--;
        if (m_CurrentIndex < 0)
            m_CurrentIndex = m_Database.CharacterCount - 1;

        SaveSelection();
        UpdatePreview();
    }

    public void SelectNext()
    {
        if (m_Database == null || m_Database.CharacterCount == 0) return;

        m_CurrentIndex++;
        if (m_CurrentIndex >= m_Database.CharacterCount)
            m_CurrentIndex = 0;

        SaveSelection();
        UpdatePreview();
    }

    public void SelectCharacter(int index)
    {
        if (m_Database == null || m_Database.CharacterCount == 0) return;

        m_CurrentIndex = Mathf.Clamp(index, 0, m_Database.CharacterCount - 1);
        SaveSelection();
        UpdatePreview();
    }

    private void SaveSelection()
    {
        if (m_Database != null)
        {
            m_Database.SetSelectedCharacter(m_CurrentIndex);
        }
    }

    private void UpdatePreview()
    {
        // Destroy old preview
        if (m_CurrentPreview != null)
        {
            Destroy(m_CurrentPreview);
            m_CurrentPreview = null;
        }

        if (m_Database == null || m_Database.CharacterCount == 0)
        {
            UpdateUI("No Characters", 0, 0);
            return;
        }

        // Get current character data
        CharacterData charData = m_Database.GetCharacter(m_CurrentIndex);
        if (charData == null)
        {
            UpdateUI("Invalid", m_CurrentIndex + 1, m_Database.CharacterCount);
            return;
        }

        // Spawn preview
        GameObject prefab = charData.GetPrefab();
        if (prefab != null && m_PreviewPoint != null)
        {
            m_CurrentPreview = Instantiate(prefab, m_PreviewPoint);
            m_CurrentPreview.transform.localPosition = m_PreviewOffset;
            m_CurrentPreview.transform.localRotation = Quaternion.Euler(m_PreviewRotation);
            m_CurrentPreview.transform.localScale = Vector3.one * m_PreviewScale;

            // Set layer to UI or a preview layer if needed
            SetLayerRecursively(m_CurrentPreview, m_PreviewPoint.gameObject.layer);

            // Disable any components that shouldn't be active in preview
            DisablePreviewComponents(m_CurrentPreview);

            // Try to play idle animation
            PlayIdleAnimation(m_CurrentPreview);
        }

        // Update UI
        UpdateUI(charData.displayName, m_CurrentIndex + 1, m_Database.CharacterCount);
    }

    private void UpdateUI(string characterName, int current, int total)
    {
        if (m_CharacterNameText != null)
            m_CharacterNameText.text = characterName;

        if (m_CharacterCountText != null)
            m_CharacterCountText.text = $"{current} / {total}";
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void DisablePreviewComponents(GameObject obj)
    {
        // Disable colliders
        foreach (var collider in obj.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        // Disable rigidbodies
        foreach (var rb in obj.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
        }

        // Disable audio sources
        foreach (var audio in obj.GetComponentsInChildren<AudioSource>())
        {
            audio.enabled = false;
        }

        // Disable any AI or character controllers
        foreach (var behaviour in obj.GetComponentsInChildren<MonoBehaviour>())
        {
            // Keep Animator
            if (behaviour is Animator)
                continue;

            // Disable other behaviours
            string typeName = behaviour.GetType().Name;
            if (typeName.Contains("Controller") || typeName.Contains("AI") ||
                typeName.Contains("Character") || typeName.Contains("Input"))
            {
                behaviour.enabled = false;
            }
        }
    }

    private void PlayIdleAnimation(GameObject obj)
    {
        var animator = obj.GetComponent<Animator>();
        if (animator == null)
            animator = obj.GetComponentInChildren<Animator>();

        if (animator != null && !string.IsNullOrEmpty(m_IdleAnimationName))
        {
            // Try to play idle animation
            animator.Play(m_IdleAnimationName, 0, 0);
        }
    }

    public void SetRotating(bool rotating)
    {
        m_IsRotating = rotating;
    }

    public int CurrentIndex => m_CurrentIndex;
    public CharacterData CurrentCharacter => m_Database?.GetCharacter(m_CurrentIndex);

    private void OnDestroy()
    {
        if (m_PrevButton != null)
            m_PrevButton.onClick.RemoveListener(SelectPrevious);

        if (m_NextButton != null)
            m_NextButton.onClick.RemoveListener(SelectNext);
    }
}
