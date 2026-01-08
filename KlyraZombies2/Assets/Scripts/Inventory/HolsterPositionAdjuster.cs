using UnityEngine;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;

/// <summary>
/// Adjusts the rifle holster position based on which backpack is equipped.
/// Supports different positions for small, medium, and large backpacks.
/// </summary>
public class HolsterPositionAdjuster : MonoBehaviour
{
    public enum BackpackSize { None, Small, Medium, Large }

    [Header("References")]
    [SerializeField] private Inventory m_Inventory;
    [SerializeField] private Transform m_RifleHolster;

    [Header("Holster Spots")]
    [SerializeField] private Transform m_DefaultHolsterSpot;
    [SerializeField] private Transform m_SmallBackpackHolsterSpot;
    [SerializeField] private Transform m_MediumBackpackHolsterSpot;
    [SerializeField] private Transform m_LargeBackpackHolsterSpot;

    [Header("Settings")]
    [SerializeField] private string m_EquippableCollectionName = "Equippable";
    [SerializeField] private bool m_DebugLog = true;

    private BackpackSize m_CurrentBackpack = BackpackSize.None;

    private void Start()
    {
        if (m_Inventory == null)
            m_Inventory = GetComponentInParent<Inventory>();

        CheckBackpackState();
    }

    private void LateUpdate()
    {
        CheckBackpackState();
        UpdateHolsterPosition();
    }

    private void CheckBackpackState()
    {
        if (m_Inventory == null) return;

        BackpackSize detectedSize = BackpackSize.None;

        // Try multiple possible collection names
        string[] collectionNames = { m_EquippableCollectionName, "Equipped", "Equipment", "Equippable Slots" };

        foreach (string collectionName in collectionNames)
        {
            var collection = m_Inventory.GetItemCollection(collectionName);
            if (collection == null) continue;

            var items = collection.GetAllItemStacks();
            if (items == null) continue;

            foreach (var itemStack in items)
            {
                if (itemStack?.Item?.Category == null) continue;

                string categoryName = itemStack.Item.Category.name;

                if (m_DebugLog && m_CurrentBackpack == BackpackSize.None)
                    Debug.Log($"[HolsterAdjuster] Found in {collectionName}: {itemStack.Item.name} ({categoryName})");

                if (!categoryName.Contains("Backpack") && !categoryName.Contains("Bag"))
                    continue;

                // Check item name for size
                string itemName = itemStack.Item.name.ToLower();
                if (itemName.Contains("small"))
                    detectedSize = BackpackSize.Small;
                else if (itemName.Contains("large"))
                    detectedSize = BackpackSize.Large;
                else
                    detectedSize = BackpackSize.Medium;

                break;
            }

            if (detectedSize != BackpackSize.None) break;
        }

        if (detectedSize != m_CurrentBackpack)
        {
            m_CurrentBackpack = detectedSize;
            if (m_DebugLog) Debug.Log($"[HolsterAdjuster] Backpack changed to: {m_CurrentBackpack}");
        }
    }

    private void UpdateHolsterPosition()
    {
        if (m_RifleHolster == null)
        {
            if (m_DebugLog) Debug.LogWarning("[HolsterAdjuster] Rifle Holster is NULL!");
            return;
        }

        Transform targetSpot = m_CurrentBackpack switch
        {
            BackpackSize.Small => m_SmallBackpackHolsterSpot,
            BackpackSize.Medium => m_MediumBackpackHolsterSpot,
            BackpackSize.Large => m_LargeBackpackHolsterSpot,
            _ => m_DefaultHolsterSpot
        };

        if (targetSpot == null)
        {
            if (m_DebugLog) Debug.LogWarning($"[HolsterAdjuster] Target spot is NULL for size: {m_CurrentBackpack}");
            return;
        }

        // Copy LOCAL position and rotation from target spot (so it follows character)
        m_RifleHolster.localPosition = targetSpot.localPosition;
        m_RifleHolster.localRotation = targetSpot.localRotation;

        if (m_DebugLog && m_CurrentBackpack != BackpackSize.None)
        {
            Debug.Log($"[HolsterAdjuster] Moved {m_RifleHolster.name} to {targetSpot.name} localPos:{targetSpot.localPosition}");
        }
    }
}
