#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool to add missing components to zombie prefabs
/// </summary>
public class ZombieComponentAdder : EditorWindow
{
    [MenuItem("Project Klyra/Zombies/Add Damage Bridge to All")]
    public static void AddDamageBridgeToAll()
    {
        int prefabsFixed = 0;
        int sceneFixed = 0;

        // Fix prefabs in Assets/Prefabs/Enemies
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Enemies" });
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null) continue;

            var zombieHealth = prefab.GetComponent<ZombieHealth>();
            if (zombieHealth == null) continue;

            var damageBridge = prefab.GetComponent<ZombieDamageBridge>();
            if (damageBridge == null)
            {
                // Open prefab for editing
                string prefabPath = AssetDatabase.GetAssetPath(prefab);
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

                prefabRoot.AddComponent<ZombieDamageBridge>();

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                PrefabUtility.UnloadPrefabContents(prefabRoot);

                prefabsFixed++;
                Debug.Log($"[ZombieComponentAdder] Added ZombieDamageBridge to prefab: {prefab.name}");
            }
        }

        // Fix zombies in current scene
        ZombieHealth[] zombiesInScene = Object.FindObjectsByType<ZombieHealth>(FindObjectsSortMode.None);
        foreach (var zombieHealth in zombiesInScene)
        {
            var damageBridge = zombieHealth.GetComponent<ZombieDamageBridge>();
            if (damageBridge == null)
            {
                Undo.AddComponent<ZombieDamageBridge>(zombieHealth.gameObject);
                sceneFixed++;
                Debug.Log($"[ZombieComponentAdder] Added ZombieDamageBridge to scene object: {zombieHealth.name}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Complete",
            $"Added ZombieDamageBridge to:\n" +
            $"- {prefabsFixed} prefabs\n" +
            $"- {sceneFixed} scene objects\n\n" +
            "Zombies can now be damaged by Opsive weapons!",
            "OK");
    }

    [MenuItem("Project Klyra/Zombies/Validate Setup")]
    public static void ValidateZombieSetup()
    {
        ZombieHealth[] zombies = Object.FindObjectsByType<ZombieHealth>(FindObjectsSortMode.None);

        int total = zombies.Length;
        int missingDamageBridge = 0;
        int missingNavAgent = 0;
        int missingAI = 0;
        int notOnNavMesh = 0;

        foreach (var zombie in zombies)
        {
            if (zombie.GetComponent<ZombieDamageBridge>() == null)
                missingDamageBridge++;

            var agent = zombie.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent == null)
                missingNavAgent++;
            else if (!agent.isOnNavMesh)
                notOnNavMesh++;

            if (zombie.GetComponent<ZombieAI>() == null)
                missingAI++;
        }

        string message = $"Found {total} zombies in scene:\n\n";

        if (missingDamageBridge > 0)
            message += $"- {missingDamageBridge} missing ZombieDamageBridge (can't be damaged!)\n";
        if (missingNavAgent > 0)
            message += $"- {missingNavAgent} missing NavMeshAgent (can't move!)\n";
        if (missingAI > 0)
            message += $"- {missingAI} missing ZombieAI (no behavior!)\n";
        if (notOnNavMesh > 0)
            message += $"- {notOnNavMesh} not on NavMesh (won't move!)\n";

        if (missingDamageBridge == 0 && missingNavAgent == 0 && missingAI == 0 && notOnNavMesh == 0)
            message += "All zombies are properly configured!";

        EditorUtility.DisplayDialog("Zombie Validation", message, "OK");
    }
}
#endif
