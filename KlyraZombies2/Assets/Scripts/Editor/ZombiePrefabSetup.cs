#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;

/// <summary>
/// Editor tool to quickly set up zombie prefabs from Synty models.
/// </summary>
public class ZombiePrefabSetup : EditorWindow
{
    private GameObject m_SourceModel;
    private string m_PrefabName = "Zombie";
    private float m_WalkSpeed = 1f;
    private float m_RunSpeed = 4f;
    private float m_Health = 100f;
    private float m_AttackDamage = 10f;

    [MenuItem("Project Klyra/Zombies/Prefab Setup")]
    public static void ShowWindow()
    {
        GetWindow<ZombiePrefabSetup>("Zombie Setup");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Zombie Prefab Creator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Drag a Synty zombie model here to create a game-ready zombie prefab " +
            "with NavMeshAgent, ZombieAI, and ZombieHealth components.",
            MessageType.Info);

        EditorGUILayout.Space(10);

        m_SourceModel = (GameObject)EditorGUILayout.ObjectField(
            "Source Model",
            m_SourceModel,
            typeof(GameObject),
            false);

        m_PrefabName = EditorGUILayout.TextField("Prefab Name", m_PrefabName);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);

        m_WalkSpeed = EditorGUILayout.Slider("Walk Speed", m_WalkSpeed, 0.5f, 3f);
        m_RunSpeed = EditorGUILayout.Slider("Run Speed", m_RunSpeed, 2f, 8f);
        m_Health = EditorGUILayout.Slider("Health", m_Health, 50f, 500f);
        m_AttackDamage = EditorGUILayout.Slider("Attack Damage", m_AttackDamage, 5f, 50f);

        EditorGUILayout.Space(10);

        GUI.enabled = m_SourceModel != null;
        if (GUILayout.Button("Create Zombie Prefab", GUILayout.Height(30)))
        {
            CreateZombiePrefab();
        }
        GUI.enabled = true;

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);

        if (GUILayout.Button("Setup All Synty Apocalypse Zombies"))
        {
            SetupAllSyntyZombies();
        }
    }

    private void CreateZombiePrefab()
    {
        if (m_SourceModel == null) return;

        // Create instance
        GameObject zombie = Instantiate(m_SourceModel);
        zombie.name = m_PrefabName;

        // Add NavMeshAgent
        var agent = zombie.GetComponent<NavMeshAgent>();
        if (agent == null)
            agent = zombie.AddComponent<NavMeshAgent>();

        agent.speed = m_WalkSpeed;
        agent.angularSpeed = 120f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 1.5f;
        agent.radius = 0.4f;
        agent.height = 1.8f;

        // Add ZombieAI
        var ai = zombie.GetComponent<ZombieAI>();
        if (ai == null)
            ai = zombie.AddComponent<ZombieAI>();

        // Add ZombieHealth
        var health = zombie.GetComponent<ZombieHealth>();
        if (health == null)
            health = zombie.AddComponent<ZombieHealth>();

        // Add ZombieDamageBridge for Opsive weapon compatibility
        var damageBridge = zombie.GetComponent<ZombieDamageBridge>();
        if (damageBridge == null)
            damageBridge = zombie.AddComponent<ZombieDamageBridge>();

        // Add AudioSource
        var audio = zombie.GetComponent<AudioSource>();
        if (audio == null)
            audio = zombie.AddComponent<AudioSource>();

        audio.spatialBlend = 1f;
        audio.rolloffMode = AudioRolloffMode.Linear;
        audio.maxDistance = 30f;

        // Add Capsule Collider if needed
        var collider = zombie.GetComponent<CapsuleCollider>();
        if (collider == null)
        {
            collider = zombie.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0, 0.9f, 0);
            collider.radius = 0.4f;
            collider.height = 1.8f;
        }

        // Add Rigidbody (kinematic for NavMesh)
        var rb = zombie.GetComponent<Rigidbody>();
        if (rb == null)
            rb = zombie.AddComponent<Rigidbody>();

        rb.isKinematic = true;

        // Set layer
        zombie.layer = LayerMask.NameToLayer("Enemy");
        if (zombie.layer == -1)
        {
            Debug.LogWarning("'Enemy' layer not found. Please create it in Tags and Layers.");
            zombie.layer = 0;
        }

        // Create prefab
        string path = $"Assets/Prefabs/Enemies/{m_PrefabName}.prefab";

        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Enemies"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Enemies");

        // Save prefab
        PrefabUtility.SaveAsPrefabAsset(zombie, path);
        DestroyImmediate(zombie);

        Debug.Log($"[ZombiePrefabSetup] Created zombie prefab at {path}");
        EditorUtility.DisplayDialog("Success", $"Created zombie prefab at:\n{path}", "OK");

        // Ping in project
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        EditorGUIUtility.PingObject(prefab);
    }

    private void SetupAllSyntyZombies()
    {
        // SidekickCharacters Apocalypse Zombies
        string[] sidekickZombies = new string[]
        {
            "Assets/Synty/SidekickCharacters/Characters/ApocalypseZombies/ApocalypseZombie_01/ApocalypseZombie_01.prefab",
            "Assets/Synty/SidekickCharacters/Characters/ApocalypseZombies/ApocalypseZombie_02/ApocalypseZombie_02.prefab",
            "Assets/Synty/SidekickCharacters/Characters/ApocalypseZombies/ApocalypseZombie_03/ApocalypseZombie_03.prefab",
            "Assets/Synty/SidekickCharacters/Characters/ApocalypseZombies/ApocalypseZombie_04/ApocalypseZombie_04.prefab",
            "Assets/Synty/SidekickCharacters/Characters/ApocalypseZombies/ApocalypseZombie_05/ApocalypseZombie_05.prefab",
        };

        // PolygonApocalypse Zombies
        string[] apocalypseZombies = new string[]
        {
            "Assets/Synty/PolygonApocalypse/Prefabs/Characters/SM_Chr_Zombie_Male_02.prefab",
            "Assets/Synty/PolygonApocalypse/Prefabs/Characters/SM_Chr_Zombie_Female_01.prefab",
        };

        // PolygonZombies Pack - 50 variants!
        string[] polygonZombies = new string[]
        {
            "Assets/PolygonZombies/Prefabs/Zombie_Bellboy_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Biker_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_BioHazardSuit_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Bride_Female_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Business_Female_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Businessman_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_BusinessShirt_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Cheerleader_Female_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Clown_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Coat_Female_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Daughter_Female_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Diver_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_FastfoodWorker_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Father_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Father_Male_02.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Firefighter_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Footballer_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_GamerGirl_Female_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Gangster_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Grandma_Female_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Grandpa_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Hipster_Female_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Hipster_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Hobo_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Hoodie_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_HotDogSuit_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Jacket_Female_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Jacket_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Jock_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Military_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Mother_Female_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Mother_Female_02.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Paramedic_Female_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Patient_Female_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Police_Female_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Police_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Prisoner_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Punk_Female_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Punk_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_RiotCop_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Roadworker_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_SchoolBoy_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_SchoolGirl_Female_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_ShopKeeper_Female_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_ShopKeeper_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Son_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_SummerGirl_Female_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Tourist_Male_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Underwear_Female_01.prefab",
            "Assets/PolygonZombies/Prefabs/Zombie_Underwear_Male_01.prefab",
        };

        int created = 0;

        // Process all zombie sources
        var allPaths = new System.Collections.Generic.List<string>();
        allPaths.AddRange(sidekickZombies);
        allPaths.AddRange(apocalypseZombies);
        allPaths.AddRange(polygonZombies);

        foreach (string path in allPaths)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model != null)
            {
                m_SourceModel = model;

                // Clean up the name
                string cleanName = model.name
                    .Replace("Apocalypse", "")
                    .Replace("SM_Chr_", "")
                    .Replace("Zombie_", "Zombie_")
                    .Trim();

                if (!cleanName.StartsWith("Zombie"))
                    cleanName = "Zombie_" + cleanName;

                m_PrefabName = cleanName;

                // Randomize stats slightly for variety
                m_Health = Random.Range(80f, 150f);
                m_WalkSpeed = Random.Range(0.8f, 1.5f);
                m_RunSpeed = Random.Range(3f, 5f);
                m_AttackDamage = Random.Range(8f, 15f);

                // Special zombies get buffed stats
                if (cleanName.Contains("Military") || cleanName.Contains("RiotCop") || cleanName.Contains("Firefighter"))
                {
                    m_Health = Random.Range(150f, 200f);
                    m_AttackDamage = Random.Range(12f, 20f);
                }
                else if (cleanName.Contains("Grandma") || cleanName.Contains("Grandpa") || cleanName.Contains("Patient"))
                {
                    m_WalkSpeed = Random.Range(0.5f, 0.8f);
                    m_RunSpeed = Random.Range(1.5f, 2.5f);
                    m_Health = Random.Range(50f, 80f);
                }

                CreateZombiePrefab();
                created++;
            }
        }

        EditorUtility.DisplayDialog("Complete",
            $"Created {created} zombie prefabs in Assets/Prefabs/Enemies/\n\n" +
            "Includes:\n" +
            "- 5 SidekickCharacters Apocalypse Zombies\n" +
            "- 2 PolygonApocalypse Zombies\n" +
            "- 50 PolygonZombies variants",
            "OK");
    }
    [MenuItem("Project Klyra/Zombies/Make All Lootable")]
    public static void MakeAllZombiesLootable()
    {
        // Find zombie loot table
        LootTable zombieLootTable = null;

        // Try Resources first
        zombieLootTable = Resources.Load<LootTable>("LootTables/ZombieLootTable");

        // Try finding in Assets
        if (zombieLootTable == null)
        {
            string[] guids = AssetDatabase.FindAssets("ZombieLootTable t:LootTable");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                zombieLootTable = AssetDatabase.LoadAssetAtPath<LootTable>(path);
            }
        }

        if (zombieLootTable == null)
        {
            EditorUtility.DisplayDialog("Loot Table Not Found",
                "Could not find ZombieLootTable asset.\n\n" +
                "Please create one at:\nAssets/Resources/LootTables/ZombieLootTable.asset",
                "OK");
            return;
        }

        // Find all zombie prefabs
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        int updatedCount = 0;
        int totalZombies = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null) continue;

            // Check if it has ZombieAI
            ZombieAI zombieAI = prefab.GetComponent<ZombieAI>();
            if (zombieAI == null) continue;

            totalZombies++;

            // Modify the prefab
            using (var editScope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                GameObject prefabRoot = editScope.prefabContentsRoot;
                ZombieAI ai = prefabRoot.GetComponent<ZombieAI>();

                if (ai != null)
                {
                    bool changed = false;
                    SerializedObject so = new SerializedObject(ai);

                    // Enable lootable on death
                    var lootableProp = so.FindProperty("m_LootableOnDeath");
                    if (lootableProp != null)
                    {
                        if (!lootableProp.boolValue)
                        {
                            lootableProp.boolValue = true;
                            changed = true;
                        }
                    }

                    // Set loot table
                    var lootTableProp = so.FindProperty("m_LootTable");
                    if (lootTableProp != null)
                    {
                        if (lootTableProp.objectReferenceValue == null)
                        {
                            lootTableProp.objectReferenceValue = zombieLootTable;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        so.ApplyModifiedPropertiesWithoutUndo();
                        updatedCount++;
                        Debug.Log($"[ZombiePrefabSetup] Updated: {prefab.name}");
                    }
                }
            }
        }

        EditorUtility.DisplayDialog("Zombie Setup Complete",
            $"Found {totalZombies} zombie prefabs.\n" +
            $"Updated {updatedCount} prefabs.\n\n" +
            $"Loot Table: {zombieLootTable.name}",
            "OK");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Project Klyra/Zombies/List All Prefabs")]
    public static void ListAllZombiePrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        string report = "Zombie Prefabs Found:\n\n";
        int count = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null) continue;

            ZombieAI zombieAI = prefab.GetComponent<ZombieAI>();
            if (zombieAI == null) continue;

            count++;

            SerializedObject so = new SerializedObject(zombieAI);
            var lootableProp = so.FindProperty("m_LootableOnDeath");
            var lootTableProp = so.FindProperty("m_LootTable");

            bool isLootable = lootableProp != null && lootableProp.boolValue;
            bool hasLootTable = lootTableProp != null && lootTableProp.objectReferenceValue != null;
            string tableName = hasLootTable ? lootTableProp.objectReferenceValue.name : "None";

            report += $"• {prefab.name}\n";
            report += $"  Lootable: {(isLootable ? "Yes" : "No")}, Table: {tableName}\n";
        }

        report += $"\nTotal: {count} zombie prefabs";
        Debug.Log(report);

        EditorUtility.DisplayDialog("Zombie Prefabs", report, "OK");
    }
}
#endif
