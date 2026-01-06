using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Opsive.UltimateCharacterController.SurfaceSystem;

public class SurfaceTextureMapper : EditorWindow
{
    private SurfaceType grassSurface;
    private SurfaceType dirtSurface;
    private SurfaceType woodSurface;
    private SurfaceType metalSurface;
    private SurfaceType waterSurface;
    private SurfaceType sandSurface;
    private SurfaceType genericSurface;

    private Vector2 scrollPos;
    private List<TextureMapping> mappings = new List<TextureMapping>();
    private bool hasScanned = false;

    private class TextureMapping
    {
        public Texture2D texture;
        public string category;
        public SurfaceType surfaceType;
        public bool include = true;
    }

    [MenuItem("Tools/Surface Texture Mapper")]
    public static void ShowWindow()
    {
        var window = GetWindow<SurfaceTextureMapper>("Surface Texture Mapper");
        window.minSize = new Vector2(500, 400);
    }

    private void OnEnable()
    {
        // Auto-load Opsive demo surface types
        LoadDefaultSurfaceTypes();
    }

    private void LoadDefaultSurfaceTypes()
    {
        string basePath = "Assets/Samples/Opsive Ultimate Character Controller/3.3.3/Demo/SurfaceSystem/SurfaceTypes/";

        if (grassSurface == null)
            grassSurface = AssetDatabase.LoadAssetAtPath<SurfaceType>(basePath + "Grass.asset");
        if (dirtSurface == null)
            dirtSurface = AssetDatabase.LoadAssetAtPath<SurfaceType>(basePath + "Dirt.asset");
        if (woodSurface == null)
            woodSurface = AssetDatabase.LoadAssetAtPath<SurfaceType>(basePath + "Wood.asset");
        if (metalSurface == null)
            metalSurface = AssetDatabase.LoadAssetAtPath<SurfaceType>(basePath + "Metal.asset");
        if (waterSurface == null)
            waterSurface = AssetDatabase.LoadAssetAtPath<SurfaceType>(basePath + "Water.asset");
        if (sandSurface == null)
            sandSurface = AssetDatabase.LoadAssetAtPath<SurfaceType>(basePath + "Sand.asset");
        if (genericSurface == null)
            genericSurface = AssetDatabase.LoadAssetAtPath<SurfaceType>(basePath + "Generic.asset");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Surface Texture Auto-Mapper", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Surface Types:", EditorStyles.boldLabel);

        grassSurface = (SurfaceType)EditorGUILayout.ObjectField("Grass", grassSurface, typeof(SurfaceType), false);
        dirtSurface = (SurfaceType)EditorGUILayout.ObjectField("Dirt", dirtSurface, typeof(SurfaceType), false);
        woodSurface = (SurfaceType)EditorGUILayout.ObjectField("Wood", woodSurface, typeof(SurfaceType), false);
        metalSurface = (SurfaceType)EditorGUILayout.ObjectField("Metal", metalSurface, typeof(SurfaceType), false);
        waterSurface = (SurfaceType)EditorGUILayout.ObjectField("Water", waterSurface, typeof(SurfaceType), false);
        sandSurface = (SurfaceType)EditorGUILayout.ObjectField("Sand", sandSurface, typeof(SurfaceType), false);
        genericSurface = (SurfaceType)EditorGUILayout.ObjectField("Generic (fallback)", genericSurface, typeof(SurfaceType), false);

        EditorGUILayout.Space();

        if (GUILayout.Button("Scan Synty Textures", GUILayout.Height(30)))
        {
            ScanTextures();
        }

        if (hasScanned && mappings.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Found {mappings.Count} ground textures:", EditorStyles.boldLabel);

            float scrollHeight = Mathf.Max(100, position.height - 350);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(scrollHeight));

