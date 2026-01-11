#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using DistantLands.Cozy.Data;

/// <summary>
/// Editor tool to replace Cozy ReSound tracks with horror ambient music
/// </summary>
public class HorrorMusicSetup : EditorWindow
{
    [MenuItem("Project Klyra/Effects/Setup Horror Music")]
    public static void SetupHorrorMusic()
    {
        // Find all horror audio clips
        List<AudioClip> horrorClips = new List<AudioClip>();

        // Load from "free horror ambience 2"
        string[] haGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/free horror ambience 2" });
        foreach (string guid in haGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null)
                horrorClips.Add(clip);
        }

        // Load from "music/darkness"
        string[] dkGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/music/darkness" });
        foreach (string guid in dkGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null)
                horrorClips.Add(clip);
        }

        if (horrorClips.Count == 0)
        {
            EditorUtility.DisplayDialog("Horror Music Setup",
                "No horror audio clips found!\n\nExpected folders:\n- Assets/free horror ambience 2\n- Assets/music/darkness",
                "OK");
            return;
        }

        Debug.Log($"[HorrorMusicSetup] Found {horrorClips.Count} horror clips");

        // Find Cozy ReSound track assets
        string[] trackGuids = AssetDatabase.FindAssets("t:ReSoundTrack", new[] { "Packages/com.distantlands.cozy.resound" });

        if (trackGuids.Length == 0)
        {
            EditorUtility.DisplayDialog("Horror Music Setup",
                "No Cozy ReSound tracks found!\n\nMake sure Cozy ReSound module is installed.",
                "OK");
            return;
        }

        // Map Cozy tracks to appropriate horror tracks
        Dictionary<string, string[]> trackMapping = new Dictionary<string, string[]>
        {
            // Night tracks - creepy, unsettling
            { "Night 01", new[] { "dk-fear", "dk-theroom", "ha-backrooms" } },
            { "Night 02", new[] { "dk-surrounded", "dk-breath", "ha-suffocate" } },

            // Wonder/calm tracks - tense exploration
            { "Wonder 01", new[] { "dk-atmosphere", "dk-lookaroundyou", "ha-hesitate" } },
            { "Wonder 02", new[] { "dk-bleak", "dk-lookaboveyou", "ha-undercurrent" } },

            // Rainy tracks - oppressive, heavy
            { "Rainy 01", new[] { "dk-condemned", "ha-pressure", "ha-distillery" } },
            { "Rainy 02", new[] { "dk-sub", "ha-pressure-nofx", "ha-waterheater" } },

            // Wind tracks - eerie, howling
            { "Wind 01", new[] { "dk-darkhorns", "ha-amorph", "dk-hello" } },
            { "Wind 02", new[] { "dk-chaos", "ha-abomination", "dk-scratch" } },

            // Quiet tracks - subtle dread
            { "Quiet 01", new[] { "dk-angels", "ha-simplestring_1", "dk-musicbox" } },
            { "Quiet 02", new[] { "dk-brokenbells", "ha-undercurrent2", "dk-effthis" } },

            // Sunset - tension building
            { "Sunset 01", new[] { "dk-shoot", "ha-crunchy_1", "dk-condemned" } },

            // Seasons - various moods
            { "Seasons - Winter", new[] { "dk-bleak", "ha-suffocate" } },
            { "Seasons - Spring", new[] { "dk-atmosphere", "ha-hesitate" } },
            { "Seasons - Summer", new[] { "dk-lookaroundyou", "ha-undercurrent" } },
            { "Seasons - Autumn", new[] { "dk-fear", "ha-backrooms" } },
        };

        int replacedCount = 0;

        foreach (string guid in trackGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ReSoundTrack track = AssetDatabase.LoadAssetAtPath<ReSoundTrack>(path);

            if (track == null) continue;

            string trackName = track.name;

            // Find matching horror clip
            AudioClip newClip = null;

            if (trackMapping.ContainsKey(trackName))
            {
                // Try to find one of the mapped clips
                foreach (string clipName in trackMapping[trackName])
                {
                    newClip = horrorClips.Find(c => c.name.ToLower().Contains(clipName.ToLower().Replace("dk-", "").Replace("ha-", "")));
                    if (newClip != null) break;

                    // Try exact match
                    newClip = horrorClips.Find(c => c.name.Equals(clipName, System.StringComparison.OrdinalIgnoreCase));
                    if (newClip != null) break;
                }
            }

            // Fallback: assign a random horror clip
            if (newClip == null && horrorClips.Count > 0)
            {
                newClip = horrorClips[replacedCount % horrorClips.Count];
            }

            if (newClip != null)
            {
                SerializedObject so = new SerializedObject(track);
                SerializedProperty clipProp = so.FindProperty("clip");

                if (clipProp != null)
                {
                    clipProp.objectReferenceValue = newClip;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(track);

                    Debug.Log($"[HorrorMusicSetup] {trackName} -> {newClip.name}");
                    replacedCount++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Horror Music Setup",
            $"Replaced {replacedCount} Cozy ReSound tracks with horror music!\n\n" +
            "The Cozy music system will now play horror ambient tracks\n" +
            "based on time of day and weather conditions.",
            "OK");
    }

    [MenuItem("Project Klyra/Effects/List Horror Tracks")]
    public static void ListHorrorTracks()
    {
        Debug.Log("=== HORROR AMBIENT TRACKS ===");

        string[] haGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/free horror ambience 2" });
        Debug.Log($"\n--- free horror ambience 2 ({haGuids.Length} tracks) ---");
        foreach (string guid in haGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null)
                Debug.Log($"  {clip.name} ({clip.length:F1}s)");
        }

        string[] dkGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/music/darkness" });
        Debug.Log($"\n--- music/darkness ({dkGuids.Length} tracks) ---");
        foreach (string guid in dkGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null)
                Debug.Log($"  {clip.name} ({clip.length:F1}s)");
        }
    }
}
#endif
