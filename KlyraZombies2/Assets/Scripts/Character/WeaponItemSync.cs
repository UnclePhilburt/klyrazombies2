using UnityEngine;
using System.Collections.Generic;
using Opsive.UltimateCharacterController.Items;
using Opsive.UltimateCharacterController.Character;
using Opsive.Shared.Events;

/// <summary>
/// Syncs weapon item positions from the Opsive skeleton to the Sidekick skeleton.
/// Since weapons are spawned by Opsive on the hidden Opsive skeleton, this component
/// ensures they appear attached to the visible Sidekick character's hands.
/// Runs with late execution order to ensure weapons are spawned first.
/// </summary>
[DefaultExecutionOrder(50)] // Run after Opsive spawns weapons but before AnimatorSync
public class WeaponItemSync : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool m_DebugLog = true;

    // Reference to the Sidekick character root
    private Transform m_SidekickRoot;

    // Cached hand bones from Sidekick skeleton
    private Transform m_SidekickRightHand;
    private Transform m_SidekickLeftHand;

    // Cached hand bones from Opsive skeleton (source)
    private Transform m_OpsiveRightHand;
    private Transform m_OpsiveLeftHand;

    // Track which items we've already processed
    private HashSet<Transform> m_ProcessedItems = new HashSet<Transform>();

    // Track original parents so we can restore when holstered
    private Dictionary<Transform, Transform> m_OriginalParents = new Dictionary<Transform, Transform>();

    // Item slots we're monitoring
    private List<Transform> m_RightHandSlots = new List<Transform>();
    private List<Transform> m_LeftHandSlots = new List<Transform>();

    private bool m_Initialized;
    private UltimateCharacterLocomotion m_CharacterLocomotion;

    private void Start()
    {
        // Find the character locomotion for event subscription
        m_CharacterLocomotion = GetComponent<UltimateCharacterLocomotion>();
        if (m_CharacterLocomotion == null)
        {
            m_CharacterLocomotion = GetComponentInParent<UltimateCharacterLocomotion>();
        }

        // Subscribe to item equip events
        if (m_CharacterLocomotion != null)
        {
            EventHandler.RegisterEvent<CharacterItem, int>(m_CharacterLocomotion.gameObject, "OnItemEquip", OnItemEquip);
            EventHandler.RegisterEvent<CharacterItem, int>(m_CharacterLocomotion.gameObject, "OnItemUnequip", OnItemUnequip);

            if (m_DebugLog)
            {
                Debug.Log("[WeaponItemSync] Subscribed to item equip events");
            }

            // Check for any items that were already equipped before we initialized
            StartCoroutine(CheckForExistingItems());
        }
    }

    private System.Collections.IEnumerator CheckForExistingItems()
    {
        // Wait a couple frames to ensure everything is initialized
        yield return null;
        yield return null;

        if (!m_Initialized)
        {
            if (m_DebugLog)
            {
                Debug.Log("[WeaponItemSync] Waiting for initialization before checking existing items...");
            }
            yield break;
        }

        // Find all CharacterItem components that might already be equipped
        var existingItems = GetComponentsInChildren<CharacterItem>(true);
        foreach (var item in existingItems)
        {
            // Skip if already processed
            if (m_ProcessedItems.Contains(item.transform))
                continue;

            if (m_DebugLog)
            {
                Debug.Log($"[WeaponItemSync] Found existing item: {item.name}");
            }

            ReparentItemToSidekick(item.transform);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (m_CharacterLocomotion != null)
        {
            EventHandler.UnregisterEvent<CharacterItem, int>(m_CharacterLocomotion.gameObject, "OnItemEquip", OnItemEquip);
            EventHandler.UnregisterEvent<CharacterItem, int>(m_CharacterLocomotion.gameObject, "OnItemUnequip", OnItemUnequip);
        }
    }

    /// <summary>
    /// Initialize with reference to the Sidekick character.
    /// Call this after the Sidekick character is created.
    /// </summary>
    public void Initialize(GameObject sidekickCharacter)
    {
        if (sidekickCharacter == null)
        {
            Debug.LogError("[WeaponItemSync] Sidekick character is null!");
            return;
        }

        m_SidekickRoot = sidekickCharacter.transform;

        // Find Sidekick hand bones (Sidekick uses hand_r, hand_l)
        m_SidekickRightHand = FindBoneInHierarchy(m_SidekickRoot,
            "hand_r", "Hand_R", "RightHand", "Right Hand", "hand.R", "HandR", "r_hand", "R_Hand");
        m_SidekickLeftHand = FindBoneInHierarchy(m_SidekickRoot,
            "hand_l", "Hand_L", "LeftHand", "Left Hand", "hand.L", "HandL", "l_hand", "L_Hand");

        // Find Opsive hand bones (on the main character, excluding Sidekick)
        FindOpsiveHandBones();

        // Find item slots on the Opsive skeleton
        FindItemSlots();

        m_Initialized = m_SidekickRightHand != null || m_SidekickLeftHand != null;

        if (m_DebugLog)
        {
            Debug.Log($"[WeaponItemSync] Initialized - Sidekick RH: {m_SidekickRightHand?.name}, LH: {m_SidekickLeftHand?.name}");
            Debug.Log($"[WeaponItemSync] Opsive RH: {m_OpsiveRightHand?.name}, LH: {m_OpsiveLeftHand?.name}");
            Debug.Log($"[WeaponItemSync] Found {m_RightHandSlots.Count} right hand slots, {m_LeftHandSlots.Count} left hand slots");
        }
    }

    private void FindOpsiveHandBones()
    {
        // Look for hand bones that are NOT part of the Sidekick character
        var allTransforms = GetComponentsInChildren<Transform>(true);
        foreach (var t in allTransforms)
        {
            // Skip if it's part of Sidekick
            if (m_SidekickRoot != null && t.IsChildOf(m_SidekickRoot))
                continue;

            string name = t.name.ToLower();

            if (m_OpsiveRightHand == null && IsRightHandBone(name))
            {
                m_OpsiveRightHand = t;
            }
            else if (m_OpsiveLeftHand == null && IsLeftHandBone(name))
            {
                m_OpsiveLeftHand = t;
            }
        }
    }

    private void FindItemSlots()
    {
        m_RightHandSlots.Clear();
        m_LeftHandSlots.Clear();

        var allTransforms = GetComponentsInChildren<Transform>(true);
        foreach (var t in allTransforms)
        {
            // Skip if it's part of Sidekick
            if (m_SidekickRoot != null && t.IsChildOf(m_SidekickRoot))
                continue;

            string name = t.name.ToLower();

            // Look for "items" containers or slot transforms
            if (name.Contains("item") || name.Contains("slot"))
            {
                // Determine if it's for right or left hand based on parent hierarchy
                Transform parent = t.parent;
                while (parent != null)
                {
                    string parentName = parent.name.ToLower();
                    if (IsRightHandBone(parentName))
                    {
                        if (!m_RightHandSlots.Contains(t))
                            m_RightHandSlots.Add(t);
                        break;
                    }
                    else if (IsLeftHandBone(parentName))
                    {
                        if (!m_LeftHandSlots.Contains(t))
                            m_LeftHandSlots.Add(t);
                        break;
                    }
                    parent = parent.parent;
                }
            }
        }
    }

    private bool IsRightHandBone(string name)
    {
        // Handle exact matches first (Sidekick uses "hand_r")
        if (name == "hand_r" || name == "Hand_R" || name == "RightHand" || name == "Right Hand")
            return true;

        // General pattern matching
        return (name.Contains("right") || name.Contains("_r") || name.EndsWith(".r") || name.StartsWith("r_"))
               && (name.Contains("hand") || name.Contains("wrist"));
    }

    private bool IsLeftHandBone(string name)
    {
        // Handle exact matches first (Sidekick uses "hand_l")
        if (name == "hand_l" || name == "Hand_L" || name == "LeftHand" || name == "Left Hand")
            return true;

        // General pattern matching
        return (name.Contains("left") || name.Contains("_l") || name.EndsWith(".l") || name.StartsWith("l_"))
               && (name.Contains("hand") || name.Contains("wrist"));
    }

    private void OnItemEquip(CharacterItem item, int slotID)
    {
        if (!m_Initialized) return;

        if (m_DebugLog)
        {
            Debug.Log($"[WeaponItemSync] Item equipped: {item.name} in slot {slotID}");
        }

        // Delay one frame to let Opsive finish setting up the item
        StartCoroutine(DelayedReparent(item.transform));
    }

    private void OnItemUnequip(CharacterItem item, int slotID)
    {
        if (m_DebugLog)
        {
            Debug.Log($"[WeaponItemSync] Item unequipped (holstered): {item.name} - restoring original parent for Opsive holstering");
        }

        // Restore original parent so Opsive can control holster positioning
        RestoreOriginalParent(item.transform);

        // Remove from processed list so it can be re-processed when equipped again
        m_ProcessedItems.Remove(item.transform);
    }

    private void RestoreOriginalParent(Transform itemTransform)
    {
        // Find the perspective item object we reparented
        var perspectiveItem = itemTransform.GetComponentInChildren<Opsive.UltimateCharacterController.ThirdPersonController.Items.ThirdPersonPerspectiveItem>(true);

        Transform objectToRestore = null;
        if (perspectiveItem != null && perspectiveItem.Object != null)
        {
            objectToRestore = perspectiveItem.Object.transform;
        }

        if (objectToRestore != null && m_OriginalParents.TryGetValue(objectToRestore, out Transform originalParent))
        {
            if (originalParent != null)
            {
                // Store world position/rotation
                Vector3 worldPos = objectToRestore.position;
                Quaternion worldRot = objectToRestore.rotation;

                // Restore to original parent
                objectToRestore.SetParent(originalParent);

                // Restore world position/rotation
                objectToRestore.position = worldPos;
                objectToRestore.rotation = worldRot;

                if (m_DebugLog)
                {
                    Debug.Log($"[WeaponItemSync] Restored {objectToRestore.name} to original parent {originalParent.name}");
                }
            }

            m_OriginalParents.Remove(objectToRestore);
        }
    }

    private System.Collections.IEnumerator DelayedReparent(Transform itemTransform)
    {
        // Wait for end of frame to let Opsive fully set up the item
        yield return new WaitForEndOfFrame();
        yield return null; // Extra frame for safety

        ReparentItemToSidekick(itemTransform);
    }

    private void ReparentItemToSidekick(Transform itemTransform)
    {
        if (itemTransform == null || m_ProcessedItems.Contains(itemTransform))
            return;

        // Get the CharacterItem component to find slot info
        var characterItem = itemTransform.GetComponent<CharacterItem>();
        if (characterItem == null)
        {
            m_ProcessedItems.Add(itemTransform);
            return;
        }

        // Use the slot ID to determine which hand (0 = right, 1 = left typically)
        int slotID = characterItem.SlotID;
        Transform targetHand = slotID == 0 ? m_SidekickRightHand : m_SidekickLeftHand;

        if (m_DebugLog)
        {
            Debug.Log($"[WeaponItemSync] Item: {itemTransform.name}, SlotID: {slotID}, Target: {targetHand?.name}");
        }

        if (targetHand == null)
        {
            if (m_DebugLog)
            {
                Debug.LogWarning($"[WeaponItemSync] No target hand for slot {slotID}");
            }
            m_ProcessedItems.Add(itemTransform);
            return;
        }

        // Find the ThirdPersonPerspectiveItem or visible object to reparent
        // Opsive weapons have perspective items that contain the visible model
        var perspectiveItem = itemTransform.GetComponentInChildren<Opsive.UltimateCharacterController.ThirdPersonController.Items.ThirdPersonPerspectiveItem>(true);

        Transform objectToMove = null;
        if (perspectiveItem != null && perspectiveItem.Object != null)
        {
            objectToMove = perspectiveItem.Object.transform;
            if (m_DebugLog)
            {
                Debug.Log($"[WeaponItemSync] Found ThirdPersonPerspectiveItem.Object: {objectToMove.name}");
            }
        }
        else
        {
            // Fallback: look for the visible mesh
            var meshRenderer = itemTransform.GetComponentInChildren<MeshRenderer>(true);
            if (meshRenderer != null)
            {
                objectToMove = meshRenderer.transform;
            }
        }

        if (objectToMove == null)
        {
            if (m_DebugLog)
            {
                Debug.LogWarning($"[WeaponItemSync] Could not find visible object for {itemTransform.name}");
            }
            m_ProcessedItems.Add(itemTransform);
            return;
        }

        // Save original parent so we can restore when holstered
        if (!m_OriginalParents.ContainsKey(objectToMove))
        {
            m_OriginalParents[objectToMove] = objectToMove.parent;
            if (m_DebugLog)
            {
                Debug.Log($"[WeaponItemSync] Saved original parent: {objectToMove.parent?.name}");
            }
        }

        // Store the world position/rotation
        Vector3 worldPos = objectToMove.position;
        Quaternion worldRot = objectToMove.rotation;

        // Reparent to Sidekick hand
        objectToMove.SetParent(targetHand);

        // Restore world position/rotation
        objectToMove.position = worldPos;
        objectToMove.rotation = worldRot;

        m_ProcessedItems.Add(itemTransform);

        if (m_DebugLog)
        {
            Debug.Log($"[WeaponItemSync] SUCCESS: Reparented {objectToMove.name} to Sidekick {targetHand.name}");
        }
    }

    private void LogHierarchy(Transform t, string indent)
    {
        string hierarchy = "";
        Transform current = t;
        while (current != null)
        {
            hierarchy = current.name + (hierarchy.Length > 0 ? " -> " + hierarchy : "");
            current = current.parent;
        }
        Debug.Log($"{indent}Hierarchy: {hierarchy}");
    }

    private string GetHierarchyPath(Transform t)
    {
        string path = t.name;
        Transform current = t.parent;
        int depth = 0;
        while (current != null && depth < 5)
        {
            path = current.name + "/" + path;
            current = current.parent;
            depth++;
        }
        return path;
    }

    private void LateUpdate()
    {
        if (!m_Initialized) return;

        // Continuously check for new CharacterItem components that may have been spawned
        // This catches items spawned through other means (not just equip events)
        CheckForNewItems();
    }

    private void CheckForNewItems()
    {
        // Find all CharacterItem components and check if they need reparenting
        var allItems = GetComponentsInChildren<CharacterItem>(true);
        foreach (var item in allItems)
        {
            if (item == null) continue;
            if (m_ProcessedItems.Contains(item.transform)) continue;

            // ONLY reparent items that are VISIBLE (equipped in hand)
            // Don't touch holstered items - let Opsive handle those
            if (!item.IsActive())
            {
                // Item is holstered/inactive - don't reparent
                continue;
            }

            // Check if this item is NOT under the Sidekick root (needs reparenting)
            if (m_SidekickRoot != null && !item.transform.IsChildOf(m_SidekickRoot))
            {
                if (m_DebugLog)
                {
                    Debug.Log($"[WeaponItemSync] LateUpdate found new ACTIVE item to reparent: {item.name}");
                }
                ReparentItemToSidekick(item.transform);
            }
        }
    }

    private Transform FindBoneInHierarchy(Transform root, params string[] possibleNames)
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            foreach (var name in possibleNames)
            {
                if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }
        }
        return null;
    }
}
