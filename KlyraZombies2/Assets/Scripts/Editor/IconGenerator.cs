using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Editor tool to generate item icons from 3D prefabs.
/// Renders prefabs to textures that can be used as inventory icons.
/// Works with URP (Universal Render Pipeline).
/// </summary>
public class IconGenerator : EditorWindow
{
    [MenuItem("Project Klyra/Inventory/Icon Generator")]
    public static void ShowWindow()
    {
        var window = GetWindow<IconGenerator>("Icon Generator");
        window.minSize = new Vector2(400, 600);
    }

    private List<GameObject> m_Prefabs = new List<GameObject>();
    private Vector2 m_ScrollPosition;
    private int m_IconSize = 256;
    private Color m_BackgroundColor = new Color(0.15f, 0.15f, 0.15f, 0f); // Transparent dark
    private string m_OutputFolder = "Assets/Data/Icons";

    // Camera settings
    private bool m_UseOrthographic = true;
    private float m_OrthographicPadding = 1.2f; // Multiplier for ortho size
    private Vector3 m_ObjectRotation = new Vector3(0f, -135f, 0f); // Rotate object, not camera
    private float m_CameraPitch = 25f; // Camera looks down at this angle

    // Lighting
    private float m_AmbientIntensity = 0.4f;
    private float m_KeyLightIntensity = 1.2f;
    private float m_FillLightIntensity = 0.5f;
    private float m_RimLightIntensity = 0.8f;

    // Preview
    private Texture2D m_PreviewTexture;
    private GameObject m_PreviewPrefab;
    private int m_PreviewSize = 256;

    // Interactive rotation
    private bool m_IsDragging = false;
    private Vector2 m_LastMousePos;
    private float m_RotationSensitivity = 0.5f;
    private float m_ZoomSensitivity = 0.1f;

    // Presets
    private enum IconPreset { Custom, SmallItem, Weapon, LargeItem }
    private IconPreset m_CurrentPreset = IconPreset.SmallItem;

