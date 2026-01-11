using UnityEngine;

/// <summary>
/// Defines the equipment slots available for character customization.
/// </summary>
public enum EquipmentSlotType
{
    // Head slots
    Head,           // Helmets, hats, hoods
    Face,           // Masks, goggles, glasses
    Hair,           // Hair styles (Sidekick)

    // Body slots
    Torso,          // Shirts, jackets, armor vests
    Hands,          // Gloves
    Legs,           // Pants, shorts
    Feet,           // Shoes, boots

    // Accessory slots
    Back,           // Backpacks (already implemented)
    ShoulderLeft,   // Shoulder armor/pads
    ShoulderRight,
    KneeLeft,       // Knee pads
    KneeRight,
    Belt,           // Belt pouches, holsters
}

/// <summary>
/// Data class representing an equipment slot on a character.
/// </summary>
[System.Serializable]
public class EquipmentSlot
{
    public EquipmentSlotType slotType;
    public Transform attachPoint;
    public GameObject currentVisual;

    [Tooltip("For Sidekick integration - the CharacterPartType this slot maps to")]
    public string sidekickPartType;
}
