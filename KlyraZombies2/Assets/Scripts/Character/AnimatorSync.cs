using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Syncs animator parameters, animation states, and bone transforms from a source animator to a target animator.
/// Used to make Opsive's animator (with IK) control a Sidekick character's animator.
/// Runs with late script execution order to ensure IK has been applied first.
/// </summary>
[DefaultExecutionOrder(100)] // Run after Opsive's IK
public class AnimatorSync : MonoBehaviour
{
    private Animator m_Source;
    private Animator m_Target;
    private bool m_Initialized;
    private int m_LayerCount;

    // Bone sync for IK
    private bool m_SyncBones = true;
    private Dictionary<Transform, Transform> m_BoneMap;
    private Transform[] m_SourceBones;

    // Hand bones for extra position sync (important for weapons)
    private Transform m_SourceRightHand;
    private Transform m_SourceLeftHand;
    private Transform m_TargetRightHand;
    private Transform m_TargetLeftHand;

    public void Initialize(Animator source, Animator target)
    {
        m_Source = source;
        m_Target = target;
        m_Initialized = source != null && target != null;

        if (m_Initialized)
        {
            m_LayerCount = m_Source.layerCount;

            // Build bone mapping for IK sync
            BuildBoneMap();

            // Find hand bones specifically (important for weapon attachment)
            FindHandBones();

            Debug.Log($"[AnimatorSync] Initialized with {m_LayerCount} layers, {m_Source.parameterCount} parameters, {m_BoneMap?.Count ?? 0} mapped bones");
            Debug.Log($"[AnimatorSync] Hand bones - Source RH: {m_SourceRightHand?.name}, LH: {m_SourceLeftHand?.name}");
            Debug.Log($"[AnimatorSync] Hand bones - Target RH: {m_TargetRightHand?.name}, LH: {m_TargetLeftHand?.name}");
        }
    }

    /// <summary>
    /// Find hand bones specifically for position syncing.
    /// </summary>
    private void FindHandBones()
    {
        // Include Sidekick naming (hand_r, hand_l), Mixamo/Opsive naming (RightHand, LeftHand), etc.
        string[] rightHandNames = { "hand_r", "Hand_R", "RightHand", "Right Hand", "hand.R", "HandR", "r_hand", "R_Hand", "mixamorig:RightHand" };
        string[] leftHandNames = { "hand_l", "Hand_L", "LeftHand", "Left Hand", "hand.L", "HandL", "l_hand", "L_Hand", "mixamorig:LeftHand" };

        m_SourceRightHand = FindBone(m_Source.transform, rightHandNames);
        m_SourceLeftHand = FindBone(m_Source.transform, leftHandNames);
        m_TargetRightHand = FindBone(m_Target.transform, rightHandNames);
        m_TargetLeftHand = FindBone(m_Target.transform, leftHandNames);
    }

