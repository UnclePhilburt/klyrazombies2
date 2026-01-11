using UnityEngine;
using UnityEngine.UI;
using Opsive.UltimateInventorySystem.UI.Item.ItemViewModules;
using Opsive.UltimateInventorySystem.UI.Item;
using Opsive.UltimateInventorySystem.Core.DataStructures;
using System.Collections.Generic;

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

    [Tooltip("Apply color tints to clothing items")]
    [SerializeField] private bool m_ApplyClothingTints = true;

    public Image Icon => m_Icon;

    // Color tints for clothing items to make them visually distinct
    private static readonly Dictionary<string, Color> CLOTHING_TINTS = new Dictionary<string, Color>
    {
        // Survivor Shirts - Earth tones
        { "Survivor Jacket", new Color(0.6f, 0.5f, 0.4f) },      // Brown
        { "Survivor Hoodie", new Color(0.5f, 0.5f, 0.6f) },      // Gray-blue
        { "Survivor Vest", new Color(0.5f, 0.6f, 0.4f) },        // Olive
        { "Survivor T-Shirt", new Color(0.9f, 0.9f, 0.85f) },    // Off-white
        { "Survivor Flannel", new Color(0.7f, 0.4f, 0.4f) },     // Red plaid

        // Survivor Pants - Neutral tones
        { "Survivor Cargo Pants", new Color(0.5f, 0.5f, 0.4f) }, // Khaki
        { "Survivor Jeans", new Color(0.4f, 0.5f, 0.7f) },       // Denim blue
        { "Survivor Work Pants", new Color(0.45f, 0.4f, 0.35f) },// Dark brown
        { "Survivor Shorts", new Color(0.6f, 0.55f, 0.5f) },     // Tan
        { "Survivor Khakis", new Color(0.75f, 0.7f, 0.55f) },    // Light khaki

        // Outlaw Shirts - Darker, edgier colors
        { "Raider Jacket", new Color(0.3f, 0.3f, 0.3f) },        // Dark gray
        { "Scavenger Vest", new Color(0.5f, 0.4f, 0.3f) },       // Dusty brown
        { "Bandit Hoodie", new Color(0.2f, 0.2f, 0.25f) },       // Near black
        { "Wasteland Coat", new Color(0.55f, 0.45f, 0.35f) },    // Desert tan
        { "Road Warrior Top", new Color(0.4f, 0.35f, 0.3f) },    // Mud brown
        { "Marauder Jacket", new Color(0.5f, 0.3f, 0.3f) },      // Dark red
        { "Punk Vest", new Color(0.25f, 0.25f, 0.3f) },          // Charcoal
        { "Biker Jacket", new Color(0.15f, 0.15f, 0.15f) },      // Black leather
        { "Nomad Shirt", new Color(0.6f, 0.5f, 0.35f) },         // Sand
        { "Drifter Coat", new Color(0.4f, 0.4f, 0.35f) },        // Worn gray

        // Outlaw Pants - Matching darker tones
        { "Raider Pants", new Color(0.35f, 0.35f, 0.3f) },       // Dark olive
        { "Scavenger Cargos", new Color(0.45f, 0.4f, 0.35f) },   // Faded brown
        { "Bandit Jeans", new Color(0.25f, 0.25f, 0.3f) },       // Dark denim
        { "Wasteland Trousers", new Color(0.6f, 0.55f, 0.45f) }, // Dusty tan
        { "Road Warrior Pants", new Color(0.35f, 0.3f, 0.25f) }, // Dark mud
        { "Marauder Cargos", new Color(0.4f, 0.3f, 0.25f) },     // Rust brown
        { "Punk Pants", new Color(0.2f, 0.2f, 0.2f) },           // Black
        { "Biker Jeans", new Color(0.3f, 0.3f, 0.35f) },         // Dark gray denim
        { "Nomad Pants", new Color(0.55f, 0.5f, 0.4f) },         // Light brown
        { "Drifter Trousers", new Color(0.45f, 0.45f, 0.4f) },   // Worn khaki

        // === CIVILIAN CLOTHING (Modern Civilians 01-12) ===
        // Shirts - Professional/clean colors
        { "Office Shirt", new Color(0.9f, 0.9f, 0.95f) },         // White dress shirt
        { "Casual Polo", new Color(0.4f, 0.5f, 0.7f) },           // Navy blue polo
        { "Business Suit", new Color(0.25f, 0.25f, 0.3f) },       // Dark charcoal suit
        { "Sweater", new Color(0.6f, 0.5f, 0.4f) },               // Brown sweater
        { "Lab Coat", new Color(0.95f, 0.95f, 0.95f) },           // White lab coat
        { "Security Uniform", new Color(0.3f, 0.35f, 0.45f) },    // Navy security
        { "Chef Jacket", new Color(0.95f, 0.95f, 0.9f) },         // White chef coat
        { "Mechanic Overalls", new Color(0.4f, 0.45f, 0.55f) },   // Blue overalls
        { "Nurse Scrubs", new Color(0.5f, 0.7f, 0.7f) },          // Teal scrubs
        { "Janitor Uniform", new Color(0.5f, 0.55f, 0.5f) },      // Gray-green
        { "Delivery Shirt", new Color(0.6f, 0.5f, 0.35f) },       // Brown delivery
        { "Barista Apron", new Color(0.35f, 0.25f, 0.2f) },       // Coffee brown

        // Pants - Matching professional tones
        { "Office Pants", new Color(0.3f, 0.3f, 0.35f) },         // Dark gray slacks
        { "Casual Khakis", new Color(0.7f, 0.65f, 0.5f) },        // Khaki
        { "Business Slacks", new Color(0.25f, 0.25f, 0.3f) },     // Charcoal
        { "Joggers", new Color(0.4f, 0.4f, 0.45f) },              // Gray joggers
        { "Lab Pants", new Color(0.9f, 0.9f, 0.9f) },             // White
        { "Security Pants", new Color(0.25f, 0.3f, 0.4f) },       // Navy
        { "Chef Pants", new Color(0.2f, 0.2f, 0.2f) },            // Black chef pants
        { "Work Jeans", new Color(0.35f, 0.4f, 0.55f) },          // Denim blue
        { "Scrub Pants", new Color(0.45f, 0.6f, 0.6f) },          // Teal
        { "Utility Pants", new Color(0.5f, 0.5f, 0.45f) },        // Olive utility
        { "Cargo Shorts", new Color(0.6f, 0.55f, 0.45f) },        // Tan cargo
    };

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

            // Apply color tint for clothing items
            if (m_ApplyClothingTints && info.Item.ItemDefinition != null)
            {
                string itemName = info.Item.ItemDefinition.name;
                if (CLOTHING_TINTS.TryGetValue(itemName, out Color tint))
                {
                    m_Icon.color = tint;
                }
                else
                {
                    m_Icon.color = Color.white; // Reset to default
                }
            }
            else
            {
                m_Icon.color = Color.white;
            }

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
            m_Icon.color = Color.white;
            if (m_HideIfNull)
            {
                m_Icon.enabled = false;
            }
        }
    }
}