    private void OnEnable()
    {
        ApplyPreset(IconPreset.SmallItem);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Icon Generator", EditorStyles.boldLabel);

        EditorGUILayout.Space(10);

        // === STEP 1: Add Prefabs ===
        EditorGUILayout.LabelField("STEP 1: Add Prefabs", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox($"Currently loaded: {m_Prefabs.Count} prefabs", MessageType.Info);

        // Quick add buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("SM_Item_*", GUILayout.Height(30)))
        {
            FindAllItemPrefabs();
        }
        if (GUILayout.Button("SM_Wep_*", GUILayout.Height(30)))
        {
            FindAllWeaponPrefabs();
        }
        if (GUILayout.Button("SM_Prop_* (Office)", GUILayout.Height(30)))
        {
            FindOfficePropPrefabs();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Selected (from Project window)"))
        {
            AddSelectedPrefabs();
        }
        if (GUILayout.Button("Clear All"))
        {
            m_Prefabs.Clear();
            Debug.Log("Cleared all prefabs");
        }
        EditorGUILayout.EndHorizontal();

        // Drop area
        Rect dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Or Drag & Drop Prefabs Here");
        HandleDragDrop(dropArea);

        // Prefab list
        if (m_Prefabs.Count > 0)
        {
            EditorGUILayout.LabelField($"Prefabs in list ({m_Prefabs.Count}):");
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition, GUILayout.Height(100));
            for (int i = m_Prefabs.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{i + 1}. {m_Prefabs[i].name}");
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    m_Prefabs.RemoveAt(i);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.Space(15);

        // === STEP 2: Settings ===
        EditorGUILayout.LabelField("STEP 2: Settings", EditorStyles.boldLabel);
        m_IconSize = EditorGUILayout.IntPopup("Icon Size", m_IconSize,
            new[] { "64", "128", "256", "512" }, new[] { 64, 128, 256, 512 });

        EditorGUILayout.BeginHorizontal();
        m_OutputFolder = EditorGUILayout.TextField("Output Folder", m_OutputFolder);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string folder = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
            if (!string.IsNullOrEmpty(folder) && folder.StartsWith(Application.dataPath))
            {
                m_OutputFolder = "Assets" + folder.Substring(Application.dataPath.Length);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(15);

        // === STEP 3: Generate ===
        EditorGUILayout.LabelField("STEP 3: Generate Icons", EditorStyles.boldLabel);

        if (m_Prefabs.Count == 0)
        {
            EditorGUILayout.HelpBox("Add prefabs first using the buttons above!", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox($"Ready to generate {m_Prefabs.Count} icons to: {m_OutputFolder}", MessageType.Info);

            if (GUILayout.Button($"GENERATE {m_Prefabs.Count} ICONS", GUILayout.Height(40)))
            {
                GenerateIcons();
            }
        }
    }

    private void FindAllItemPrefabs()
    {
        m_Prefabs.Clear();
        string[] guids = AssetDatabase.FindAssets("SM_Item_ t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && !m_Prefabs.Contains(prefab))
            {
                m_Prefabs.Add(prefab);
            }
        }
        Debug.Log($"Found {m_Prefabs.Count} SM_Item_* prefabs in project");
    }

    private void FindAllWeaponPrefabs()
    {
        m_Prefabs.Clear();
        string[] guids = AssetDatabase.FindAssets("SM_Wep_ t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && !m_Prefabs.Contains(prefab))
            {
                m_Prefabs.Add(prefab);
            }
        }
        Debug.Log($"Found {m_Prefabs.Count} SM_Wep_* prefabs in project");
    }

    private void FindOfficePropPrefabs()
    {
        m_Prefabs.Clear();

        // Office prop prefabs that are lootable items - matching item names from OfficeItemGenerator
        string[] searchTerms = new string[]
        {
            // Office Supplies
            "SM_Prop_Pen_", "SM_Prop_Pencil", "SM_Prop_Scissors", "SM_Prop_Stapler",
            "SM_Prop_JellyStapler", "SM_Prop_Tape", "SM_Prop_PaperClip", "SM_Prop_RubberBand",

            // Documents
            "SM_Prop_Folder_Manila", "SM_Prop_Folder_PVC", "SM_Prop_Book_", "SM_Prop_NotePad",
            "SM_Prop_Note_", "SM_Prop_Clipboard", "SM_Prop_Paper_", "SM_Prop_Magazine",

            // Electronics
            "SM_Prop_Calculator", "SM_Prop_Laptop", "SM_Prop_Headphones", "SM_Prop_Flash_Drive",
            "SM_Prop_Cellphone", "SM_Prop_Phone_", "SM_Prop_SD_Card", "SM_Prop_HDD_USB",
            "SM_Prop_GraphicsTablet",

            // Valuables
            "SM_Prop_Briefcase", "SM_Prop_Trophy", "SM_Prop_ID_Card", "SM_Prop_Watch",
            "SM_Prop_Keys", "SM_Prop_Keypad",

            // Food & Drink
            "SM_Prop_Donut", "SM_Prop_Sandwich", "SM_Prop_Snack_", "SM_Prop_Can_Soda",
            "SM_Prop_Cup_", "SM_Prop_Bottle", "SM_Prop_Coffee", "SM_Prop_Alcohol",

            // Medical
            "SM_Prop_Pills", "SM_Prop_SprayBottle",

            // Misc
            "SM_Prop_Lighter", "SM_Prop_Matches", "SM_Prop_Cigarette",

            // Emergency/Valuable
            "SM_Prop_Emergency_Axe", "SM_Prop_Fire_Extinguisher"
        };

        HashSet<GameObject> addedPrefabs = new HashSet<GameObject>();

        foreach (string term in searchTerms)
        {
            string[] guids = AssetDatabase.FindAssets($"{term} t:Prefab");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                // Only include PolygonOffice prefabs
                if (!path.Contains("PolygonOffice")) continue;

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && !addedPrefabs.Contains(prefab))
                {
                    m_Prefabs.Add(prefab);
                    addedPrefabs.Add(prefab);
                }
            }
        }

        // Sort alphabetically for easier review
        m_Prefabs.Sort((a, b) => a.name.CompareTo(b.name));

        Debug.Log($"Found {m_Prefabs.Count} office prop prefabs for icon generation");
    }

    private void ApplyPreset(IconPreset preset)
    {
        m_CurrentPreset = preset;

        switch (preset)
        {
            case IconPreset.SmallItem:
                m_ObjectRotation = new Vector3(15f, -135f, 0f);
                m_CameraPitch = 20f;
                m_OrthographicPadding = 1.3f;
                m_AmbientIntensity = 0.5f;
                m_KeyLightIntensity = 1.0f;
                m_FillLightIntensity = 0.4f;
                m_RimLightIntensity = 0.6f;
                break;

            case IconPreset.Weapon:
                m_ObjectRotation = new Vector3(0f, -45f, -15f); // Diagonal orientation for guns
                m_CameraPitch = 15f;
                m_OrthographicPadding = 1.4f;
                m_AmbientIntensity = 0.4f;
                m_KeyLightIntensity = 1.2f;
                m_FillLightIntensity = 0.5f;
                m_RimLightIntensity = 0.8f;
                break;

            case IconPreset.LargeItem:
                m_ObjectRotation = new Vector3(10f, -135f, 0f);
                m_CameraPitch = 30f;
                m_OrthographicPadding = 1.5f;
                m_AmbientIntensity = 0.5f;
                m_KeyLightIntensity = 1.0f;
                m_FillLightIntensity = 0.5f;
                m_RimLightIntensity = 0.5f;
                break;
        }

        if (m_PreviewPrefab != null)
        {
            GeneratePreview(m_PreviewPrefab);
        }
    }

    private void HandleDragDrop(Rect dropArea)
    {
        Event evt = Event.current;
        if (!dropArea.Contains(evt.mousePosition)) return;

        if (evt.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            evt.Use();
        }
        else if (evt.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            foreach (Object obj in DragAndDrop.objectReferences)
            {
                if (obj is GameObject go && PrefabUtility.IsPartOfPrefabAsset(go))
                {
                    if (!m_Prefabs.Contains(go))
                    {
                        m_Prefabs.Add(go);
                    }
                }
            }
            evt.Use();
        }
    }

    private void AddSelectedPrefabs()
    {
        int added = 0;

        // Get all selected objects (works for Project window selections)
        foreach (Object obj in Selection.objects)
        {
            GameObject go = obj as GameObject;
            if (go == null) continue;

            // Check if it's a prefab asset (not a scene instance)
            if (PrefabUtility.IsPartOfPrefabAsset(go) || AssetDatabase.Contains(go))
            {
                // Get the prefab root
                GameObject prefabRoot = go.transform.root.gameObject;

                if (!m_Prefabs.Contains(prefabRoot))
                {
                    m_Prefabs.Add(prefabRoot);
                    added++;
                }
            }
        }

        // Also try to add from asset paths (for when folders are selected)
        foreach (Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) continue;

            // If it's a folder, find prefabs in it
            if (AssetDatabase.IsValidFolder(path))
            {
                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
                foreach (string guid in guids)
                {
                    string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab != null && !m_Prefabs.Contains(prefab))
                    {
                        m_Prefabs.Add(prefab);
                        added++;
                    }
                }
            }
            else if (path.EndsWith(".prefab"))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && !m_Prefabs.Contains(prefab))
                {
                    m_Prefabs.Add(prefab);
                    added++;
                }
            }
        }