    private Transform FindBone(Transform root, string[] possibleNames)
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            foreach (var name in possibleNames)
            {
                if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Build a mapping from source skeleton bones to target skeleton bones by name.
    /// </summary>
    private void BuildBoneMap()
    {
        m_BoneMap = new Dictionary<Transform, Transform>();

        // Get all bones from source
        var sourceRoot = m_Source.transform;
        m_SourceBones = sourceRoot.GetComponentsInChildren<Transform>();

        // Get all bones from target
        var targetRoot = m_Target.transform;
        var targetBones = targetRoot.GetComponentsInChildren<Transform>();

        // Build a lookup for target bones by name
        var targetLookup = new Dictionary<string, Transform>();
        foreach (var bone in targetBones)
        {
            string normalizedName = NormalizeBoneName(bone.name);
            if (!targetLookup.ContainsKey(normalizedName))
            {
                targetLookup[normalizedName] = bone;
            }
        }

        // Map source bones to target bones
        foreach (var sourceBone in m_SourceBones)
        {
            string normalizedName = NormalizeBoneName(sourceBone.name);
            if (targetLookup.TryGetValue(normalizedName, out var targetBone))
            {
                m_BoneMap[sourceBone] = targetBone;
            }
        }
    }

    /// <summary>
    /// Normalize bone names to handle different naming conventions.
    /// </summary>
    private string NormalizeBoneName(string name)
    {
        // Convert to lowercase and remove common prefixes/suffixes
        string normalized = name.ToLower();

        // Remove common prefixes
        if (normalized.StartsWith("mixamorig:"))
            normalized = normalized.Substring(10);
        if (normalized.StartsWith("bip01 "))
            normalized = normalized.Substring(6);

        // Normalize common bone name variations
        normalized = normalized.Replace("_", "");
        normalized = normalized.Replace(" ", "");
        normalized = normalized.Replace("-", "");

        return normalized;
    }

    private void LateUpdate()
    {
        if (!m_Initialized || m_Source == null || m_Target == null)
            return;

        // Sync all parameters from source to target
        foreach (var param in m_Source.parameters)
        {
            switch (param.type)
            {
                case AnimatorControllerParameterType.Float:
                    m_Target.SetFloat(param.nameHash, m_Source.GetFloat(param.nameHash));
                    break;
                case AnimatorControllerParameterType.Int:
                    m_Target.SetInteger(param.nameHash, m_Source.GetInteger(param.nameHash));
                    break;
                case AnimatorControllerParameterType.Bool:
                    m_Target.SetBool(param.nameHash, m_Source.GetBool(param.nameHash));
                    break;
                case AnimatorControllerParameterType.Trigger:
                    // Triggers are harder to sync - skip for now
                    break;
            }
        }

        // Sync layer weights
        for (int i = 0; i < m_LayerCount; i++)
        {
            m_Target.SetLayerWeight(i, m_Source.GetLayerWeight(i));
        }

        // Sync animation states for each layer
        for (int i = 0; i < m_LayerCount; i++)
        {
            var sourceState = m_Source.GetCurrentAnimatorStateInfo(i);
            var targetState = m_Target.GetCurrentAnimatorStateInfo(i);

            // If source is playing a different state, crossfade to it
            if (sourceState.fullPathHash != targetState.fullPathHash)
            {
                m_Target.CrossFade(sourceState.fullPathHash, 0.1f, i, sourceState.normalizedTime);
            }

            // Check if there's a next state transition happening
            if (m_Source.IsInTransition(i))
            {
                var nextState = m_Source.GetNextAnimatorStateInfo(i);
                var transitionInfo = m_Source.GetAnimatorTransitionInfo(i);

                // If target isn't in the same transition, start it
                if (!m_Target.IsInTransition(i) || m_Target.GetNextAnimatorStateInfo(i).fullPathHash != nextState.fullPathHash)
                {
                    m_Target.CrossFade(nextState.fullPathHash, transitionInfo.duration, i);
                }
            }
        }

        // Sync bone transforms (for IK results)
        if (m_SyncBones && m_BoneMap != null)
        {
            SyncBoneTransforms();
        }
    }

    /// <summary>
    /// Copy bone rotations from source skeleton to target skeleton.
    /// This captures the IK modifications made by Opsive's CharacterIK.
    /// </summary>
    private void SyncBoneTransforms()
    {
        foreach (var pair in m_BoneMap)
        {
            if (pair.Key != null && pair.Value != null)
            {
                // Copy local rotation (this is what IK modifies)
                pair.Value.localRotation = pair.Key.localRotation;
            }
        }

        // Special handling for hand bones - sync world position AND rotation
        // This ensures weapons attached to Opsive hands appear at Sidekick hand positions
        SyncHandBones();
    }

    /// <summary>
    /// Sync hand bone world positions and rotations.
    /// This is critical for weapon attachment - weapons stay on Opsive skeleton
    /// but Sidekick hands need to match Opsive hands exactly.
    /// </summary>
    private void SyncHandBones()
    {
        // Make Sidekick right hand match Opsive right hand world transform
        if (m_SourceRightHand != null && m_TargetRightHand != null)
        {
            m_TargetRightHand.position = m_SourceRightHand.position;
            m_TargetRightHand.rotation = m_SourceRightHand.rotation;
        }

        // Make Sidekick left hand match Opsive left hand world transform
        if (m_SourceLeftHand != null && m_TargetLeftHand != null)
        {
            m_TargetLeftHand.position = m_SourceLeftHand.position;
            m_TargetLeftHand.rotation = m_SourceLeftHand.rotation;
        }
    }
}
