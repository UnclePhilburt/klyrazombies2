using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component for selecting a character in the main menu
/// </summary>
public class CharacterSelector : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button m_LeftButton;
    [SerializeField] private Button m_RightButton;
    [SerializeField] private TextMeshProUGUI m_CharacterNameText;
    [SerializeField] private TextMeshProUGUI m_CharacterDescText;
    [SerializeField] private Image m_CharacterPortrait;

    [Header("3D Preview (Optional)")]
    [SerializeField] private Transform m_PreviewSpawnPoint;
    [SerializeField] private float m_PreviewRotationSpeed = 30f;

    [Header("Database")]
    [SerializeField] private CharacterDatabase m_Database;

    private int m_CurrentIndex = 0;
    private GameObject m_PreviewInstance;

    private void Start()
    {
        // Load database if not assigned
        if (m_Database == null)
        {
            m_Database = CharacterDatabase.Instance;
        }

        if (m_Database == null || m_Database.CharacterCount == 0)
        {
            Debug.LogWarning("[CharacterSelector] No character database or empty database!");
            gameObject.SetActive(false);
            return;
        }

        // Setup button listeners
        if (m_LeftButton != null)
            m_LeftButton.onClick.AddListener(PreviousCharacter);

        if (m_RightButton != null)
            m_RightButton.onClick.AddListener(NextCharacter);

        // Load saved selection
        m_CurrentIndex = m_Database.GetSelectedIndex();
        m_CurrentIndex = Mathf.Clamp(m_CurrentIndex, 0, m_Database.CharacterCount - 1);

        UpdateDisplay();
    }

    private void Update()
    {
        // Rotate preview model
        if (m_PreviewInstance != null)
        {
            m_PreviewInstance.transform.Rotate(Vector3.up, m_PreviewRotationSpeed * Time.deltaTime);
        }

        // Keyboard navigation
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            PreviousCharacter();
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            NextCharacter();
        }
    }

    public void NextCharacter()
    {
        if (m_Database == null || m_Database.CharacterCount == 0) return;

        m_CurrentIndex++;
        if (m_CurrentIndex >= m_Database.CharacterCount)
            m_CurrentIndex = 0;

        SaveAndUpdateDisplay();
    }

    public void PreviousCharacter()
    {
        if (m_Database == null || m_Database.CharacterCount == 0) return;

        m_CurrentIndex--;
        if (m_CurrentIndex < 0)
            m_CurrentIndex = m_Database.CharacterCount - 1;

        SaveAndUpdateDisplay();
    }

    private void SaveAndUpdateDisplay()
    {
        m_Database.SetSelectedCharacter(m_CurrentIndex);
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        CharacterData character = m_Database.GetCharacter(m_CurrentIndex);
        if (character == null) return;

        // Update name
        if (m_CharacterNameText != null)
        {
            m_CharacterNameText.text = character.displayName;
        }

        // Update description
        if (m_CharacterDescText != null)
        {
            m_CharacterDescText.text = character.description;
        }

        // Update portrait
        if (m_CharacterPortrait != null)
        {
            if (character.portrait != null)
            {
                m_CharacterPortrait.sprite = character.portrait;
                m_CharacterPortrait.enabled = true;
            }
            else
            {
                m_CharacterPortrait.enabled = false;
            }
        }

        // Update 3D preview
        UpdatePreviewModel(character);
    }

    private void UpdatePreviewModel(CharacterData character)
    {
        if (m_PreviewSpawnPoint == null)
        {
            Debug.LogWarning("[CharacterSelector] PreviewSpawnPoint is null - cannot spawn preview");
            return;
        }

        // Destroy old preview
        if (m_PreviewInstance != null)
        {
            Destroy(m_PreviewInstance);
            m_PreviewInstance = null;
        }

        // Spawn new preview
        GameObject prefab = character.GetPrefab();
        if (prefab != null)
        {
            Debug.Log($"[CharacterSelector] Spawning preview: {prefab.name} at {m_PreviewSpawnPoint.position}");
            m_PreviewInstance = Instantiate(prefab, m_PreviewSpawnPoint.position, m_PreviewSpawnPoint.rotation);
            m_PreviewInstance.transform.SetParent(m_PreviewSpawnPoint);
            m_PreviewInstance.transform.localPosition = Vector3.zero;
            m_PreviewInstance.transform.localRotation = Quaternion.identity;

            // Disable any scripts that might interfere
            DisableComponents(m_PreviewInstance);

            Debug.Log($"[CharacterSelector] Preview spawned successfully at world pos: {m_PreviewInstance.transform.position}");
        }
        else
        {
            Debug.LogWarning($"[CharacterSelector] Character '{character.displayName}' has no prefab!");
        }
    }

    private void DisableComponents(GameObject obj)
    {
        // Disable animators (or set to idle)
        var animator = obj.GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = true; // Keep animator for idle pose
        }

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

        // Disable any MonoBehaviours except Animator
        foreach (var mb in obj.GetComponentsInChildren<MonoBehaviour>())
        {
            if (!(mb is Animator))
            {
                mb.enabled = false;
            }
        }
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void OnDestroy()
    {
        if (m_PreviewInstance != null)
        {
            Destroy(m_PreviewInstance);
        }
    }

    /// <summary>
    /// Get the currently selected character data
    /// </summary>
    public CharacterData GetSelectedCharacter()
    {
        return m_Database?.GetCharacter(m_CurrentIndex);
    }
}
