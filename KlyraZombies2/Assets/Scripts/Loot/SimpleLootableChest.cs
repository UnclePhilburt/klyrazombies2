using UnityEngine;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;
using Opsive.UltimateInventorySystem.UI.Menus.Chest;
using Opsive.UltimateInventorySystem.UI.Panels;

/// <summary>
/// A simplified Chest that doesn't require an Animator.
/// Use this for static containers like barrels, desks, lockers, etc.
/// </summary>
public class SimpleLootableChest : MonoBehaviour, IChest
{
    public event System.Action OnClose;
    public event System.Action<Inventory> OnOpen;

    [Tooltip("The chest menu (auto-finds if not set).")]
    [SerializeField] private ChestMenu m_ChestMenu;

    [Tooltip("The chest inventory (auto-finds on this object if not set).")]
    [SerializeField] private Inventory m_Inventory;

    private bool m_IsOpen;

    public bool IsOpen => m_IsOpen;
    public Inventory Inventory => m_Inventory;

    public ChestMenu ChestMenu
    {
        get => m_ChestMenu;
        set => m_ChestMenu = value;
    }

    private void Awake()
    {
        if (m_Inventory == null)
        {
            m_Inventory = GetComponent<Inventory>();
        }
    }

    private void Start()
    {
        // Auto-find ChestMenu if not assigned (include inactive objects)
        if (m_ChestMenu == null)
        {
#if UNITY_2023_1_OR_NEWER
            m_ChestMenu = FindFirstObjectByType<ChestMenu>(FindObjectsInactive.Include);
#else
            m_ChestMenu = FindObjectOfType<ChestMenu>(true);
#endif

            if (m_ChestMenu == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Could not find ChestMenu component in scene. Make sure the Chest Menu prefab has a ChestMenu component!");
            }
        }
    }

    /// <summary>
    /// Open the chest menu (no animation).
    /// </summary>
    public void Open(Inventory clientInventory)
    {
        // Try to find ChestMenu again if not set (in case it was spawned after Start)
        if (m_ChestMenu == null)
        {
#if UNITY_2023_1_OR_NEWER
            m_ChestMenu = FindFirstObjectByType<ChestMenu>(FindObjectsInactive.Include);
#else
            m_ChestMenu = FindObjectOfType<ChestMenu>(true);
#endif
        }

        if (m_ChestMenu == null)
        {
            // Debug: list all ChestMenu-related components in scene
            var allPanels = FindObjectsByType<Opsive.UltimateInventorySystem.UI.Panels.DisplayPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.LogWarning($"[{gameObject.name}] ChestMenu not found! Found {allPanels.Length} DisplayPanels in scene. Check that your Chest Menu prefab has a ChestMenu component on it.");
            return;
        }

        m_IsOpen = true;

        m_ChestMenu.BindInventory(clientInventory);
        m_ChestMenu.SetChest(this);
        m_ChestMenu.DisplayPanel.SmartOpen();

        OnOpen?.Invoke(clientInventory);
    }

    /// <summary>
    /// Close the chest (no animation).
    /// </summary>
    public void Close()
    {
        m_IsOpen = false;
        OnClose?.Invoke();
    }
}
