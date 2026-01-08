using UnityEngine;
using UnityEngine.UI;
using Opsive.UltimateInventorySystem.UI.Item.ItemViewModules;
using Opsive.UltimateInventorySystem.UI.Item;
using Opsive.UltimateInventorySystem.Core.DataStructures;

/// <summary>
/// ItemViewModule that displays icons from the ItemIconDatabase.
/// Add this to your ItemView prefabs to use the icon database instead of UIS Icon attributes.
/// </summary>
public class IconDatabaseItemView : ItemViewModule
{
    [Tooltip("The Image component to display the icon")]
    [SerializeField] private Image m_Icon;

    [Tooltip("Hide the icon if no sprite is found")]
    [SerializeField] private bool m_HideIfNull = true;

    public Image Icon => m_Icon;

    public override void SetValue(ItemInfo info)
    {
        if (info.Item == null || info.Item.IsInitialized == false)
        {
            Clear();
            return;
        }

        Sprite icon = null;

        // First try our custom database
        if (ItemIconDatabase.Instance != null)
        {
            icon = ItemIconDatabase.Instance.GetIcon(info.Item.ItemDefinition);
        }

        // Fallback: try to get from UIS Icon attribute
        if (icon == null)
        {
            var iconAttribute = info.Item.GetAttribute<Opsive.UltimateInventorySystem.Core.AttributeSystem.Attribute<Sprite>>("Icon");
            if (iconAttribute != null)
            {
                icon = iconAttribute.GetValue();
            }
        }

        if (m_Icon != null)
        {
            m_Icon.sprite = icon;

            if (m_HideIfNull)
            {
                m_Icon.enabled = icon != null;
            }
        }
    }

    public override void Clear()
    {
        if (m_Icon != null)
        {
            m_Icon.sprite = null;
            if (m_HideIfNull)
            {
                m_Icon.enabled = false;
            }
        }
    }
}
