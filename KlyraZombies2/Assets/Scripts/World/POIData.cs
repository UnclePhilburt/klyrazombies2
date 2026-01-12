using UnityEngine;

/// <summary>
/// ScriptableObject defining a Point of Interest location.
/// </summary>
[CreateAssetMenu(fileName = "New POI", menuName = "Game/POI Data")]
public class POIData : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Display name shown when discovered")]
    public string displayName = "Unknown Location";

    [Tooltip("Brief description of the location")]
    [TextArea(2, 4)]
    public string description = "";

    [Header("Discovery")]
    [Tooltip("Unique ID for tracking discovery state")]
    public string poiId = "";

    [Tooltip("Category/type of POI")]
    public POICategory category = POICategory.Landmark;

    [Header("Optional")]
    [Tooltip("Icon to display (optional)")]
    public Sprite icon;

    [Tooltip("Hint about what can be found here")]
    public string lootHint = "";

    private void OnValidate()
    {
        // Auto-generate ID from name if empty
        if (string.IsNullOrEmpty(poiId) && !string.IsNullOrEmpty(displayName))
        {
            poiId = displayName.Replace(" ", "_").ToLower();
        }
    }
}

public enum POICategory
{
    Landmark,
    Building,
    Military,
    Medical,
    Commercial,
    Residential,
    Industrial,
    SafeZone,
    DangerZone
}
