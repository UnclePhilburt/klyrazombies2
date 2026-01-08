using UnityEngine;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;

/// <summary>
/// Handles spawning and attaching backpack visuals to the character.
/// Each backpack size can have its own attachment point.
/// </summary>
public class BackpackAttachmentHandler : MonoBehaviour
{
    public enum BackpackSize { None, Small, Medium, Large }

    [Header("References")]
    [SerializeField] private Inventory m_Inventory;

    [Header("Attachment Points")]
    [SerializeField] private Transform m_SmallBackpackAttachPoint;
    [SerializeField] private Transform m_MediumBackpackAttachPoint;
    [SerializeField] private Transform m_LargeBackpackAttachPoint;

    [Header("Backpack Prefabs (optional - can use item's Prefabs attribute)")]
    [SerializeField] private GameObject m_SmallBackpackPrefab;
    [SerializeField] private GameObject m_MediumBackpackPrefab;
    [SerializeField] private GameObject m_LargeBackpackPrefab;

    [Header("Settings")]
    [SerializeField] private string m_EquippableCollectionName = "Equippable";
    [SerializeField] private string m_PrefabAttributeName = "Prefabs";
    [SerializeField] private bool m_DebugLog = true;

    private GameObject m_SpawnedBackpack;
    private BackpackSize m_CurrentSize = BackpackSize.None;
    private string m_CurrentItemName = "";

    private void Start()
    {
        if (m_Inventory == null)
            m_Inventory = GetComponentInParent<Inventory>();

        CheckBackpackState();
    }

    private void Update()
    {
        CheckBackpackState();
    }

    private void CheckBackpackState()
    {
        if (m_Inventory == null) return;

        BackpackSize detectedSize = BackpackSize.None;
        GameObject prefabToUse = null;
        string itemName = "";

        var equipCollection = m_Inventory.GetItemCollection(m_EquippableCollectionName);
        if (equipCollection == null) return;

        var items = equipCollection.GetAllItemStacks();
        if (items != null)
        {
            foreach (var itemStack in items)
            {
                if (itemStack?.Item?.Category == null) continue;

                string categoryName = itemStack.Item.Category.name;
                if (!categoryName.Contains("Backpack") && !categoryName.Contains("Bag"))
                    continue;

                itemName = itemStack.Item.name;
                string lowerName = itemName.ToLower();

                // Determine size
                if (lowerName.Contains("small"))
                    detectedSize = BackpackSize.Small;
                else if (lowerName.Contains("large"))
                    detectedSize = BackpackSize.Large;
                else
                    detectedSize = BackpackSize.Medium;

                // Try to get prefab from item attribute first
                if (itemStack.Item.TryGetAttributeValue<GameObject>(m_PrefabAttributeName, out var itemPrefab))
                {
                    prefabToUse = itemPrefab;
                }

                break;
            }
        }

        // Check if state changed
        if (detectedSize != m_CurrentSize || itemName != m_CurrentItemName)
        {
            m_CurrentSize = detectedSize;
            m_CurrentItemName = itemName;

            // Destroy old backpack
            if (m_SpawnedBackpack != null)
            {
                Destroy(m_SpawnedBackpack);
                m_SpawnedBackpack = null;
            }

            // Spawn new backpack if equipped
            if (detectedSize != BackpackSize.None)
            {
                SpawnBackpack(detectedSize, prefabToUse);
            }

            if (m_DebugLog) Debug.Log($"[BackpackAttachment] Backpack: {detectedSize} ({itemName})");
        }
    }

    private void SpawnBackpack(BackpackSize size, GameObject itemPrefab)
    {
        // Get attachment point based on size
        Transform attachPoint = size switch
        {
            BackpackSize.Small => m_SmallBackpackAttachPoint,
            BackpackSize.Medium => m_MediumBackpackAttachPoint,
            BackpackSize.Large => m_LargeBackpackAttachPoint,
            _ => null
        };

        if (attachPoint == null)
        {
            if (m_DebugLog) Debug.LogWarning($"[BackpackAttachment] No attach point for size: {size}");
            return;
        }

        // Get prefab - use item's prefab if available, otherwise fallback to serialized prefabs
        GameObject prefab = itemPrefab;
        if (prefab == null)
        {
            prefab = size switch
            {
                BackpackSize.Small => m_SmallBackpackPrefab,
                BackpackSize.Medium => m_MediumBackpackPrefab,
                BackpackSize.Large => m_LargeBackpackPrefab,
                _ => null
            };
        }

        if (prefab == null)
        {
            if (m_DebugLog) Debug.LogWarning($"[BackpackAttachment] No prefab for size: {size}");
            return;
        }

        // Spawn and attach
        m_SpawnedBackpack = Instantiate(prefab, attachPoint);
        m_SpawnedBackpack.transform.localPosition = Vector3.zero;
        m_SpawnedBackpack.transform.localRotation = Quaternion.identity;
        m_SpawnedBackpack.transform.localScale = Vector3.one;

        if (m_DebugLog) Debug.Log($"[BackpackAttachment] Spawned {prefab.name} at {attachPoint.name}");
    }

    private void OnDestroy()
    {
        if (m_SpawnedBackpack != null)
            Destroy(m_SpawnedBackpack);
    }
}