            foreach (var mapping in mappings)
            {
                EditorGUILayout.BeginHorizontal();
                mapping.include = EditorGUILayout.Toggle(mapping.include, GUILayout.Width(20));
                EditorGUILayout.ObjectField(mapping.texture, typeof(Texture2D), false, GUILayout.Width(180));
                EditorGUILayout.LabelField(mapping.category, GUILayout.Width(60));
                mapping.surfaceType = (SurfaceType)EditorGUILayout.ObjectField(mapping.surfaceType, typeof(SurfaceType), false);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        GUILayout.FlexibleSpace();

        if (hasScanned && mappings.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All", GUILayout.Height(25)))
            {
                foreach (var m in mappings) m.include = true;
            }
            if (GUILayout.Button("Select None", GUILayout.Height(25)))
            {
                foreach (var m in mappings) m.include = false;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("APPLY TO SURFACE MANAGER", GUILayout.Height(45)))
            {
                ApplyToSurfaceManager();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);
        }
    }

    private void ScanTextures()
    {
        mappings.Clear();

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Synty" });
        Debug.Log($"Found {guids.Length} total textures in Synty folder");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();

            // Skip non-diffuse textures
            if (fileName.Contains("normal") || fileName.Contains("_n.") ||
                fileName.EndsWith("_n") || fileName.Contains("metallic") ||
                fileName.Contains("_m.") || fileName.EndsWith("_m") ||
                fileName.Contains("mask") || fileName.Contains("occlusion") ||
                fileName.Contains("ao") || fileName.Contains("roughness") ||
                fileName.Contains("height") || fileName.Contains("emission") ||
                fileName.Contains("glow") || fileName.Contains("refraction") ||
                fileName.Contains("lensdirt") || fileName.Contains("_ao"))
            {
                continue;
            }

            string category = CategorizeTexture(fileName);
            if (category == null) continue;

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) continue;

            var mapping = new TextureMapping
            {
                texture = texture,
                category = category,
                surfaceType = GetSurfaceTypeForCategory(category)
            };

