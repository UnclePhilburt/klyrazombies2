using UnityEngine;
using UnityEditor;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;
using Opsive.UltimateInventorySystem.ItemActions;
using Opsive.UltimateInventorySystem.Exchange;
using Opsive.UltimateInventorySystem.Interactions;
using Opsive.UltimateCharacterController.Integrations.UltimateInventorySystem;

/// <summary>
/// Editor tool to set up the SidekickPlayerBase prefab with all necessary
/// UIS (Ultimate Inventory System) components for inventory, looting, and UI.
/// </summary>
public class PlayerPrefabSetup : EditorWindow
{
    private GameObject targetPrefab;
    private GameObject sourcePrefab;

    [MenuItem("Project Klyra/Player/Setup Player Prefab Components")]
    public static void ShowWindow()
    {
        GetWindow<PlayerPrefabSetup>("Player Prefab Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Player Prefab Component Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "This tool adds all necessary UIS components to the SidekickPlayerBase prefab.\n\n" +
            "Components added:\n" +
            "- InventoryIdentifier\n" +
            "- Inventory (UIS)\n" +
            "- ItemUser\n" +
            "- CurrencyOwner\n" +
            "- InventoryInteractor\n" +
            "- CharacterInventoryBridge\n" +
            "- InventoryItemSetManager\n" +
            "- DynamicInventorySize\n" +
            "- BackpackEquipHandler\n" +
            "- LootableInteraction\n" +
            "- SidekickClothingEquipHandler",
            MessageType.Info);

        GUILayout.Space(10);

        targetPrefab = (GameObject)EditorGUILayout.ObjectField("Target Prefab (New)", targetPrefab, typeof(GameObject), false);
        sourcePrefab = (GameObject)EditorGUILayout.ObjectField("Source Prefab (Old)", sourcePrefab, typeof(GameObject), false);

        GUILayout.Space(10);

        if (GUILayout.Button("Load Default Prefabs"))
        {
            LoadDefaultPrefabs();
        }

        GUILayout.Space(10);

        EditorGUI.BeginDisabledGroup(targetPrefab == null);
        if (GUILayout.Button("Add Missing Components to Target"))
        {
            AddMissingComponents();
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(10);

        EditorGUI.BeginDisabledGroup(targetPrefab == null || sourcePrefab == null);
        if (GUILayout.Button("Copy Component Settings from Source"))
        {
            CopyComponentSettings();
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(20);
        GUILayout.Label("Manual Steps After Setup:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "After running setup:\n" +
            "1. Open the prefab and verify components are added\n" +
            "2. Assign the InventoryDatabase to the Inventory component\n" +
            "3. Set up ItemCollections (Default, Equippable, etc.)\n" +
            "4. Configure backpack attach points\n" +
            "5. Test in play mode",
            MessageType.Warning);
    }

    private void LoadDefaultPrefabs()
    {
        // Try to find the prefabs
        string[] targetGuids = AssetDatabase.FindAssets("SidekickPlayerBase t:Prefab");
        string[] sourceGuids = AssetDatabase.FindAssets("SM_Chr_Biker_Male_01 t:Prefab");

        if (targetGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(targetGuids[0]);
            targetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Debug.Log($"Loaded target prefab: {path}");
        }

        if (sourceGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(sourceGuids[0]);
            sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Debug.Log($"Loaded source prefab: {path}");
        }
    }

    private void AddMissingComponents()
    {
        if (targetPrefab == null)
        {
            Debug.LogError("No target prefab assigned!");
            return;
        }

        // Open prefab for editing
        string prefabPath = AssetDatabase.GetAssetPath(targetPrefab);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        try
        {
            int addedCount = 0;

            // Add UIS components if missing
            if (prefabRoot.GetComponent<InventoryIdentifier>() == null)
            {
                prefabRoot.AddComponent<InventoryIdentifier>();
                addedCount++;
                Debug.Log("Added: InventoryIdentifier");
            }

            // Check for UIS Inventory (not UCC Inventory)
            var uisInventory = prefabRoot.GetComponent<Inventory>();
            if (uisInventory == null)
            {
                prefabRoot.AddComponent<Inventory>();
                addedCount++;
                Debug.Log("Added: Inventory (UIS)");
            }

            if (prefabRoot.GetComponent<ItemUser>() == null)
            {
                prefabRoot.AddComponent<ItemUser>();
                addedCount++;
                Debug.Log("Added: ItemUser");
            }

            if (prefabRoot.GetComponent<CurrencyOwner>() == null)
            {
                prefabRoot.AddComponent<CurrencyOwner>();
                addedCount++;
                Debug.Log("Added: CurrencyOwner");
            }

            if (prefabRoot.GetComponent<InventoryInteractor>() == null)
            {
                prefabRoot.AddComponent<InventoryInteractor>();
                addedCount++;
                Debug.Log("Added: InventoryInteractor");
            }

            if (prefabRoot.GetComponent<CharacterInventoryBridge>() == null)
            {
                prefabRoot.AddComponent<CharacterInventoryBridge>();
                addedCount++;
                Debug.Log("Added: CharacterInventoryBridge");
            }

            if (prefabRoot.GetComponent<InventoryItemSetManager>() == null)
            {
                prefabRoot.AddComponent<InventoryItemSetManager>();
                addedCount++;
                Debug.Log("Added: InventoryItemSetManager");
            }

            if (prefabRoot.GetComponent<DynamicInventorySize>() == null)
            {
                prefabRoot.AddComponent<DynamicInventorySize>();
                addedCount++;
                Debug.Log("Added: DynamicInventorySize");
            }

            // Add custom components
            if (prefabRoot.GetComponent<BackpackEquipHandler>() == null)
            {
                prefabRoot.AddComponent<BackpackEquipHandler>();
                addedCount++;
                Debug.Log("Added: BackpackEquipHandler");
            }

            // Note: BackpackAttachmentHandler was removed - it duplicates BackpackEquipHandler functionality

            if (prefabRoot.GetComponent<LootableInteraction>() == null)
            {
                prefabRoot.AddComponent<LootableInteraction>();
                addedCount++;
                Debug.Log("Added: LootableInteraction");
            }

            if (prefabRoot.GetComponent<HolsterPositionAdjuster>() == null)
            {
                prefabRoot.AddComponent<HolsterPositionAdjuster>();
                addedCount++;
                Debug.Log("Added: HolsterPositionAdjuster");
            }

            if (prefabRoot.GetComponent<SidekickClothingEquipHandler>() == null)
            {
                prefabRoot.AddComponent<SidekickClothingEquipHandler>();
                addedCount++;
                Debug.Log("Added: SidekickClothingEquipHandler");
            }

            // Create backpack attach points if they don't exist
            CreateAttachPointsIfMissing(prefabRoot, ref addedCount);

            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            Debug.Log($"Added {addedCount} components to {targetPrefab.name}");

            EditorUtility.DisplayDialog("Setup Complete",
                $"Added {addedCount} components to {targetPrefab.name}.\n\n" +
                "Please open the prefab and configure the component settings.",
                "OK");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private void CopyComponentSettings()
    {
        if (targetPrefab == null || sourcePrefab == null)
        {
            Debug.LogError("Both prefabs must be assigned!");
            return;
        }

        // This is complex because we need to copy serialized data
        // For now, just show what needs to be manually copied
        EditorUtility.DisplayDialog("Manual Copy Required",
            "Due to the complexity of Opsive's serialized data, component settings must be copied manually:\n\n" +
            "1. Open both prefabs side by side\n" +
            "2. For each component, copy the settings from old to new\n" +
            "3. Key components to configure:\n" +
            "   - Inventory: Set up ItemCollections\n" +
            "   - CharacterInventoryBridge: Copy collection names\n" +
            "   - InventoryItemSetManager: Copy ItemSetRules\n" +
            "   - DynamicInventorySize: Copy bag settings\n\n" +
            "Or use Unity's 'Copy Component' / 'Paste Component Values' feature.",
            "OK");
    }

    private void CreateAttachPointsIfMissing(GameObject prefabRoot, ref int addedCount)
    {
        // Find the spine bone for backpack attachment
        Transform spineBone = FindBoneRecursive(prefabRoot.transform, "spine_03", "Spine3", "spine3");
        if (spineBone == null)
        {
            spineBone = FindBoneRecursive(prefabRoot.transform, "spine_02", "Spine2", "spine2");
        }
        if (spineBone == null)
        {
            spineBone = FindBoneRecursive(prefabRoot.transform, "spine", "Spine");
        }

        if (spineBone == null)
        {
            Debug.LogWarning("Could not find spine bone for backpack attach points. Add them manually.");
            return;
        }

        // Create Small Backpack Attach Point
        if (FindChildByName(prefabRoot.transform, "SmallBackpackAttachPoint") == null)
        {
            GameObject smallAttach = new GameObject("SmallBackpackAttachPoint");
            smallAttach.transform.SetParent(spineBone);
            smallAttach.transform.localPosition = new Vector3(0, 0.1f, -0.15f);
            smallAttach.transform.localRotation = Quaternion.identity;
            addedCount++;
            Debug.Log("Created: SmallBackpackAttachPoint");
        }

        // Create Medium Backpack Attach Point
        if (FindChildByName(prefabRoot.transform, "MediumBackpackAttachPoint") == null)
        {
            GameObject mediumAttach = new GameObject("MediumBackpackAttachPoint");
            mediumAttach.transform.SetParent(spineBone);
            mediumAttach.transform.localPosition = new Vector3(0, 0.1f, -0.18f);
            mediumAttach.transform.localRotation = Quaternion.identity;
            addedCount++;
            Debug.Log("Created: MediumBackpackAttachPoint");
        }

        // Create Large Backpack Attach Point
        if (FindChildByName(prefabRoot.transform, "LargeBackpackAttachPoint") == null)
        {
            GameObject largeAttach = new GameObject("LargeBackpackAttachPoint");
            largeAttach.transform.SetParent(spineBone);
            largeAttach.transform.localPosition = new Vector3(0, 0.1f, -0.22f);
            largeAttach.transform.localRotation = Quaternion.identity;
            addedCount++;
            Debug.Log("Created: LargeBackpackAttachPoint");
        }

        // Create Backpack Holster Spots (for rifle holster adjustment)
        Transform pelvisBone = FindBoneRecursive(prefabRoot.transform, "pelvis", "Pelvis", "Hips");
        if (pelvisBone != null)
        {
            if (FindChildByName(prefabRoot.transform, "Small Backpack Holster Spot") == null)
            {
                GameObject smallHolster = new GameObject("Small Backpack Holster Spot");
                smallHolster.transform.SetParent(spineBone);
                smallHolster.transform.localPosition = new Vector3(0.2f, 0, -0.1f);
                smallHolster.transform.localRotation = Quaternion.identity;
                addedCount++;
            }

            if (FindChildByName(prefabRoot.transform, "Medium Backpack Holster Spot") == null)
            {
                GameObject mediumHolster = new GameObject("Medium Backpack Holster Spot");
                mediumHolster.transform.SetParent(spineBone);
                mediumHolster.transform.localPosition = new Vector3(0.25f, 0, -0.12f);
                mediumHolster.transform.localRotation = Quaternion.identity;
                addedCount++;
            }

            if (FindChildByName(prefabRoot.transform, "Large Backpack Holster Spot") == null)
            {
                GameObject largeHolster = new GameObject("Large Backpack Holster Spot");
                largeHolster.transform.SetParent(spineBone);
                largeHolster.transform.localPosition = new Vector3(0.3f, 0, -0.15f);
                largeHolster.transform.localRotation = Quaternion.identity;
                addedCount++;
            }
        }
    }

    private Transform FindBoneRecursive(Transform root, params string[] possibleNames)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            foreach (string name in possibleNames)
            {
                if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }
        }
        return null;
    }

    private Transform FindChildByName(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                return child;
            }
        }
        return null;
    }
}
