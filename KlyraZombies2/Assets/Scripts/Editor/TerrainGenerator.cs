using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class TerrainGenerator : EditorWindow
{
    // Terrain settings
    private Terrain terrain;
    private int resolution = 513;
    private float terrainSize = 1000f;
    private float maxHeight = 200f;

    // Boundary mountains
    private float boundaryHeight = 0.85f;
    private float boundaryWidth = 0.12f;

    // Interior terrain
    private float baseHeight = 0.15f;
    private int numHills = 8;
    private int numMountains = 3;
    private int numFlatAreas = 4;

    // Detail
    private float noiseStrength = 0.08f;
    private float rollingHillStrength = 0.12f;

    // Seed for randomization
    private int seed = 12345;

    [MenuItem("Project Klyra/Level Design/Terrain Generator")]
    public static void ShowWindow()
    {
        GetWindow<TerrainGenerator>("Terrain Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Valley Terrain Generator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);

        GUILayout.Space(10);
        GUILayout.Label("Terrain Size", EditorStyles.boldLabel);
        resolution = EditorGUILayout.IntPopup("Resolution", resolution,
            new string[] { "257", "513", "1025", "2049" },
            new int[] { 257, 513, 1025, 2049 });
        terrainSize = EditorGUILayout.Slider("Size (meters)", terrainSize, 500f, 4000f);
        maxHeight = EditorGUILayout.Slider("Max Height", maxHeight, 100f, 500f);

        GUILayout.Space(10);
        GUILayout.Label("Boundary Mountains", EditorStyles.boldLabel);
        boundaryHeight = EditorGUILayout.Slider("Height", boundaryHeight, 0.5f, 1f);
        boundaryWidth = EditorGUILayout.Slider("Width", boundaryWidth, 0.05f, 0.25f);

        GUILayout.Space(10);
        GUILayout.Label("Interior Features", EditorStyles.boldLabel);
        numMountains = EditorGUILayout.IntSlider("Big Mountains", numMountains, 0, 8);
        numHills = EditorGUILayout.IntSlider("Medium Hills", numHills, 0, 20);
        numFlatAreas = EditorGUILayout.IntSlider("Flat Areas (for building)", numFlatAreas, 0, 10);

        GUILayout.Space(10);
        GUILayout.Label("Terrain Detail", EditorStyles.boldLabel);
        rollingHillStrength = EditorGUILayout.Slider("Rolling Hills", rollingHillStrength, 0f, 0.25f);
        noiseStrength = EditorGUILayout.Slider("Surface Noise", noiseStrength, 0f, 0.15f);

        GUILayout.Space(10);
        seed = EditorGUILayout.IntField("Seed (change for variety)", seed);

        GUILayout.Space(20);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate Terrain", GUILayout.Height(40)))
        {
            if (terrain == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Terrain first!", "OK");
                return;
            }
            GenerateTerrain();
        }
        if (GUILayout.Button("Randomize", GUILayout.Height(40), GUILayout.Width(100)))
        {
            seed = Random.Range(0, 99999);
            if (terrain != null) GenerateTerrain();
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        if (GUILayout.Button("Create New Terrain"))
        {
            CreateNewTerrain();
        }

        GUILayout.Space(20);
        GUILayout.Label("What it generates:", EditorStyles.boldLabel);
        GUILayout.Label(
            "• Mountains around edges (can't escape)\n" +
            "• Big mountains inside to build around\n" +
            "• Rolling hills for cover/variety\n" +
            "• Flat plateaus for bases/towns\n" +
            "• Natural surface detail", EditorStyles.wordWrappedLabel);
    }

    private void CreateNewTerrain()
    {
        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = resolution;
        terrainData.size = new Vector3(terrainSize, maxHeight, terrainSize);

        string path = "Assets/GeneratedTerrainData.asset";
        AssetDatabase.CreateAsset(terrainData, path);

        GameObject terrainGO = Terrain.CreateTerrainGameObject(terrainData);
        terrainGO.name = "Generated Valley Terrain";
        terrain = terrainGO.GetComponent<Terrain>();

        Selection.activeGameObject = terrainGO;
        Debug.Log("Created new terrain. Now click 'Generate Terrain' to sculpt it.");
    }

    private void GenerateTerrain()
    {
        Random.InitState(seed);

        TerrainData data = terrain.terrainData;
        data.heightmapResolution = resolution;
        data.size = new Vector3(terrainSize, maxHeight, terrainSize);

        float[,] heights = new float[resolution, resolution];

        // Generate feature points
        List<TerrainFeature> features = new List<TerrainFeature>();

        // Add interior mountains (big features to build around)
        for (int i = 0; i < numMountains; i++)
        {
            features.Add(new TerrainFeature
            {
                position = GetRandomInteriorPoint(0.25f),
                radius = Random.Range(0.12f, 0.22f),  // Wider mountains
                height = Random.Range(0.35f, 0.6f),
                falloff = Random.Range(0.8f, 1.2f),   // Gentler falloff
                type = FeatureType.Mountain
            });
        }

        // Add medium hills
        for (int i = 0; i < numHills; i++)
        {
            features.Add(new TerrainFeature
            {
                position = GetRandomInteriorPoint(0.2f),
                radius = Random.Range(0.08f, 0.18f),  // Much wider hills
                height = Random.Range(0.1f, 0.25f),   // Lower, gentler
                falloff = Random.Range(0.6f, 1.0f),   // Smoother gaussian
                type = FeatureType.Hill
            });
        }

        // Add flat areas (plateaus for building)
        for (int i = 0; i < numFlatAreas; i++)
        {
            features.Add(new TerrainFeature
            {
                position = GetRandomInteriorPoint(0.25f),
                radius = Random.Range(0.06f, 0.12f),
                height = Random.Range(0.12f, 0.2f),
                falloff = 0.5f, // Sharp falloff for flat top
                type = FeatureType.Flat
            });
        }

        // Noise seeds
        float seedX = Random.Range(0f, 10000f);
        float seedZ = Random.Range(0f, 10000f);

        for (int x = 0; x < resolution; x++)
        {
            for (int z = 0; z < resolution; z++)
            {
                float nx = (float)x / (resolution - 1);
                float nz = (float)z / (resolution - 1);

                // Start with base height
                float height = baseHeight;

                // Add boundary mountains
                height = Mathf.Max(height, GetBoundaryHeight(nx, nz, seedX, seedZ));

                // Add interior features
                foreach (var feature in features)
                {
                    height = ApplyFeature(height, nx, nz, feature);
                }

                // Add rolling hills (large scale noise)
                height += GetRollingHills(nx, nz, seedX, seedZ) * rollingHillStrength;

                // Add surface detail noise
                height += GetDetailNoise(nx, nz, seedX, seedZ) * noiseStrength;

                // Ensure we don't exceed boundary or go negative
                float boundaryMask = GetBoundaryMask(nx, nz);
                height = Mathf.Clamp(height, 0.01f, boundaryMask > 0.5f ? boundaryHeight + 0.1f : 0.95f);

                heights[z, x] = height;
            }
        }

        // Smooth pass to blend features nicely
        heights = SmoothHeights(heights, 1);

        data.SetHeights(0, 0, heights);

        Debug.Log($"Terrain generated with seed {seed}! " +
                 $"{numMountains} mountains, {numHills} hills, {numFlatAreas} flat areas.");
    }

    private Vector2 GetRandomInteriorPoint(float margin)
    {
        // Keep features away from boundaries
        return new Vector2(
            Random.Range(margin + boundaryWidth, 1f - margin - boundaryWidth),
            Random.Range(margin + boundaryWidth, 1f - margin - boundaryWidth)
        );
    }

    private float GetBoundaryHeight(float x, float z, float seedX, float seedZ)
    {
        // Distance to nearest edge
        float distToEdge = Mathf.Min(
            Mathf.Min(x, 1f - x),
            Mathf.Min(z, 1f - z)
        );

        if (distToEdge > boundaryWidth) return 0f;

        // Ramp up toward edge
        float t = 1f - (distToEdge / boundaryWidth);
        t = t * t; // Exponential curve for steeper mountains

        // Add noise to make mountains interesting
        float noise = Mathf.PerlinNoise(
            (x + seedX) * 15f,
            (z + seedZ) * 15f
        ) * 0.15f;

        return (boundaryHeight + noise) * t;
    }

    private float GetBoundaryMask(float x, float z)
    {
        float distToEdge = Mathf.Min(
            Mathf.Min(x, 1f - x),
            Mathf.Min(z, 1f - z)
        );
        return distToEdge < boundaryWidth ? 1f : 0f;
    }

    private float ApplyFeature(float currentHeight, float x, float z, TerrainFeature feature)
    {
        float dist = Vector2.Distance(new Vector2(x, z), feature.position);

        if (dist > feature.radius * 2f) return currentHeight;

        float t = dist / feature.radius;

        switch (feature.type)
        {
            case FeatureType.Mountain:
                // Mountains: rounded top, steeper sides (cosine curve)
                if (t < 1.5f)
                {
                    // Cosine falloff for smooth rounded peak
                    float influence = (Mathf.Cos(Mathf.Clamp01(t / 1.5f) * Mathf.PI) + 1f) * 0.5f;
                    return Mathf.Max(currentHeight, baseHeight + feature.height * influence);
                }
                return currentHeight;

            case FeatureType.Hill:
                // Hills: very smooth, wide, gentle (gaussian-like)
                if (t < 2f)
                {
                    // Gaussian falloff for natural rounded hills
                    float gaussian = Mathf.Exp(-t * t * feature.falloff);
                    return Mathf.Max(currentHeight, baseHeight + feature.height * gaussian);
                }
                return currentHeight;

            case FeatureType.Flat:
                // Flat top plateau
                if (t < 0.5f)
                {
                    // Flat center
                    return Mathf.Max(currentHeight, feature.height);
                }
                else if (t < 1.2f)
                {
                    // Smooth ramp down at edges using cosine
                    float rampT = (t - 0.5f) / 0.7f;
                    float smoothT = (1f - Mathf.Cos(rampT * Mathf.PI)) * 0.5f;
                    float rampHeight = Mathf.Lerp(feature.height, baseHeight, smoothT);
                    return Mathf.Max(currentHeight, rampHeight);
                }
                return currentHeight;

            default:
                return currentHeight;
        }
    }

    private float GetRollingHills(float x, float z, float seedX, float seedZ)
    {
        // Large scale gentle hills
        float scale = 3f;
        float noise = 0f;

        noise += Mathf.PerlinNoise((x + seedX) * scale, (z + seedZ) * scale) * 1f;
        noise += Mathf.PerlinNoise((x + seedX) * scale * 2f, (z + seedZ) * scale * 2f) * 0.5f;

        return (noise / 1.5f) - 0.5f; // Center around 0
    }

    private float GetDetailNoise(float x, float z, float seedX, float seedZ)
    {
        // Small scale surface variation
        float scale = 20f;
        return (Mathf.PerlinNoise((x + seedX + 100f) * scale, (z + seedZ + 100f) * scale) - 0.5f);
    }

    private float[,] SmoothHeights(float[,] heights, int iterations)
    {
        int size = heights.GetLength(0);
        float[,] smoothed = new float[size, size];

        for (int iter = 0; iter < iterations; iter++)
        {
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    float sum = heights[z, x];
                    int count = 1;

                    // Sample neighbors
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            int nx = Mathf.Clamp(x + dx, 0, size - 1);
                            int nz = Mathf.Clamp(z + dz, 0, size - 1);
                            if (nx != x || nz != z)
                            {
                                sum += heights[nz, nx];
                                count++;
                            }
                        }
                    }

                    smoothed[z, x] = sum / count;
                }
            }

            // Copy back for next iteration
            if (iter < iterations - 1)
            {
                System.Array.Copy(smoothed, heights, size * size);
            }
        }

        return smoothed;
    }

    private enum FeatureType { Mountain, Hill, Flat }

    private struct TerrainFeature
    {
        public Vector2 position;
        public float radius;
        public float height;
        public float falloff;
        public FeatureType type;
    }
}
