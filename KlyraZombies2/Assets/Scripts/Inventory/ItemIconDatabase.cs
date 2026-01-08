using UnityEngine;
using System.Collections.Generic;
using Opsive.UltimateInventorySystem.Core;

/// <summary>
/// Simple icon lookup database that maps ItemDefinitions to Sprites.
/// Use this as a workaround when UIS Icon attributes aren't working.
/// </summary>
[CreateAssetMenu(fileName = "ItemIconDatabase", menuName = "Game/Item Icon Database")]
public class ItemIconDatabase : ScriptableObject
{
    private static ItemIconDatabase s_Instance;
    public static ItemIconDatabase Instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = Resources.Load<ItemIconDatabase>("ItemIconDatabase");
            }
            return s_Instance;
        }
    }

    [System.Serializable]
    public class IconEntry
    {
        public ItemDefinition itemDefinition;
        public Sprite icon;
    }

    [SerializeField] private List<IconEntry> m_Icons = new List<IconEntry>();

    private Dictionary<ItemDefinition, Sprite> m_IconLookup;

    /// <summary>
    /// Get the icon for an item definition.
    /// </summary>
    public Sprite GetIcon(ItemDefinition itemDef)
    {
        if (itemDef == null) return null;

        // Build lookup on first use
        if (m_IconLookup == null)
        {
            m_IconLookup = new Dictionary<ItemDefinition, Sprite>();
            foreach (var entry in m_Icons)
            {
                if (entry.itemDefinition != null && entry.icon != null)
                {
                    m_IconLookup[entry.itemDefinition] = entry.icon;
                }
            }
        }

        m_IconLookup.TryGetValue(itemDef, out Sprite icon);
        return icon;
    }

    /// <summary>
    /// Get the icon for an item.
    /// </summary>
    public Sprite GetIcon(Opsive.UltimateInventorySystem.Core.Item item)
    {
        if (item == null) return null;
        return GetIcon(item.ItemDefinition);
    }

    // Editor helper to add entries
    public void AddEntry(ItemDefinition itemDef, Sprite icon)
    {
        // Check if already exists
        for (int i = 0; i < m_Icons.Count; i++)
        {
            if (m_Icons[i].itemDefinition == itemDef)
            {
                m_Icons[i].icon = icon;
                return;
            }
        }

        m_Icons.Add(new IconEntry { itemDefinition = itemDef, icon = icon });
    }

    public List<IconEntry> GetAllEntries() => m_Icons;

    // Clear the cached lookup (call after modifying entries)
    public void ClearCache()
    {
        m_IconLookup = null;
    }
}