        if (added > 0)
        {
            Debug.Log($"Added {added} prefabs");
        }
        else
        {
            Debug.LogWarning("No prefabs found in selection. Select prefabs in the Project window.");
        }
    }

    private void AddPrefabsFromFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath)) return;

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && !m_Prefabs.Contains(prefab))
            {
                m_Prefabs.Add(prefab);
            }
        }
        Debug.Log($"Added {guids.Length} prefabs from {folderPath}");
    }

    private void HandlePreviewInput(Rect previewRect)
    {
        Event evt = Event.current;
        int controlID = GUIUtility.GetControlID(FocusType.Passive);

        switch (evt.GetTypeForControl(controlID))
        {
            case EventType.MouseDown:
                if (previewRect.Contains(evt.mousePosition) && evt.button == 0)
                {
                    m_IsDragging = true;
                    m_LastMousePos = evt.mousePosition;
                    GUIUtility.hotControl = controlID;
                    evt.Use();
                }
                break;

            case EventType.MouseUp:
                if (m_IsDragging && evt.button == 0)
                {
                    m_IsDragging = false;
                    GUIUtility.hotControl = 0;
                    evt.Use();
                }
                break;

            case EventType.MouseDrag:
                if (m_IsDragging)
                {
                    Vector2 delta = evt.mousePosition - m_LastMousePos;
                    m_LastMousePos = evt.mousePosition;

                    // Left-right drag rotates Y axis
                    // Up-down drag rotates X axis
                    if (evt.shift)
                    {
                        // Shift + drag for Z rotation (roll)
                        m_ObjectRotation.z += delta.x * m_RotationSensitivity;
                    }
                    else
                    {
                        m_ObjectRotation.y += delta.x * m_RotationSensitivity;
                        m_ObjectRotation.x += delta.y * m_RotationSensitivity;
                    }

                    // Clamp X rotation to prevent flipping
                    m_ObjectRotation.x = Mathf.Clamp(m_ObjectRotation.x, -90f, 90f);

                    if (m_PreviewPrefab != null)
                    {
                        GeneratePreview(m_PreviewPrefab);
                    }

                    evt.Use();
                    Repaint();
                }
                break;

            case EventType.ScrollWheel:
                if (previewRect.Contains(evt.mousePosition))
                {
                    // Scroll to adjust padding (zoom)
                    m_OrthographicPadding += evt.delta.y * m_ZoomSensitivity;
                    m_OrthographicPadding = Mathf.Clamp(m_OrthographicPadding, 1.0f, 3.0f);

                    if (m_PreviewPrefab != null)
                    {
                        GeneratePreview(m_PreviewPrefab);
                    }

                    evt.Use();
                    Repaint();
                }
                break;
        }

        // Show cursor hint when hovering
        if (previewRect.Contains(evt.mousePosition))
        {
            EditorGUIUtility.AddCursorRect(previewRect, MouseCursor.Pan);
        }
    }

    private void DrawCheckerboard(Rect rect)
    {
        // Draw a checkerboard pattern to show transparency
        int gridSize = 10;
        Color light = new Color(0.4f, 0.4f, 0.4f);
        Color dark = new Color(0.3f, 0.3f, 0.3f);

        for (int x = 0; x < rect.width; x += gridSize)
        {
            for (int y = 0; y < rect.height; y += gridSize)
            {
                bool isLight = ((x / gridSize) + (y / gridSize)) % 2 == 0;
                Rect cell = new Rect(rect.x + x, rect.y + y,
                    Mathf.Min(gridSize, rect.width - x),
                    Mathf.Min(gridSize, rect.height - y));
                EditorGUI.DrawRect(cell, isLight ? light : dark);
            }
        }
    }

    private void GeneratePreview(GameObject prefab)
    {
        if (prefab == null) return;

        if (m_PreviewTexture != null)
        {
            DestroyImmediate(m_PreviewTexture);
        }

        m_PreviewPrefab = prefab;
        m_PreviewTexture = RenderPrefabToTexture(prefab, m_PreviewSize);
        Repaint();
    }

    private void GenerateIcons()
    {
        if (!Directory.Exists(m_OutputFolder))
        {
            Directory.CreateDirectory(m_OutputFolder);
        }

        int count = 0;
        foreach (GameObject prefab in m_Prefabs)
        {
            if (prefab == null) continue;

            EditorUtility.DisplayProgressBar("Generating Icons", prefab.name, (float)count / m_Prefabs.Count);

            Texture2D icon = RenderPrefabToTexture(prefab, m_IconSize);
            if (icon != null)
            {
                // Use clean name without SM_Item_ prefix
                string cleanName = prefab.name;
                if (cleanName.StartsWith("SM_Item_"))
                    cleanName = cleanName.Substring(8);
                else if (cleanName.StartsWith("SM_Wep_"))
                    cleanName = cleanName.Substring(7);
                else if (cleanName.StartsWith("SM_Prop_"))
                    cleanName = cleanName.Substring(8);

                string filename = cleanName + "_Icon.png";
                string filepath = Path.Combine(m_OutputFolder, filename);

                byte[] pngData = icon.EncodeToPNG();
                File.WriteAllBytes(filepath, pngData);

                DestroyImmediate(icon);
                count++;
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();

        // Set import settings for all generated icons
        SetIconImportSettings();

        Debug.Log($"Generated {count} icons in {m_OutputFolder}");
        EditorUtility.RevealInFinder(m_OutputFolder);
    }

    private void SetIconImportSettings()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { m_OutputFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
        }
    }

    private Texture2D RenderPrefabToTexture(GameObject prefab, int size)
    {
        // Use Unity's AssetPreview which handles URP correctly
        // First ensure preview is loaded
        AssetPreview.SetPreviewTextureCacheSize(100);

        Texture2D assetPreview = null;

        // Try to get asset preview (may need multiple attempts as Unity generates it async)
        for (int i = 0; i < 10; i++)
        {
            assetPreview = AssetPreview.GetAssetPreview(prefab);
            if (assetPreview != null) break;

            // Force preview generation
            AssetPreview.GetAssetPreview(prefab);
            System.Threading.Thread.Sleep(50);
        }

        if (assetPreview == null)
        {
            // Use the mini thumbnail as fallback
            assetPreview = AssetPreview.GetMiniThumbnail(prefab);
        }

        if (assetPreview == null)
        {
            return CreatePlaceholderTexture(size, prefab.name);
        }

        // Create a copy at the desired size (AssetPreview is usually 128x128)
        Texture2D result = new Texture2D(size, size, TextureFormat.ARGB32, false);

        // Scale the preview to our size
        RenderTexture rt = RenderTexture.GetTemporary(size, size);
        RenderTexture.active = rt;

        // Clear with background
        GL.Clear(true, true, m_BackgroundColor);

        // Blit the preview
        Graphics.Blit(assetPreview, rt);

        result.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }

    private Texture2D CreatePlaceholderTexture(int size, string name)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        Color bgColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        Color[] pixels = new Color[size * size];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = bgColor;
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private void DisableNonVisualComponents(GameObject obj)
    {
        // Disable colliders, rigidbodies, scripts etc that might interfere
        foreach (var collider in obj.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }
        foreach (var rb in obj.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
        }
        foreach (var behaviour in obj.GetComponentsInChildren<MonoBehaviour>())
        {
            behaviour.enabled = false;
        }
    }

    private Bounds CalculateBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshFilter>();

        if (renderers.Length == 0 && meshFilters.Length == 0)
        {
            return new Bounds(obj.transform.position, Vector3.one * 0.1f);
        }

        Bounds bounds = new Bounds();
        bool boundsInitialized = false;

        foreach (Renderer renderer in renderers)
        {
            // Skip particle systems and other non-mesh renderers for bounds
            if (renderer is ParticleSystemRenderer) continue;

            if (!boundsInitialized)
            {
                bounds = renderer.bounds;
                boundsInitialized = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        // Fallback to mesh bounds if no valid renderers
        if (!boundsInitialized)
        {
            foreach (MeshFilter mf in meshFilters)
            {
                if (mf.sharedMesh != null)
                {
                    Bounds meshBounds = mf.sharedMesh.bounds;
                    // Transform bounds to world space
                    Vector3 center = mf.transform.TransformPoint(meshBounds.center);
                    Vector3 size = Vector3.Scale(meshBounds.size, mf.transform.lossyScale);

                    if (!boundsInitialized)
                    {
                        bounds = new Bounds(center, size);
                        boundsInitialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(new Bounds(center, size));
                    }
                }
            }
        }

        // Ensure minimum size
        if (bounds.size.magnitude < 0.001f)
        {
            bounds = new Bounds(obj.transform.position, Vector3.one * 0.1f);
        }

        return bounds;
    }

    private void OnDestroy()
    {
        if (m_PreviewTexture != null)
        {
            DestroyImmediate(m_PreviewTexture);
        }
    }
}
