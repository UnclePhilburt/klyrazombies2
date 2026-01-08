using UnityEngine;
using UnityEngine.UI;
using Opsive.UltimateInventorySystem.UI.Item;
using Opsive.UltimateInventorySystem.UI.Item.ItemViewModules;
using Opsive.UltimateInventorySystem.UI.Panels;
using Opsive.UltimateInventorySystem.Core.DataStructures;

/// <summary>
/// Add this component to your Canvas (same object as DisplayPanelManager).
/// It automatically patches item icons using ItemIconDatabase whenever panels refresh.
/// </summary>
public class ItemIconOverride : MonoBehaviour
{
    [Tooltip("How often to refresh icons (in seconds). Set to 0 to only refresh on panel open.")]
    [SerializeField] private float m_RefreshInterval = 0.5f;

    private DisplayPanelManager m_PanelManager;
    private float m_LastRefreshTime;

    private void Start()
    {
        m_PanelManager = GetComponent<DisplayPanelManager>();
        if (m_PanelManager == null)
        {
            m_PanelManager = GetComponentInChildren<DisplayPanelManager>();
        }

        if (ItemIconDatabase.Instance == null)
        {
            Debug.LogWarning("[ItemIconOverride] ItemIconDatabase not found in Resources folder!");
        }
        else
        {
            Debug.Log($"[ItemIconOverride] Loaded ItemIconDatabase with icons");
        }
    }

    private void LateUpdate()
    {
        // Only refresh if interval is set
        if (m_RefreshInterval <= 0) return;

        if (Time.time - m_LastRefreshTime > m_RefreshInterval)
        {
            m_LastRefreshTime = Time.time;
            RefreshAllIcons();
        }
    }

    /// <summary>
    /// Refresh all visible item icons.
    /// </summary>
    public void RefreshAllIcons()
    {
        var database = ItemIconDatabase.Instance;
        if (database == null) return;

        // Find all active ItemViews
        var itemViews = FindObjectsByType<ItemView>(FindObjectsSortMode.None);
        foreach (var itemView in itemViews)
        {
            if (!itemView.gameObject.activeInHierarchy) continue;
            UpdateItemViewIcon(itemView, database);
        }
    }

    private void UpdateItemViewIcon(ItemView itemView, ItemIconDatabase database)
    {
        if (itemView == null) return;

        var itemInfo = itemView.CurrentValue;
        if (itemInfo.Item == null || itemInfo.Item.ItemDefinition == null) return;

        Sprite icon = database.GetIcon(itemInfo.Item.ItemDefinition);
        if (icon == null) return;

        // Find Image children named "Icon" or containing "icon"
        var images = itemView.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            string imgName = img.name.ToLower();
            if (imgName.Contains("icon") || imgName == "image")
            {
                img.sprite = icon;
                img.enabled = true;
                break; // Only set the first matching image
            }
        }
    }
}
