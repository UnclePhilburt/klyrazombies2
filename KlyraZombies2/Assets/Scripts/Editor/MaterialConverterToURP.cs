using UnityEngine;
using UnityEditor;
using System.IO;

namespace ProjectKlyra.Editor
{
    public class MaterialConverterToURP : EditorWindow
    {
        [MenuItem("Project Klyra/Companion/Convert Dog Materials to URP")]
        public static void ConvertDogMaterials()
        {
            string[] materialPaths = Directory.GetFiles(
                "Assets/PolygonDog/Materials",
                "*.mat",
                SearchOption.AllDirectories
            );

            int converted = 0;
            int skipped = 0;

            foreach (string path in materialPaths)
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                // Check if it's using Built-in shader
                if (mat.shader.name == "Standard" ||
                    mat.shader.name == "Standard (Specular setup)" ||
                    mat.shader.name.Contains("Legacy"))
                {
                    // Get the URP/Lit shader
                    Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                    if (urpShader == null)
                    {
                        Debug.LogError("URP/Lit shader not found. Is URP installed?");
                        return;
                    }

                    // Store properties before conversion
                    Texture mainTex = mat.GetTexture("_MainTex");
                    Color color = mat.color;
                    float smoothness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;
                    float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;

                    // Change shader
                    mat.shader = urpShader;

                    // Restore properties (URP uses different property names)
                    if (mainTex != null)
                    {
                        mat.SetTexture("_BaseMap", mainTex);
                    }
                    mat.SetColor("_BaseColor", color);
                    mat.SetFloat("_Smoothness", smoothness);
                    mat.SetFloat("_Metallic", metallic);

                    EditorUtility.SetDirty(mat);
                    converted++;
                    Debug.Log($"Converted: {mat.name}");
                }
                else
                {
                    skipped++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green>Material conversion complete!</color>\nConverted: {converted}, Skipped: {skipped}");
            EditorUtility.DisplayDialog(
                "Conversion Complete",
                $"Successfully converted {converted} materials to URP.\n{skipped} materials were already using compatible shaders.",
                "OK"
            );
        }
    }
}
