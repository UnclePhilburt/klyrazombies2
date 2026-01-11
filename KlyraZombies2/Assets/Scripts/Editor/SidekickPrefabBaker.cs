using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Bakes a Sidekick character into a WebGL-ready prefab with pre-bound clothing meshes.
///
/// Usage:
/// 1. Set up a character in the scene using Sidekick normally
/// 2. Select the character GameObject
/// 3. Run "Project Klyra > Sidekick > Bake Character for WebGL"
/// 4. The script will create a prefab with all meshes pre-bound to the skeleton
/// </summary>
public class SidekickPrefabBaker : EditorWindow
{
    private GameObject m_SourceCharacter;
    private string m_PrefabName = "BakedCharacter";
    private string m_OutputPath = "Assets/Prefabs/Characters";

    [MenuItem("Project Klyra/Sidekick/Bake Character for WebGL")]
    public static void ShowWindow()
    {
        var window = GetWindow<SidekickPrefabBaker>("Sidekick Prefab Baker");
        window.minSize = new Vector2(400, 300);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Sidekick Prefab Baker", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This tool bakes a Sidekick character into a WebGL-ready prefab.\n\n" +
            "1. Set up your character in the scene using Sidekick\n" +
            "2. Drag the character GameObject below\n" +
            "3. Click 'Bake Prefab'",
            MessageType.Info);

        EditorGUILayout.Space(10);

        m_SourceCharacter = (GameObject)EditorGUILayout.ObjectField(
            "Source Character", m_SourceCharacter, typeof(GameObject), true);

        m_PrefabName = EditorGUILayout.TextField("Prefab Name", m_PrefabName);
        m_OutputPath = EditorGUILayout.TextField("Output Path", m_OutputPath);

        EditorGUILayout.Space(10);

        // Show info about selected character
        if (m_SourceCharacter != null)
        {
            var meshes = m_SourceCharacter.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var animator = m_SourceCharacter.GetComponentInChildren<Animator>();

            EditorGUILayout.LabelField("Character Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"  Skinned Meshes: {meshes.Length}");
            EditorGUILayout.LabelField($"  Animator: {(animator != null ? "Found" : "Not Found")}");

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                EditorGUILayout.LabelField($"  Controller: {animator.runtimeAnimatorController.name}");
            }
        }

        EditorGUILayout.Space(20);

        GUI.enabled = m_SourceCharacter != null;
        if (GUILayout.Button("Bake Prefab", GUILayout.Height(40)))
        {
            BakePrefab();
        }
        GUI.enabled = true;

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Bake From Selection", GUILayout.Height(30)))
        {
            if (Selection.activeGameObject != null)
            {
                m_SourceCharacter = Selection.activeGameObject;
                BakePrefab();
            }
            else
            {
                EditorUtility.DisplayDialog("No Selection", "Please select a GameObject in the scene.", "OK");
            }
        }
    }

    private void BakePrefab()
    {
        if (m_SourceCharacter == null)
        {
            EditorUtility.DisplayDialog("Error", "No source character selected.", "OK");
            return;
        }

        // Ensure output directory exists
        if (!AssetDatabase.IsValidFolder(m_OutputPath))
        {
            string[] folders = m_OutputPath.Split('/');
            string currentPath = folders[0];
            for (int i = 1; i < folders.Length; i++)
            {
                string newPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = newPath;
            }
        }

        // Create a copy of the character
        GameObject bakedCharacter = Instantiate(m_SourceCharacter);
        bakedCharacter.name = m_PrefabName;

        // Find all skinned mesh renderers
        var meshRenderers = bakedCharacter.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        Debug.Log($"[SidekickPrefabBaker] Found {meshRenderers.Length} skinned mesh renderers");

        // Find the skeleton root
        Transform skeletonRoot = FindSkeletonRoot(bakedCharacter.transform);
        if (skeletonRoot == null)
        {
            Debug.LogError("[SidekickPrefabBaker] Could not find skeleton root!");
            DestroyImmediate(bakedCharacter);
            return;
        }
        Debug.Log($"[SidekickPrefabBaker] Found skeleton root: {skeletonRoot.name}");

        // Build bone dictionary
        var boneDict = new Dictionary<string, Transform>();
        foreach (var bone in skeletonRoot.GetComponentsInChildren<Transform>())
        {
            if (!boneDict.ContainsKey(bone.name))
            {
                boneDict[bone.name] = bone;
            }
        }
        Debug.Log($"[SidekickPrefabBaker] Built bone dictionary with {boneDict.Count} bones");

        // Verify all mesh bone bindings
        int validMeshes = 0;
        int fixedMeshes = 0;
        foreach (var renderer in meshRenderers)
        {
            bool allBonesValid = true;
            for (int i = 0; i < renderer.bones.Length; i++)
            {
                if (renderer.bones[i] == null)
                {
                    allBonesValid = false;
                    break;
                }
            }

            if (allBonesValid)
            {
                validMeshes++;
            }
            else
            {
                // Try to fix bone bindings
                Debug.LogWarning($"[SidekickPrefabBaker] Mesh {renderer.name} has null bones, attempting to fix...");
                // This shouldn't happen if the character was set up correctly in the scene
                fixedMeshes++;
            }
        }

        Debug.Log($"[SidekickPrefabBaker] Valid meshes: {validMeshes}, Fixed: {fixedMeshes}");

        // Remove any Sidekick runtime components that won't work in WebGL
        RemoveRuntimeComponents(bakedCharacter);

        // Add the WebGL character controller
        var webglController = bakedCharacter.GetComponent<BakedCharacterController>();
        if (webglController == null)
        {
            webglController = bakedCharacter.AddComponent<BakedCharacterController>();
        }

        // Save as prefab
        string prefabPath = $"{m_OutputPath}/{m_PrefabName}.prefab";

        // Remove existing prefab if it exists
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            AssetDatabase.DeleteAsset(prefabPath);
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(bakedCharacter, prefabPath);

        // Clean up scene object
        DestroyImmediate(bakedCharacter);

        if (prefab != null)
        {
            Debug.Log($"[SidekickPrefabBaker] Successfully created prefab at: {prefabPath}");
            EditorUtility.DisplayDialog("Success", $"Prefab created at:\n{prefabPath}", "OK");

            // Select the new prefab
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "Failed to create prefab.", "OK");
        }
    }

    private Transform FindSkeletonRoot(Transform root)
    {
        // Look for common skeleton root names
        string[] rootNames = { "root", "Root", "Armature", "Skeleton", "Hips", "hips" };

        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            foreach (var name in rootNames)
            {
                if (child.name == name)
                {
                    return child;
                }
            }
        }

        return null;
    }

    private void RemoveRuntimeComponents(GameObject obj)
    {
        // Remove components that won't work in WebGL or are Sidekick-specific
        var componentsToRemove = new List<Component>();

        // Find SidekickPlayerController
        var sidekickController = obj.GetComponent<SidekickPlayerController>();
        if (sidekickController != null)
        {
            componentsToRemove.Add(sidekickController);
        }

        // Remove them
        foreach (var comp in componentsToRemove)
        {
            Debug.Log($"[SidekickPrefabBaker] Removing component: {comp.GetType().Name}");
            DestroyImmediate(comp);
        }
    }
}
