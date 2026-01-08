using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Opsive.UltimateInventorySystem.Core.DataStructures;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;
using Opsive.UltimateInventorySystem.UI.Item;
using Opsive.UltimateInventorySystem.Demo.UI.Menus.Storage;

/// <summary>
/// Handles right-click quick transfer of items in the Storage Menu.
/// Right-clicking an item in either inventory transfers it to the other inventory.
/// Attach this to the Storage Menu prefab or as a child of it.
/// </summary>
public class StorageQuickTransfer : MonoBehaviour
{
    [Tooltip("Reference to the StorageMenu (auto-found if not set).")]
    [SerializeField] private StorageMenu m_StorageMenu;

    private GraphicRaycaster m_Raycaster;
    private EventSystem m_EventSystem;
    private PointerEventData m_PointerEventData;
    private List<RaycastResult> m_RaycastResults = new List<RaycastResult>();

    // These are set by SimpleLootableStorage when opening the menu
    private Inventory m_PlayerInventory;
    private Inventory m_ContainerInventory;

    private static StorageQuickTransfer s_Instance;
    public static StorageQuickTransfer Instance => s_Instance;

    private void Awake()
    {
        s_Instance = this;

        if (m_StorageMenu == null)
        {
            m_StorageMenu = GetComponent<StorageMenu>();
            if (m_StorageMenu == null)
            {
                m_StorageMenu = GetComponentInParent<StorageMenu>();
            }
        }
    }

    /// <summary>
    /// Called by SimpleLootableStorage to set the inventory references.
    /// </summary>
    public void SetInventories(Inventory playerInventory, Inventory containerInventory)
    {
        m_PlayerInventory = playerInventory;
        m_ContainerInventory = containerInventory;
    }

    /// <summary>
    /// Clear inventory references when menu closes.
    /// </summary>
    public void ClearInventories()
    {
        m_PlayerInventory = null;
        m_ContainerInventory = null;
    }

    private void Start()
    {
        // Find the GraphicRaycaster - search up the hierarchy for a canvas with one
        var canvas = GetComponentInParent<Canvas>();
        while (canvas != null)
        {
            m_Raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (m_Raycaster != null)
                break;

            // Check if there's a parent canvas
            if (canvas.transform.parent != null)
            {
                canvas = canvas.transform.parent.GetComponentInParent<Canvas>();
            }
            else
            {
                break;
            }
        }

        // If still not found, search scene-wide
        if (m_Raycaster == null)
        {
#if UNITY_2023_1_OR_NEWER
            m_Raycaster = FindFirstObjectByType<GraphicRaycaster>();
#else
            m_Raycaster = FindObjectOfType<GraphicRaycaster>();
#endif
        }

        m_EventSystem = EventSystem.current;
    }

    private void Update()
    {
        // Only process when the storage menu is active and visible
        if (m_StorageMenu == null || !m_StorageMenu.gameObject.activeInHierarchy)
            return;

        // Check for right-click
        if (Input.GetMouseButtonDown(1))
        {
            HandleRightClick();
        }
    }

    private void HandleRightClick()
    {
        if (m_Raycaster == null || m_EventSystem == null)
        {
            // Try to find them again
#if UNITY_2023_1_OR_NEWER
            m_Raycaster = FindFirstObjectByType<GraphicRaycaster>();
#else
            m_Raycaster = FindObjectOfType<GraphicRaycaster>();
#endif
            m_EventSystem = EventSystem.current;

            if (m_Raycaster == null || m_EventSystem == null)
                return;
        }

        // Raycast to find UI elements under the cursor
        m_PointerEventData = new PointerEventData(m_EventSystem);
        m_PointerEventData.position = Input.mousePosition;

        m_RaycastResults.Clear();
        m_Raycaster.Raycast(m_PointerEventData, m_RaycastResults);

        // Find an ItemViewSlot in the raycast results
        ItemViewSlot itemViewSlot = null;
        foreach (var result in m_RaycastResults)
        {
            itemViewSlot = result.gameObject.GetComponent<ItemViewSlot>();
            if (itemViewSlot == null)
            {
                itemViewSlot = result.gameObject.GetComponentInParent<ItemViewSlot>();
            }

            if (itemViewSlot != null)
                break;
        }

        if (itemViewSlot == null || itemViewSlot.ItemInfo.Item == null)
            return;

        // Get the item info from the slot
        var itemInfo = itemViewSlot.ItemInfo;
        var sourceCollection = itemInfo.ItemCollection;

        if (sourceCollection == null)
            return;

        // Use the inventories that were set by SimpleLootableStorage
        if (m_PlayerInventory == null || m_ContainerInventory == null)
        {
            Debug.LogWarning("[StorageQuickTransfer] Inventories not set. Make sure SimpleLootableStorage calls SetInventories.");
            return;
        }

        var playerInventory = m_PlayerInventory;
        var storageInventory = m_ContainerInventory;

        Inventory sourceInventory = null;
        Inventory destInventory = null;

        // Check if the item is in the player's inventory or the storage
        if (IsItemInInventory(itemInfo, playerInventory))
        {
            sourceInventory = playerInventory;
            destInventory = storageInventory;
        }
        else if (IsItemInInventory(itemInfo, storageInventory))
        {
            sourceInventory = storageInventory;
            destInventory = playerInventory;
        }

        if (sourceInventory == null || destInventory == null)
            return;

        // Transfer the entire stack
        TransferItem(itemInfo, sourceInventory, destInventory);
    }

    private bool IsItemInInventory(ItemInfo itemInfo, Inventory inventory)
    {
        if (inventory == null || itemInfo.ItemCollection == null)
            return false;

        // Check if the item's collection belongs to this inventory
        // by comparing the collection's Inventory reference
        return ReferenceEquals(itemInfo.ItemCollection.Inventory, inventory);
    }

    private void TransferItem(ItemInfo itemInfo, Inventory source, Inventory dest)
    {
        // Get the main collection of the destination (usually "Default")
        var destCollection = dest.MainItemCollection;

        // If no main collection, try to get "Default" collection by name
        if (destCollection == null)
        {
            destCollection = dest.GetItemCollection("Default");
        }

        if (destCollection == null)
        {
            Debug.LogWarning("[StorageQuickTransfer] Could not find destination collection.");
            return;
        }

        // Try to add to destination first
        var addResult = destCollection.AddItem(itemInfo);

        if (addResult.Amount > 0)
        {
            // Remove from source what we successfully added
            var removeInfo = new ItemInfo(itemInfo.Item, addResult.Amount);
            itemInfo.ItemCollection.RemoveItem(removeInfo);
        }

        // Explicitly refresh both inventory grids to ensure UI stays in sync
        if (m_StorageMenu != null)
        {
            m_StorageMenu.ClientInventoryGrid?.Draw();
            m_StorageMenu.StorageInventoryGrid?.Draw();
        }
    }
}
