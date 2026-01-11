#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Editor tool to assign zombie sounds from ZombieHorrorPackageFree to zombie prefabs
/// </summary>
public class ZombieSoundAssigner : EditorWindow
{
    private static string SoundBasePath = "Assets/ZombieHorrorPackageFree/WAV";

    [MenuItem("Project Klyra/Zombies/Assign Sounds")]
    public static void AssignSounds()
    {
        // Load all sounds
        var idleSounds = new List<AudioClip>();
        var attackSounds = new List<AudioClip>();
        var hurtSounds = new List<AudioClip>();
        var deathSounds = new List<AudioClip>();

        // Load Idle sounds from both zombie variants
        idleSounds.AddRange(LoadClips($"{SoundBasePath}/VO/Zombie01", "Idle"));
        idleSounds.AddRange(LoadClips($"{SoundBasePath}/VO/Zombie03", "Idle"));

        // Load Attack sounds (vocals + bite sounds)
        attackSounds.AddRange(LoadClips($"{SoundBasePath}/VO/Zombie01", "Attack"));
        attackSounds.AddRange(LoadClips($"{SoundBasePath}/VO/Zombie03", "Attack"));
        attackSounds.AddRange(LoadClips($"{SoundBasePath}/Bite", "Bite"));

        // Load Hurt sounds (vocals + impacts)
        hurtSounds.AddRange(LoadClips($"{SoundBasePath}/VO/Zombie01", "Hurt"));
        hurtSounds.AddRange(LoadClips($"{SoundBasePath}/VO/Zombie03", "Hurt"));
        hurtSounds.AddRange(LoadClips($"{SoundBasePath}/Impact", "Impact"));

        // Load Death sounds (body fall + hurt sounds for variety)
        deathSounds.AddRange(LoadClips($"{SoundBasePath}/BodyFall", "BodyFall"));

        Debug.Log($"[ZombieSoundAssigner] Loaded sounds - Idle: {idleSounds.Count}, Attack: {attackSounds.Count}, Hurt: {hurtSounds.Count}, Death: {deathSounds.Count}");

        if (idleSounds.Count == 0 && attackSounds.Count == 0)
        {
            EditorUtility.DisplayDialog("Error",
                "No sounds found!\n\nExpected path: Assets/ZombieHorrorPackageFree/WAV/",
                "OK");
            return;
        }

        // Find all zombie prefabs
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Enemies" });
        int updated = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null) continue;

            var zombieHealth = prefab.GetComponent<ZombieHealth>();
            if (zombieHealth == null) continue;

            // Open prefab for editing
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            var health = prefabRoot.GetComponent<ZombieHealth>();
            var ai = prefabRoot.GetComponent<ZombieAI>();

            if (health != null)
            {
                // Use SerializedObject to set the arrays
                SerializedObject healthSO = new SerializedObject(health);

                SetAudioClipArray(healthSO, "m_HitSounds", hurtSounds);
                SetAudioClipArray(healthSO, "m_DeathSounds", deathSounds);

                healthSO.ApplyModifiedProperties();
            }

            if (ai != null)
            {
                SerializedObject aiSO = new SerializedObject(ai);

                SetAudioClipArray(aiSO, "m_IdleSounds", idleSounds);
                SetAudioClipArray(aiSO, "m_AlertSounds", attackSounds); // Use attack sounds for alert too
                SetAudioClipArray(aiSO, "m_AttackSounds", attackSounds);

                aiSO.ApplyModifiedProperties();
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            updated++;
            Debug.Log($"[ZombieSoundAssigner] Updated: {prefab.name}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Complete",
            $"Assigned sounds to {updated} zombie prefabs!\n\n" +
            $"Sounds loaded:\n" +
            $"- Idle: {idleSounds.Count} clips\n" +
            $"- Attack: {attackSounds.Count} clips\n" +
            $"- Hurt: {hurtSounds.Count} clips\n" +
            $"- Death: {deathSounds.Count} clips",
            "OK");
    }

    private static List<AudioClip> LoadClips(string folderPath, string filter)
    {
        var clips = new List<AudioClip>();

        if (!AssetDatabase.IsValidFolder(folderPath))
            return clips;

        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folderPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);

            if (fileName.Contains(filter))
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                {
                    clips.Add(clip);
                }
            }
        }

        return clips;
    }

    private static void SetAudioClipArray(SerializedObject so, string propertyName, List<AudioClip> clips)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null && prop.isArray)
        {
            prop.arraySize = clips.Count;
            for (int i = 0; i < clips.Count; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
            }
        }
    }

    [MenuItem("Project Klyra/Zombies/Test Sounds")]
    public static void TestSounds()
    {
        // Quick test to verify sounds are loaded
        var idleSounds = LoadClips($"{SoundBasePath}/VO/Zombie01", "Idle");
        var attackSounds = LoadClips($"{SoundBasePath}/Bite", "Bite");
        var hurtSounds = LoadClips($"{SoundBasePath}/Impact", "Impact");
        var deathSounds = LoadClips($"{SoundBasePath}/BodyFall", "BodyFall");

        string message = "Sound files found:\n\n";
        message += $"Idle (Zombie01): {idleSounds.Count}\n";
        message += $"Attack (Bite): {attackSounds.Count}\n";
        message += $"Hurt (Impact): {hurtSounds.Count}\n";
        message += $"Death (BodyFall): {deathSounds.Count}\n";

        foreach (var clip in idleSounds)
            message += $"  - {clip.name}\n";

        EditorUtility.DisplayDialog("Sound Test", message, "OK");
    }
}
#endif
