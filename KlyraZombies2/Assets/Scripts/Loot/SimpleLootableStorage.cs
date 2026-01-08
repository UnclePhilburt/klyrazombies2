using UnityEngine;
using Opsive.Shared.Events;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;
using Opsive.UltimateInventorySystem.Interactions;

/// <summary>
/// A lootable container that opens the SimpleLootUI (matching inventory style).
/// </summary>
public class SimpleLootableStorage : InteractableBehavior
{
    [Tooltip("Custom name for this container (shown in UI).")]
    [SerializeField] private string m_ContainerName = "Container";

    [Tooltip("The container's inventory (on root object or assigned manually).")]
    [SerializeField] private Inventory m_StorageInventory;

    private bool m_IsMenuOpen = false;
    private Vector3 m_PlayerPositionOnOpen;
    private Transform m_PlayerTransform;
    private GameObject m_PlayerCharacter;
    private Inventory m_PlayerInventory;
    private float m_MovementThreshold = 0.5f;

    private SimpleLootUI m_LootUI;

    private void Awake()
    {
        m_DeactivateOnInteract = false;

        if (m_StorageInventory == null)
        {
            m_StorageInventory = GetComponent<Inventory>();
            if (m_StorageInventory == null)
            {
                m_StorageInventory = GetComponentInParent<Inventory>();
            }
        }
    }

    private void Update()
    {
        if (!m_IsMenuOpen) return;

        // Check if loot UI was closed externally
        if (m_LootUI != null && !m_LootUI.IsOpen)
        {
            OnMenuClosed();
            return;
        }

        // Close menu if player moves
        if (m_PlayerTransform != null)
        {
            float distance = Vector3.Distance(m_PlayerTransform.position, m_PlayerPositionOnOpen);
            if (distance > m_MovementThreshold)
            {
                CloseMenu();
                return;
            }
        }

        // Close on WASD
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) ||
            Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D))
        {
            CloseMenu();
        }
    }

    protected override void Start()
    {
        base.Start();
        FindLootUI();
    }

    private void FindLootUI()
    {
        if (m_LootUI != null) return;

#if UNITY_2023_1_OR_NEWER
        m_LootUI = FindFirstObjectByType<SimpleLootUI>(FindObjectsInactive.Include);
#else
        m_LootUI = FindObjectOfType<SimpleLootUI>(true);
#endif

        // If not found, create one
        if (m_LootUI == null)
        {
            var lootUIObj = new GameObject("SimpleLootUI");
            m_LootUI = lootUIObj.AddComponent<SimpleLootUI>();
        }
    }

    /// <summary>
    /// Called when the player interacts with this container.
    /// </summary>
    protected override void OnInteractInternal(IInteractor interactor)
    {
        if (!(interactor is IInteractorWithInventory interactorWithInventory))
        {
            return;
        }

        Open(interactorWithInventory.Inventory);
    }

    /// <summary>
    /// Open the loot UI showing both inventories.
    /// </summary>
    public void Open(Inventory clientInventory)
    {
        FindLootUI();

        if (m_LootUI == null)
        {
            Debug.LogWarning($"[{gameObject.name}] SimpleLootUI not found!");
            return;
        }

        // Mark highlight as opened (for looted state tracking)
        Transform root = transform.parent != null ? transform.parent : transform;
        var highlight = root.GetComponentInChildren<InteractionHighlight>();
        if (highlight != null)
        {
            highlight.MarkAsOpened();
        }

        // Track player position for movement detection
        m_PlayerTransform = clientInventory.transform;
        m_PlayerPositionOnOpen = m_PlayerTransform.position;
        m_PlayerCharacter = clientInventory.gameObject;
        m_PlayerInventory = clientInventory;
        m_IsMenuOpen = true;

        // Open the loot UI
        m_LootUI.Open(clientInventory, m_StorageInventory, m_ContainerName);
    }

    /// <summary>
    /// Close the loot UI.
    /// </summary>
    public void CloseMenu()
    {
        if (!m_IsMenuOpen) return;

        if (m_LootUI != null)
        {
            m_LootUI.Close();
        }

        OnMenuClosed();
    }

    /// <summary>
    /// Called when the loot UI is closed.
    /// </summary>
    private void OnMenuClosed()
    {
        m_IsMenuOpen = false;
        m_PlayerTransform = null;
        m_PlayerCharacter = null;
        m_PlayerInventory = null;
    }
}