            mappings.Add(mapping);
        }

        mappings = mappings.OrderBy(m => m.category).ThenBy(m => m.texture.name).ToList();
        hasScanned = true;

        Debug.Log($"Categorized {mappings.Count} ground textures");
    }

    private string CategorizeTexture(string fileName)
    {
        if (fileName.Contains("grass") || fileName.Contains("clover") ||
            fileName.Contains("groundcover") || fileName.Contains("lawn") ||
            fileName.Contains("moss") || fileName.Contains("vegetation"))
            return "Grass";

        if (fileName.Contains("dirt") || fileName.Contains("mud") ||
            fileName.Contains("soil") || fileName.Contains("ground") ||
            fileName.Contains("earth"))
            return "Dirt";

        if (fileName.Contains("sand") || fileName.Contains("beach") ||
            fileName.Contains("desert"))
            return "Sand";

        if (fileName.Contains("wood") || fileName.Contains("plank") ||
            fileName.Contains("timber") || fileName.Contains("lumber"))
            return "Wood";

        if (fileName.Contains("metal") || fileName.Contains("steel") ||
            fileName.Contains("iron") || fileName.Contains("aluminum") ||
            fileName.Contains("rust"))
            return "Metal";

        if (fileName.Contains("water") && !fileName.Contains("fall"))
            return "Water";

        if (fileName.Contains("concrete") || fileName.Contains("stone") ||
            fileName.Contains("brick") || fileName.Contains("tile") ||
            fileName.Contains("road") || fileName.Contains("asphalt") ||
            fileName.Contains("pavement") || fileName.Contains("cobble") ||
            fileName.Contains("gravel") || fileName.Contains("pebble") ||
            fileName.Contains("rock") || fileName.Contains("ruin") ||
            fileName.Contains("footpath") || fileName.Contains("path"))
            return "Generic";

        return null;
    }

    private SurfaceType GetSurfaceTypeForCategory(string category)
    {
        switch (category)
        {
            case "Grass": return grassSurface;
            case "Dirt": return dirtSurface;
            case "Sand": return sandSurface;
            case "Wood": return woodSurface;
            case "Metal": return metalSurface;
            case "Water": return waterSurface;
            case "Generic": return genericSurface;
            default: return genericSurface;
        }
    }

    private void ApplyToSurfaceManager()
    {
        // Find or create SurfaceManager
        SurfaceManager surfaceManager = FindObjectOfType<SurfaceManager>();

        if (surfaceManager == null)
        {
            GameObject go = new GameObject("SurfaceManager");
            surfaceManager = go.AddComponent<SurfaceManager>();
            Undo.RegisterCreatedObjectUndo(go, "Create SurfaceManager");
            Debug.Log("Created new SurfaceManager in scene");
        }

        var includedMappings = mappings.Where(m => m.include && m.surfaceType != null).ToList();

        if (includedMappings.Count == 0)
        {
            Debug.LogError("No textures selected or no surface types assigned!");
            return;
        }

        // Group by surface type
        var groupedBySurface = includedMappings
            .GroupBy(m => m.surfaceType)
            .ToList();

        // Build ObjectSurface array
        var objectSurfaces = new ObjectSurface[groupedBySurface.Count];

        for (int i = 0; i < groupedBySurface.Count; i++)
        {
            var group = groupedBySurface[i];
            var surfaceType = group.Key;
            var textures = group.ToList();

            // Create UVTexture array for this surface type
            var uvTextures = new UVTexture[textures.Count];
            for (int j = 0; j < textures.Count; j++)
            {
                uvTextures[j] = new UVTexture
                {
                    Texture = textures[j].texture,
                    UV = new Rect(0, 0, 1, 1) // Default UV
                };
            }

            objectSurfaces[i] = new ObjectSurface
            {
                SurfaceType = surfaceType,
                UVTextures = uvTextures
            };
        }

        // Apply via SerializedObject
        SerializedObject so = new SerializedObject(surfaceManager);

        // Set ObjectSurfaces
        SerializedProperty objectSurfacesProp = so.FindProperty("m_ObjectSurfaces");
        objectSurfacesProp.arraySize = objectSurfaces.Length;

        for (int i = 0; i < objectSurfaces.Length; i++)
        {
            SerializedProperty element = objectSurfacesProp.GetArrayElementAtIndex(i);

            // Set SurfaceType
            SerializedProperty surfaceTypeProp = element.FindPropertyRelative("m_SurfaceType");
            surfaceTypeProp.objectReferenceValue = objectSurfaces[i].SurfaceType;

            // Set UVTextures array
            SerializedProperty uvTexturesProp = element.FindPropertyRelative("m_UVTextures");
            uvTexturesProp.arraySize = objectSurfaces[i].UVTextures.Length;

            for (int j = 0; j < objectSurfaces[i].UVTextures.Length; j++)
            {
                SerializedProperty uvElement = uvTexturesProp.GetArrayElementAtIndex(j);

                SerializedProperty texProp = uvElement.FindPropertyRelative("m_Texture");
                texProp.objectReferenceValue = objectSurfaces[i].UVTextures[j].Texture;

                SerializedProperty uvProp = uvElement.FindPropertyRelative("m_UV");
                uvProp.rectValue = objectSurfaces[i].UVTextures[j].UV;
            }
        }

        // Set fallback
        SerializedProperty fallbackProp = so.FindProperty("m_FallbackSurfaceType");
        if (fallbackProp != null && genericSurface != null)
        {
            fallbackProp.objectReferenceValue = genericSurface;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(surfaceManager);

        Debug.Log($"SUCCESS! Applied {includedMappings.Count} textures across {groupedBySurface.Count} surface types to SurfaceManager");
        EditorUtility.DisplayDialog("Success!",
            $"Applied {includedMappings.Count} textures to SurfaceManager!\n\n" +
            $"Surface types: {groupedBySurface.Count}\n" +
            $"Don't forget to save your scene.", "OK");
    }
}
