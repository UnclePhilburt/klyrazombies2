#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor tool to setup BloodEffectManager with Synty blood effects and sounds
/// </summary>
public class BloodEffectSetup : EditorWindow
{
    [MenuItem("Tools/Setup Blood Effects")]
    public static void SetupBloodEffects()
    {
        // Find or create BloodEffectManager in scene
        BloodEffectManager manager = Object.FindFirstObjectByType<BloodEffectManager>();

        if (manager == null)
        {
            GameObject managerObj = new GameObject("BloodEffectManager");
            manager = managerObj.AddComponent<BloodEffectManager>();
            Undo.RegisterCreatedObjectUndo(managerObj, "Create BloodEffectManager");
            Debug.Log("[BloodEffectSetup] Created BloodEffectManager GameObject");
        }

        SerializedObject so = new SerializedObject(manager);

        // === BLOOD SPLATTER PREFAB ===
        // Try to find the best blood splatter effect
        string[] splatterPaths = new string[]
        {
            "Assets/Synty/PolygonApocalypse/Prefabs/FX/FX_Blood_Splat_01.prefab",
            "Assets/Synty/PolygonPoliceStation/Prefabs/FX/FX_BloodSplatter_01.prefab",
            "Assets/Synty/PolygonGeneric/Prefabs/FX/FX_Blood_Splatter_01.prefab"
        };

        GameObject splatterPrefab = null;
        foreach (string path in splatterPaths)
        {
            splatterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (splatterPrefab != null)
            {
                Debug.Log($"[BloodEffectSetup] Found splatter: {path}");
                break;
            }
        }

        if (splatterPrefab != null)
        {
            SerializedProperty splatterProp = so.FindProperty("m_BloodSplatterPrefab");
            if (splatterProp != null)
                splatterProp.objectReferenceValue = splatterPrefab;
        }

        // === BLOOD POOL PREFABS ===
        // Load all blood pool variants for variety
        string[] poolPaths = new string[]
        {
            "Assets/Synty/PolygonApocalypse/Prefabs/Props/SM_Prop_BloodPool_01.prefab",
            "Assets/Synty/PolygonApocalypse/Prefabs/Props/SM_Prop_BloodPool_02.prefab",
            "Assets/Synty/PolygonApocalypse/Prefabs/Props/SM_Prop_BloodPool_03.prefab",
            "Assets/Synty/PolygonApocalypse/Prefabs/Props/SM_Prop_BloodPool_04.prefab",
            "Assets/Synty/PolygonApocalypse/Prefabs/Props/SM_Prop_BloodPool_05.prefab"
        };

        List<GameObject> poolPrefabs = new List<GameObject>();
        foreach (string path in poolPaths)
        {
            GameObject pool = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (pool != null)
            {
                poolPrefabs.Add(pool);
            }
        }

        // Use the first pool as the main prefab
        if (poolPrefabs.Count > 0)
        {
            SerializedProperty poolProp = so.FindProperty("m_BloodPoolPrefab");
            if (poolProp != null)
                poolProp.objectReferenceValue = poolPrefabs[0];

            // Set pool prefabs array if it exists
            SerializedProperty poolsProp = so.FindProperty("m_BloodPoolPrefabs");
            if (poolsProp != null && poolsProp.isArray)
            {
                poolsProp.arraySize = poolPrefabs.Count;
                for (int i = 0; i < poolPrefabs.Count; i++)
                {
                    poolsProp.GetArrayElementAtIndex(i).objectReferenceValue = poolPrefabs[i];
                }
            }
        }

        // === BLOOD SOUNDS ===
        List<AudioClip> bloodSounds = new List<AudioClip>();

        // Load blood splash sounds
        string bloodSoundPath = "Assets/ZombieHorrorPackageFree/WAV/Blood";
        if (AssetDatabase.IsValidFolder(bloodSoundPath))
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { bloodSoundPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                    bloodSounds.Add(clip);
            }
        }

        // Also add impact sounds
        string impactSoundPath = "Assets/ZombieHorrorPackageFree/WAV/Impact";
        if (AssetDatabase.IsValidFolder(impactSoundPath))
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { impactSoundPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                    bloodSounds.Add(clip);
            }
        }

        if (bloodSounds.Count > 0)
        {
            SerializedProperty soundsProp = so.FindProperty("m_BloodSplashSounds");
            if (soundsProp != null && soundsProp.isArray)
            {
                soundsProp.arraySize = bloodSounds.Count;
                for (int i = 0; i < bloodSounds.Count; i++)
                {
                    soundsProp.GetArrayElementAtIndex(i).objectReferenceValue = bloodSounds[i];
                }
            }
        }

        // Disable auto-generate since we have real prefabs
        SerializedProperty autoGenProp = so.FindProperty("m_AutoGenerateIfMissing");
        if (autoGenProp != null)
            autoGenProp.boolValue = false;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);

        EditorUtility.DisplayDialog("Blood Effects Setup",
            $"BloodEffectManager configured with Synty effects!\n\n" +
            $"- Blood splatter: {(splatterPrefab != null ? splatterPrefab.name : "None")}\n" +
            $"- Blood pools: {poolPrefabs.Count} variants\n" +
            $"- Blood sounds: {bloodSounds.Count}\n\n" +
            "Synty's stylized blood effects will now appear\n" +
            "when zombies are hit and killed!",
            "OK");

        Selection.activeGameObject = manager.gameObject;
        EditorGUIUtility.PingObject(manager);
    }
}
#endif
