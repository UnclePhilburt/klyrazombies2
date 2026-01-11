using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Opsive.UltimateInventorySystem.Core;
using Opsive.UltimateInventorySystem.Core.DataStructures;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;
using Synty.SidekickCharacters.Enums;

/// <summary>
/// Handles equipping/unequipping Sidekick clothing parts based on UIS inventory changes.
/// When a player equips a clothing item, this updates their Sidekick character appearance.
/// </summary>
public class SidekickClothingEquipHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SidekickPlayerController m_CharacterController;
    [SerializeField] private Inventory m_Inventory;

    [Header("Settings")]
    [SerializeField] private string m_EquippedCollectionName = "Equippable";

    [Header("Category Names")]
    [Tooltip("Category name for shirt/upper body clothing")]
    [SerializeField] private string m_ShirtCategoryName = "Shirt";
    [Tooltip("Category name for pants/lower body clothing")]
    [SerializeField] private string m_PantsCategoryName = "Pants";
    [Tooltip("Apply both upper and lower body when equipping a shirt (full outfit)")]
    [SerializeField] private bool m_ApplyFullOutfit = false;

    [Header("Attribute Names")]
    [Tooltip("Item attribute containing the Sidekick preset name (optional, falls back to item name)")]
    [SerializeField] private string m_PresetNameAttribute = "SidekickPresetName";

    [Header("Debug")]
    [SerializeField] private bool m_DebugLog = true;

    private ItemCollection m_EquippedCollection;

    // Track what's currently equipped by part group
    private Dictionary<PartGroup, ItemDefinition> m_EquippedByGroup = new Dictionary<PartGroup, ItemDefinition>();

    // Store the original "base body" appearance to revert to when unequipping
    private CharacterSaveData m_OriginalAppearance;

    // Lookup table mapping display names to Sidekick preset names
    // Available packs: Apocalypse Survivor (01-05), Apocalypse Outlaws (01-10), Modern Civilians (01-12)
    private static readonly Dictionary<string, string> PRESET_LOOKUP = new Dictionary<string, string>
    {
        // === SURVIVOR CLOTHING (Apocalypse Survivor 01-05) ===
        // Shirts
        { "Survivor Jacket", "Apocalypse Survivor 01" },
        { "Survivor Hoodie", "Apocalypse Survivor 02" },
        { "Survivor Vest", "Apocalypse Survivor 03" },
        { "Survivor T-Shirt", "Apocalypse Survivor 04" },
        { "Survivor Flannel", "Apocalypse Survivor 05" },
        // Pants
        { "Survivor Cargo Pants", "Apocalypse Survivor 01" },
        { "Survivor Jeans", "Apocalypse Survivor 02" },
        { "Survivor Work Pants", "Apocalypse Survivor 03" },
        { "Survivor Shorts", "Apocalypse Survivor 04" },
        { "Survivor Khakis", "Apocalypse Survivor 05" },

        // === OUTLAW CLOTHING (Apocalypse Outlaws 01-10) ===
        // Shirts
        { "Raider Jacket", "Apocalypse Outlaws 01" },
        { "Scavenger Vest", "Apocalypse Outlaws 02" },
        { "Bandit Hoodie", "Apocalypse Outlaws 03" },
        { "Wasteland Coat", "Apocalypse Outlaws 04" },
        { "Road Warrior Top", "Apocalypse Outlaws 05" },
        { "Marauder Jacket", "Apocalypse Outlaws 06" },
        { "Punk Vest", "Apocalypse Outlaws 07" },
        { "Biker Jacket", "Apocalypse Outlaws 08" },
        { "Nomad Shirt", "Apocalypse Outlaws 09" },
        { "Drifter Coat", "Apocalypse Outlaws 10" },
        // Pants
        { "Raider Pants", "Apocalypse Outlaws 01" },
        { "Scavenger Cargos", "Apocalypse Outlaws 02" },
        { "Bandit Jeans", "Apocalypse Outlaws 03" },
        { "Wasteland Trousers", "Apocalypse Outlaws 04" },
        { "Road Warrior Pants", "Apocalypse Outlaws 05" },
        { "Marauder Cargos", "Apocalypse Outlaws 06" },
        { "Punk Pants", "Apocalypse Outlaws 07" },
        { "Biker Jeans", "Apocalypse Outlaws 08" },
        { "Nomad Pants", "Apocalypse Outlaws 09" },
        { "Drifter Trousers", "Apocalypse Outlaws 10" },

        // === CIVILIAN CLOTHING (Modern Civilians 01-12) ===
        // Shirts
        { "Office Shirt", "Modern Civilians 01" },
        { "Casual Polo", "Modern Civilians 02" },
        { "Business Suit", "Modern Civilians 03" },
        { "Sweater", "Modern Civilians 04" },
        { "Lab Coat", "Modern Civilians 05" },
        { "Security Uniform", "Modern Civilians 06" },
        { "Chef Jacket", "Modern Civilians 07" },
        { "Mechanic Overalls", "Modern Civilians 08" },
        { "Nurse Scrubs", "Modern Civilians 09" },
        { "Janitor Uniform", "Modern Civilians 10" },
        { "Delivery Shirt", "Modern Civilians 11" },
        { "Barista Apron", "Modern Civilians 12" },
        // Pants
        { "Office Pants", "Modern Civilians 01" },
        { "Casual Khakis", "Modern Civilians 02" },
        { "Business Slacks", "Modern Civilians 03" },
        { "Joggers", "Modern Civilians 04" },
        { "Lab Pants", "Modern Civilians 05" },
        { "Security Pants", "Modern Civilians 06" },
        { "Chef Pants", "Modern Civilians 07" },
        { "Work Jeans", "Modern Civilians 08" },
        { "Scrub Pants", "Modern Civilians 09" },
        { "Utility Pants", "Modern Civilians 10" },
        { "Cargo Shorts", "Modern Civilians 11" },
    };

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        Debug.Log("[SidekickClothingEquipHandler] Initialize() called");

        // Get inventory if not assigned
        if (m_Inventory == null)
        {
            m_Inventory = GetComponent<Inventory>();
        }

        if (m_Inventory == null)
        {
            Debug.LogError("[SidekickClothingEquipHandler] No Inventory component found!");
            return;
        }

        Debug.Log($"[SidekickClothingEquipHandler] Found inventory: {m_Inventory.name}");

        // Get character controller if not assigned
        if (m_CharacterController == null)
        {
            m_CharacterController = GetComponent<SidekickPlayerController>();
            if (m_CharacterController == null)
            {
                m_CharacterController = GetComponentInChildren<SidekickPlayerController>();
            }
        }

        if (m_CharacterController == null)
        {
            Debug.LogError("[SidekickClothingEquipHandler] No SidekickPlayerController found!");
            return;
        }

        Debug.Log($"[SidekickClothingEquipHandler] Found SidekickPlayerController: {m_CharacterController.name}");

        // Find the equipped collection
        m_EquippedCollection = m_Inventory.GetItemCollection(m_EquippedCollectionName);
        if (m_EquippedCollection == null)
        {
            Debug.LogWarning($"[SidekickClothingEquipHandler] Could not find collection '{m_EquippedCollectionName}'");
            return;
        }

        Debug.Log($"[SidekickClothingEquipHandler] Subscribed to collection: {m_EquippedCollection.Name}");

        // Save the original "base body" appearance BEFORE any clothing is applied
        // This is what we'll revert to when unequipping
        m_OriginalAppearance = m_CharacterController.GetSaveData();
        if (m_OriginalAppearance != null)
        {
            Debug.Log($"[SidekickClothingEquipHandler] Saved original appearance - Upper: '{m_OriginalAppearance.upperBodyPresetName}', Lower: '{m_OriginalAppearance.lowerBodyPresetName}'");
        }

        // Subscribe to inventory events
        m_EquippedCollection.OnItemAdded += OnItemEquipped;
        m_EquippedCollection.OnItemRemoved += OnItemUnequipped;

        // Check for already equipped clothing items
        RefreshEquippedClothing();

        if (m_DebugLog)
        {
            Debug.Log("[SidekickClothingEquipHandler] Initialized");
        }
    }

    private void OnDestroy()
    {
        if (m_EquippedCollection != null)
        {
            m_EquippedCollection.OnItemAdded -= OnItemEquipped;
            m_EquippedCollection.OnItemRemoved -= OnItemUnequipped;
        }
    }

    private void OnItemEquipped(ItemInfo originalItemInfo, ItemStack addedItemStack)
    {
        if (addedItemStack?.Item?.ItemDefinition == null) return;

        var itemDef = addedItemStack.Item.ItemDefinition;

        Debug.Log($"[SidekickClothingEquipHandler] OnItemEquipped triggered for: {itemDef.name}, Category: {itemDef.Category?.name ?? "NULL"}");

        // Check if this is a Sidekick clothing item
        if (!TryGetSidekickInfo(itemDef, out PartGroup partGroup, out string presetName))
        {
            Debug.Log($"[SidekickClothingEquipHandler] TryGetSidekickInfo returned false for {itemDef.name}");
            return;
        }

        if (m_DebugLog)
        {
            Debug.Log($"[SidekickClothingEquipHandler] Equipping {itemDef.name} -> {partGroup}: {presetName}");
        }

        // Update character appearance
        ApplyClothingPart(partGroup, presetName);
        m_EquippedByGroup[partGroup] = itemDef;
    }

    private void OnItemUnequipped(ItemInfo originalItemInfo)
    {
        Debug.Log($"[SidekickClothingEquipHandler] OnItemUnequipped called");

        if (originalItemInfo.Item?.ItemDefinition == null)
        {
            Debug.Log($"[SidekickClothingEquipHandler] Item or ItemDefinition is null");
            return;
        }

        var itemDef = originalItemInfo.Item.ItemDefinition;
        Debug.Log($"[SidekickClothingEquipHandler] Unequipping item: {itemDef.name}");

        // Check if this is a Sidekick clothing item
        if (!TryGetSidekickInfo(itemDef, out PartGroup partGroup, out string presetName))
        {
            Debug.Log($"[SidekickClothingEquipHandler] TryGetSidekickInfo returned false for unequip");
            return;
        }

        Debug.Log($"[SidekickClothingEquipHandler] Unequipping {itemDef.name} from {partGroup}");

        // Revert to default appearance for this part group
        RevertToDefault(partGroup);
        m_EquippedByGroup.Remove(partGroup);
    }

    private bool TryGetSidekickInfo(ItemDefinition itemDef, out PartGroup partGroup, out string presetName)
    {
        partGroup = PartGroup.Head;
        presetName = null;

        // Check item category to determine part group
        var category = itemDef.Category;
        if (category == null)
        {
            return false;
        }

        string categoryName = category.name;

        // Check if it's a shirt (upper body) or pants (lower body)
        if (categoryName == m_ShirtCategoryName || categoryName.Contains("Shirt") || categoryName.Contains("Torso"))
        {
            partGroup = PartGroup.UpperBody;
        }
        else if (categoryName == m_PantsCategoryName || categoryName.Contains("Pants") || categoryName.Contains("Legs"))
        {
            partGroup = PartGroup.LowerBody;
        }
        else if (categoryName.Contains("Head") || categoryName.Contains("Hat") || categoryName.Contains("Helmet"))
        {
            partGroup = PartGroup.Head;
        }
        else
        {
            // Not a recognized clothing category
            return false;
        }

        // Try lookup table first (for unique display names)
        if (PRESET_LOOKUP.TryGetValue(itemDef.name, out var lookupPreset))
        {
            presetName = lookupPreset;
            Debug.Log($"[SidekickClothingEquipHandler] Using lookup preset: '{presetName}' for item '{itemDef.name}'");
        }
        // Then try attribute
        else if (itemDef.TryGetAttributeValue<string>(m_PresetNameAttribute, out var attrPresetName) && !string.IsNullOrEmpty(attrPresetName))
        {
            presetName = attrPresetName;
            Debug.Log($"[SidekickClothingEquipHandler] Using attribute preset name: '{presetName}'");
        }
        else
        {
            // Fall back to item name (remove " Shirt" or " Pants" suffix if present)
            presetName = itemDef.name;
            if (presetName.EndsWith(" Shirt"))
            {
                presetName = presetName.Substring(0, presetName.Length - 6);
            }
            else if (presetName.EndsWith(" Pants"))
            {
                presetName = presetName.Substring(0, presetName.Length - 6);
            }
            Debug.Log($"[SidekickClothingEquipHandler] Using item name as preset: '{presetName}'");
        }

        return true;
    }

    private void ApplyClothingPart(PartGroup partGroup, string presetName)
    {
        if (m_CharacterController == null)
        {
            Debug.LogError("[SidekickClothingEquipHandler] ApplyClothingPart: m_CharacterController is NULL!");
            return;
        }

        Debug.Log($"[SidekickClothingEquipHandler] ApplyClothingPart: {partGroup} -> '{presetName}'");

        // Check if controller is initialized
        var presets = m_CharacterController.GetPresetsForGroup(partGroup);
        Debug.Log($"[SidekickClothingEquipHandler] Available presets for {partGroup}: {presets?.Count ?? 0}");
        if (presets != null && presets.Count > 0)
        {
            // Log first few to verify names
            for (int i = 0; i < Mathf.Min(3, presets.Count); i++)
            {
                Debug.Log($"[SidekickClothingEquipHandler]   [{i}] '{presets[i].Name}'");
            }
            // Check if our target exists
            var match = presets.FirstOrDefault(p => p.Name == presetName);
            Debug.Log($"[SidekickClothingEquipHandler] Preset match for '{presetName}': {(match != null ? "FOUND" : "NOT FOUND")}");
        }

        switch (partGroup)
        {
            case PartGroup.Head:
                m_CharacterController.SetHeadPresetByName(presetName);
                break;
            case PartGroup.UpperBody:
                m_CharacterController.SetUpperBodyPresetByName(presetName);
                // If full outfit mode, also apply lower body with same preset
                if (m_ApplyFullOutfit)
                {
                    m_CharacterController.SetLowerBodyPresetByName(presetName);
                    if (m_DebugLog)
                    {
                        Debug.Log($"[SidekickClothingEquipHandler] Full outfit: also applied {presetName} to LowerBody");
                    }
                }
                break;
            case PartGroup.LowerBody:
                m_CharacterController.SetLowerBodyPresetByName(presetName);
                break;
        }
    }

    private void RevertToDefault(PartGroup partGroup)
    {
        if (m_CharacterController == null)
        {
            Debug.LogWarning("[SidekickClothingEquipHandler] RevertToDefault: CharacterController is null!");
            return;
        }

        // Use the original appearance we saved during initialization
        // NOT GetSaveData() which returns current (possibly clothed) state
        var saveData = m_OriginalAppearance;
        Debug.Log($"[SidekickClothingEquipHandler] RevertToDefault: using original appearance = {(saveData != null ? "exists" : "NULL")}");
        if (saveData != null)
        {
            Debug.Log($"[SidekickClothingEquipHandler] Original presets - Upper: '{saveData.upperBodyPresetName}', Lower: '{saveData.lowerBodyPresetName}'");
        }

        switch (partGroup)
        {
            case PartGroup.Head:
                // Revert to original saved preset
                m_CharacterController.SetHeadPresetByName(saveData?.headPresetName);
                break;
            case PartGroup.UpperBody:
                Debug.Log($"[SidekickClothingEquipHandler] Reverting UpperBody to: '{saveData?.upperBodyPresetName}'");
                m_CharacterController.SetUpperBodyPresetByName(saveData?.upperBodyPresetName);
                // If full outfit mode, also revert lower body
                if (m_ApplyFullOutfit)
                {
                    m_CharacterController.SetLowerBodyPresetByName(saveData?.lowerBodyPresetName);
                    Debug.Log($"[SidekickClothingEquipHandler] Full outfit: also reverted LowerBody to '{saveData?.lowerBodyPresetName}'");
                }
                break;
            case PartGroup.LowerBody:
                Debug.Log($"[SidekickClothingEquipHandler] Reverting LowerBody to: '{saveData?.lowerBodyPresetName}'");
                m_CharacterController.SetLowerBodyPresetByName(saveData?.lowerBodyPresetName);
                break;
        }
    }

    private void RefreshEquippedClothing()
    {
        if (m_EquippedCollection == null) return;

        m_EquippedByGroup.Clear();

        var allItems = m_EquippedCollection.GetAllItemStacks();
        foreach (var itemStack in allItems)
        {
            if (itemStack.Item?.ItemDefinition == null) continue;

            var itemDef = itemStack.Item.ItemDefinition;
            if (TryGetSidekickInfo(itemDef, out PartGroup partGroup, out string presetName))
            {
                ApplyClothingPart(partGroup, presetName);
                m_EquippedByGroup[partGroup] = itemDef;

                if (m_DebugLog)
                {
                    Debug.Log($"[SidekickClothingEquipHandler] Refreshed {partGroup}: {presetName}");
                }
            }
        }
    }

    /// <summary>
    /// Get the currently equipped item for a part group.
    /// </summary>
    public ItemDefinition GetEquippedItem(PartGroup partGroup)
    {
        return m_EquippedByGroup.TryGetValue(partGroup, out var item) ? item : null;
    }

    /// <summary>
    /// Check if a part group has clothing equipped.
    /// </summary>
    public bool HasEquipped(PartGroup partGroup)
    {
        return m_EquippedByGroup.ContainsKey(partGroup);
    }

    /// <summary>
    /// Called directly by SimpleInventoryUI when clothing is equipped.
    /// </summary>
    public void OnClothingEquipped(ItemDefinition itemDef)
    {
        if (itemDef == null) return;

        Debug.Log($"[SidekickClothingEquipHandler] OnClothingEquipped called for: {itemDef.name}");

        if (!TryGetSidekickInfo(itemDef, out PartGroup partGroup, out string presetName))
        {
            Debug.Log($"[SidekickClothingEquipHandler] Not a clothing item: {itemDef.name}");
            return;
        }

        Debug.Log($"[SidekickClothingEquipHandler] Applying {partGroup}: {presetName}");
        ApplyClothingPart(partGroup, presetName);
        m_EquippedByGroup[partGroup] = itemDef;
    }

    /// <summary>
    /// Called directly by SimpleInventoryUI when clothing is unequipped.
    /// </summary>
    public void OnClothingUnequipped(ItemDefinition itemDef)
    {
        if (itemDef == null) return;

        Debug.Log($"[SidekickClothingEquipHandler] OnClothingUnequipped called for: {itemDef.name}");

        if (!TryGetSidekickInfo(itemDef, out PartGroup partGroup, out string presetName))
        {
            return;
        }

        Debug.Log($"[SidekickClothingEquipHandler] Reverting {partGroup} to default");
        RevertToDefault(partGroup);
        m_EquippedByGroup.Remove(partGroup);
    }
}
