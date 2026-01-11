#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor tool to batch-apply ragdoll setup to all zombie prefabs
/// </summary>
public class ZombieRagdollSetup : EditorWindow
{
    private float totalMass = 50f;
    private bool addToExisting = false;

    [MenuItem("Project Klyra/Zombies/Setup Ragdolls")]
    public static void ShowWindow()
    {
        GetWindow<ZombieRagdollSetup>("Zombie Ragdoll Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Batch Ragdoll Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        totalMass = EditorGUILayout.FloatField("Total Mass", totalMass);
        addToExisting = EditorGUILayout.Toggle("Overwrite Existing", addToExisting);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "This will add ragdoll rigidbodies and colliders to all zombie prefabs.\n\n" +
            "Looks for prefabs with 'Zombie' in the name under:\n" +
            "- Assets/Synty/SidekickCharacters/\n" +
            "- Any prefab with ZombieAI component",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Setup All Zombie Ragdolls", GUILayout.Height(30)))
        {
            SetupAllZombieRagdolls();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Setup Selected Prefab Only", GUILayout.Height(25)))
        {
            SetupSelectedPrefab();
        }
    }

    private void SetupAllZombieRagdolls()
    {
        // Find zombie prefabs
        string[] guids = AssetDatabase.FindAssets("t:Prefab Zombie");
        List<GameObject> zombiePrefabs = new List<GameObject>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && prefab.name.ToLower().Contains("zombie"))
            {
                zombiePrefabs.Add(prefab);
            }
        }

        // Also search in Synty SidekickCharacters
        string[] sidekickGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Synty/SidekickCharacters" });
        foreach (string guid in sidekickGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.ToLower().Contains("zombie"))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && !zombiePrefabs.Contains(prefab))
                {
                    zombiePrefabs.Add(prefab);
                }
            }
        }

        if (zombiePrefabs.Count == 0)
        {
            EditorUtility.DisplayDialog("No Zombies Found", "No zombie prefabs found.", "OK");
            return;
        }

        int success = 0;
        int skipped = 0;
        int failed = 0;

        foreach (var prefab in zombiePrefabs)
        {
            try
            {
                // Check if already has ragdoll
                var existingRbs = prefab.GetComponentsInChildren<Rigidbody>();
                bool hasRagdoll = false;
                foreach (var rb in existingRbs)
                {
                    if (rb.gameObject != prefab) // Has rigidbody on child = has ragdoll
                    {
                        hasRagdoll = true;
                        break;
                    }
                }

                if (hasRagdoll && !addToExisting)
                {
                    skipped++;
                    continue;
                }

                if (SetupRagdollOnPrefab(prefab))
                {
                    success++;
                }
                else
                {
                    failed++;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to setup ragdoll on {prefab.name}: {e.Message}");
                failed++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Ragdoll Setup Complete",
            $"Success: {success}\nSkipped (already has ragdoll): {skipped}\nFailed: {failed}",
            "OK");
    }

    private void SetupSelectedPrefab()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select a prefab in the Project window.", "OK");
            return;
        }

        string path = AssetDatabase.GetAssetPath(selected);
        if (string.IsNullOrEmpty(path))
        {
            EditorUtility.DisplayDialog("Not a Prefab", "Please select a prefab from the Project window.", "OK");
            return;
        }

        if (SetupRagdollOnPrefab(selected))
        {
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Success", $"Ragdoll setup complete on {selected.name}", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Failed", $"Could not find bones on {selected.name}", "OK");
        }
    }

    private bool SetupRagdollOnPrefab(GameObject prefab)
    {
        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        try
        {
            // Find the armature/skeleton root
            Animator animator = prefabRoot.GetComponentInChildren<Animator>();
            if (animator == null || animator.avatar == null)
            {
                Debug.LogWarning($"{prefab.name}: No animator with avatar found");
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                return false;
            }

            // Get bone transforms
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Transform spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);

            Transform leftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            Transform leftLowerLeg = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);

            Transform rightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            Transform rightLowerLeg = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);

            Transform leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Transform leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);

            Transform rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            Transform rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);

            if (hips == null)
            {
                Debug.LogWarning($"{prefab.name}: Could not find Hips bone");
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                return false;
            }

            // Remove existing ragdoll components if overwriting
            if (addToExisting)
            {
                var existingJoints = prefabRoot.GetComponentsInChildren<CharacterJoint>();
                foreach (var j in existingJoints) DestroyImmediate(j);

                var existingRbs = prefabRoot.GetComponentsInChildren<Rigidbody>();
                foreach (var rb in existingRbs)
                {
                    if (rb.gameObject != prefabRoot) DestroyImmediate(rb);
                }

                var existingCols = prefabRoot.GetComponentsInChildren<Collider>();
                foreach (var c in existingCols)
                {
                    if (c.gameObject != prefabRoot) DestroyImmediate(c);
                }
            }

            // Mass distribution (approximate human proportions)
            float hipsMass = totalMass * 0.15f;
            float spineMass = totalMass * 0.12f;
            float chestMass = totalMass * 0.12f;
            float headMass = totalMass * 0.08f;
            float upperLegMass = totalMass * 0.10f;
            float lowerLegMass = totalMass * 0.05f;
            float footMass = totalMass * 0.02f;
            float upperArmMass = totalMass * 0.04f;
            float lowerArmMass = totalMass * 0.02f;
            float handMass = totalMass * 0.01f;

            // Setup bones
            Rigidbody hipsRb = SetupBone(hips, null, hipsMass, 0.15f, 0.2f);

            Rigidbody spineRb = null;
            if (spine != null)
                spineRb = SetupBone(spine, hipsRb, spineMass, 0.12f, 0.2f, 20f, 20f);

            Rigidbody chestRb = null;
            if (chest != null)
                chestRb = SetupBone(chest, spineRb ?? hipsRb, chestMass, 0.15f, 0.25f, 20f, 10f);

            if (head != null)
                SetupBone(head, chestRb ?? spineRb ?? hipsRb, headMass, 0.1f, 0.15f, 30f, 30f);

            // Legs
            Rigidbody leftUpperLegRb = null;
            if (leftUpperLeg != null)
                leftUpperLegRb = SetupBone(leftUpperLeg, hipsRb, upperLegMass, 0.08f, 0.4f, 60f, 20f);

            Rigidbody leftLowerLegRb = null;
            if (leftLowerLeg != null)
                leftLowerLegRb = SetupBone(leftLowerLeg, leftUpperLegRb, lowerLegMass, 0.06f, 0.35f, 80f, 0f);

            if (leftFoot != null)
                SetupBone(leftFoot, leftLowerLegRb, footMass, 0.05f, 0.1f, 30f, 20f);

            Rigidbody rightUpperLegRb = null;
            if (rightUpperLeg != null)
                rightUpperLegRb = SetupBone(rightUpperLeg, hipsRb, upperLegMass, 0.08f, 0.4f, 60f, 20f);

            Rigidbody rightLowerLegRb = null;
            if (rightLowerLeg != null)
                rightLowerLegRb = SetupBone(rightLowerLeg, rightUpperLegRb, lowerLegMass, 0.06f, 0.35f, 80f, 0f);

            if (rightFoot != null)
                SetupBone(rightFoot, rightLowerLegRb, footMass, 0.05f, 0.1f, 30f, 20f);

            // Arms
            Rigidbody leftUpperArmRb = null;
            if (leftUpperArm != null)
                leftUpperArmRb = SetupBone(leftUpperArm, chestRb ?? spineRb ?? hipsRb, upperArmMass, 0.05f, 0.25f, 70f, 40f);

            Rigidbody leftLowerArmRb = null;
            if (leftLowerArm != null)
                leftLowerArmRb = SetupBone(leftLowerArm, leftUpperArmRb, lowerArmMass, 0.04f, 0.2f, 90f, 0f);

            if (leftHand != null)
                SetupBone(leftHand, leftLowerArmRb, handMass, 0.03f, 0.08f, 40f, 20f);

            Rigidbody rightUpperArmRb = null;
            if (rightUpperArm != null)
                rightUpperArmRb = SetupBone(rightUpperArm, chestRb ?? spineRb ?? hipsRb, upperArmMass, 0.05f, 0.25f, 70f, 40f);

            Rigidbody rightLowerArmRb = null;
            if (rightLowerArm != null)
                rightLowerArmRb = SetupBone(rightLowerArm, rightUpperArmRb, lowerArmMass, 0.04f, 0.2f, 90f, 0f);

            if (rightHand != null)
                SetupBone(rightHand, rightLowerArmRb, handMass, 0.03f, 0.08f, 40f, 20f);

            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            Debug.Log($"[ZombieRagdollSetup] Successfully set up ragdoll on {prefab.name}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ZombieRagdollSetup] Error on {prefab.name}: {e.Message}");
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            return false;
        }
    }

    private Rigidbody SetupBone(Transform bone, Rigidbody connectedBody, float mass,
        float radius, float height, float swingLimit = 0f, float twistLimit = 0f)
    {
        if (bone == null) return null;

        // Add or get Rigidbody
        Rigidbody rb = bone.GetComponent<Rigidbody>();
        if (rb == null)
            rb = bone.gameObject.AddComponent<Rigidbody>();

        rb.mass = mass;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 1f;
        rb.isKinematic = true; // Start kinematic, ZombieRagdoll will enable
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Add capsule collider
        CapsuleCollider col = bone.GetComponent<CapsuleCollider>();
        if (col == null)
            col = bone.gameObject.AddComponent<CapsuleCollider>();

        col.radius = radius;
        col.height = height;
        col.direction = 1; // Y-axis
        col.enabled = false; // Start disabled

        // Add character joint if connected to parent
        if (connectedBody != null)
        {
            CharacterJoint joint = bone.GetComponent<CharacterJoint>();
            if (joint == null)
                joint = bone.gameObject.AddComponent<CharacterJoint>();

            joint.connectedBody = connectedBody;
            joint.enableProjection = true;

            // Set limits
            var lowTwist = joint.lowTwistLimit;
            lowTwist.limit = -twistLimit;
            joint.lowTwistLimit = lowTwist;

            var highTwist = joint.highTwistLimit;
            highTwist.limit = twistLimit;
            joint.highTwistLimit = highTwist;

            var swing1 = joint.swing1Limit;
            swing1.limit = swingLimit;
            joint.swing1Limit = swing1;

            var swing2 = joint.swing2Limit;
            swing2.limit = swingLimit;
            joint.swing2Limit = swing2;
        }

        return rb;
    }
}
#endif
