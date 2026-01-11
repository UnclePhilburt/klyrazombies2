#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Converts PolygonZombies materials from custom shader to URP/Lit
/// </summary>
public class ZombieMaterialConverter : EditorWindow
{
    [MenuItem("Project Klyra/Zombies/Convert Materials to URP")]
    public static void ConvertMaterials()
    {
        string[] materialsPaths = new string[]
        {
            "Assets/PolygonZombies/Materials",
            "Assets/PolygonZombies/Materials/Alts"
        };

        string[] texturesPaths = new string[]
        {
            "Assets/PolygonZombies/Textures",
            "Assets/PolygonZombies/Textures/Alts"
        };

        // Find URP Lit shader
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            EditorUtility.DisplayDialog("Error", "URP Lit shader not found! Make sure URP is installed.", "OK");
            return;
        }

        // Build texture lookup dictionary
        Dictionary<string, Texture2D> textureLookup = new Dictionary<string, Texture2D>();
        foreach (string texPath in texturesPaths)
        {
            if (!AssetDatabase.IsValidFolder(texPath)) continue;

            string[] texGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { texPath });
            foreach (string guid in texGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string texName = Path.GetFileNameWithoutExtension(path);
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null && !textureLookup.ContainsKey(texName))
                {
                    textureLookup[texName] = tex;
                }
            }
        }

        int converted = 0;
        int texturesAssigned = 0;

        foreach (string materialsPath in materialsPaths)
        {
            if (!AssetDatabase.IsValidFolder(materialsPath)) continue;

            string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { materialsPath });

            foreach (string guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (mat == null) continue;

                string matName = Path.GetFileNameWithoutExtension(path);

                // Get the main texture before changing shader
                Texture mainTex = null;
                float smoothness = 0.2f;

                // Try to get existing texture from custom shader properties
                string[] texProps = { "_Texture", "_MainTex", "_BaseMap", "_Albedo" };
                foreach (string prop in texProps)
                {
                    if (mat.HasProperty(prop))
                    {
                        mainTex = mat.GetTexture(prop);
                        if (mainTex != null) break;
                    }
                }

                if (mat.HasProperty("_Smoothness"))
                    smoothness = mat.GetFloat("_Smoothness");

                // If no texture found, try to find matching texture by name
                if (mainTex == null)
                {
                    // Material: PolygonZombie_Texture_01_A -> Texture: PolygonZombie_Texture_01_A
                    if (textureLookup.TryGetValue(matName, out Texture2D matchedTex))
                    {
                        mainTex = matchedTex;
                        texturesAssigned++;
                    }
                }

                // Change to URP Lit
                Undo.RecordObject(mat, "Convert to URP");
                mat.shader = urpLit;

                // Reassign textures to URP slots
                if (mainTex != null)
                {
                    mat.SetTexture("_BaseMap", mainTex);
                    mat.SetTexture("_MainTex", mainTex);
                }

                // Set smoothness (low for skin)
                mat.SetFloat("_Smoothness", smoothness);

                // Set to opaque
                mat.SetFloat("_Surface", 0);
                mat.SetFloat("_Blend", 0);
                mat.renderQueue = -1;

                EditorUtility.SetDirty(mat);
                converted++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Complete",
            $"Converted {converted} materials to URP/Lit.\n" +
            $"Auto-assigned {texturesAssigned} textures by name matching.\n\n" +
            "Zombies should now render correctly!",
            "OK");
    }

    [MenuItem("Project Klyra/Zombies/Find and Assign Textures")]
    public static void FindAndAssignTextures()
    {
        string materialsPath = "Assets/PolygonZombies/Materials";
        string texturesPath = "Assets/PolygonZombies/Textures";

        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { materialsPath });
        int fixed_count = 0;

        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null) continue;

            // Check if Base Map is missing
            Texture baseTex = mat.GetTexture("_BaseMap");
            if (baseTex != null) continue; // Already has texture

            // Try to find matching texture
            string matName = mat.name;

            // Search for textures
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { texturesPath });

            foreach (string texGuid in textureGuids)
            {
                string texPath = AssetDatabase.GUIDToAssetPath(texGuid);
                string texName = Path.GetFileNameWithoutExtension(texPath);

                // Check if texture name matches material name pattern
                if (matName.Contains("01_A") && texName.Contains("01") && !texName.Contains("02") && !texName.Contains("03"))
                {
                    Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                    if (tex != null && !texName.Contains("Blood") && !texName.Contains("_N"))
                    {
                        mat.SetTexture("_BaseMap", tex);
                        EditorUtility.SetDirty(mat);
                        fixed_count++;
                        Debug.Log($"Assigned {texName} to {matName}");
                        break;
                    }
                }
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Complete", $"Fixed {fixed_count} materials", "OK");
    }
}
#endif
