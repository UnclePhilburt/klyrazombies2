#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class CreateZombieAnimator
{
    [MenuItem("Tools/Create Zombie Animator (Kevin Iglesias)")]
    public static void Create()
    {
        string animPath = "Assets/Kevin Iglesias/Zombie Animations/Animations";

        // Load animation clips from FBX files
        AnimationClip idleClip = LoadClipFromFBX($"{animPath}/Zombie@Idle01.fbx");
        AnimationClip walkClip = LoadClipFromFBX($"{animPath}/Zombie@Walk01.fbx");
        AnimationClip attackClip = LoadClipFromFBX($"{animPath}/Zombie@Attack01.fbx");
        AnimationClip deathClip = LoadClipFromFBX($"{animPath}/Zombie@Death01_A.fbx");
        AnimationClip damageClip = LoadClipFromFBX($"{animPath}/Zombie@Damage01.fbx");
        AnimationClip alertClip = LoadClipFromFBX($"{animPath}/Zombie@Idle01_Action01.fbx");

        if (idleClip == null || walkClip == null)
        {
            EditorUtility.DisplayDialog("Error",
                "Could not find Kevin Iglesias zombie animations!\n\n" +
                "Expected path: Assets/Kevin Iglesias/Zombie Animations/Animations/",
                "OK");
            return;
        }

        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");
        if (!AssetDatabase.IsValidFolder("Assets/Animations/Zombie"))
            AssetDatabase.CreateFolder("Assets/Animations", "Zombie");

        string controllerPath = "Assets/Animations/Zombie/ZombieAnimator.controller";

        // Delete existing if present
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
        {
            AssetDatabase.DeleteAsset(controllerPath);
        }

        // Create controller
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // Add parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dead", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Alert", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);

        var rootSM = controller.layers[0].stateMachine;

        // Create states
        var idleState = rootSM.AddState("Idle", new Vector3(0, 0, 0));
        var walkState = rootSM.AddState("Walk", new Vector3(0, 100, 0));
        var runState = rootSM.AddState("Run", new Vector3(0, 200, 0));
        var attackState = rootSM.AddState("Attack", new Vector3(300, 0, 0));
        var deathState = rootSM.AddState("Death", new Vector3(300, 100, 0));
        var alertState = rootSM.AddState("Alert", new Vector3(-300, 0, 0));
        var hitState = rootSM.AddState("Hit", new Vector3(300, 200, 0));

        // Assign clips
        idleState.motion = idleClip;
        walkState.motion = walkClip;
        runState.motion = walkClip; // Use walk sped up for run
        attackState.motion = attackClip ?? idleClip;
        deathState.motion = deathClip ?? idleClip;
        alertState.motion = alertClip ?? idleClip;
        hitState.motion = damageClip ?? idleClip;

        // Speed up run state
        runState.speed = 1.5f;

        rootSM.defaultState = idleState;

        // === TRANSITIONS ===

        // Idle -> Walk (Speed > 0.1)
        var t1 = idleState.AddTransition(walkState);
        t1.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        t1.hasExitTime = false;
        t1.duration = 0.2f;

        // Walk -> Idle (Speed < 0.1)
        var t2 = walkState.AddTransition(idleState);
        t2.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        t2.hasExitTime = false;
        t2.duration = 0.2f;

        // Walk -> Run (Speed > 2.5)
        var t3 = walkState.AddTransition(runState);
        t3.AddCondition(AnimatorConditionMode.Greater, 2.5f, "Speed");
        t3.hasExitTime = false;
        t3.duration = 0.15f;

        // Run -> Walk (Speed < 2.5)
        var t4 = runState.AddTransition(walkState);
        t4.AddCondition(AnimatorConditionMode.Less, 2.5f, "Speed");
        t4.hasExitTime = false;
        t4.duration = 0.15f;

        // Idle -> Run (Speed > 2.5, skip walk)
        var t5 = idleState.AddTransition(runState);
        t5.AddCondition(AnimatorConditionMode.Greater, 2.5f, "Speed");
        t5.hasExitTime = false;
        t5.duration = 0.2f;

        // Run -> Idle (Speed < 0.1)
        var t6 = runState.AddTransition(idleState);
        t6.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        t6.hasExitTime = false;
        t6.duration = 0.2f;

        // Any -> Attack
        var tAttack = rootSM.AddAnyStateTransition(attackState);
        tAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        tAttack.hasExitTime = false;
        tAttack.duration = 0.1f;
        tAttack.canTransitionToSelf = false;

        // Attack -> Idle
        var tAttackOut = attackState.AddTransition(idleState);
        tAttackOut.hasExitTime = true;
        tAttackOut.exitTime = 0.85f;
        tAttackOut.duration = 0.15f;

        // Any -> Death
        var tDeath = rootSM.AddAnyStateTransition(deathState);
        tDeath.AddCondition(AnimatorConditionMode.If, 0, "Dead");
        tDeath.hasExitTime = false;
        tDeath.duration = 0.1f;

        // Any -> Alert
        var tAlert = rootSM.AddAnyStateTransition(alertState);
        tAlert.AddCondition(AnimatorConditionMode.If, 0, "Alert");
        tAlert.hasExitTime = false;
        tAlert.duration = 0.2f;
        tAlert.canTransitionToSelf = false;

        // Alert -> Idle
        var tAlertOut = alertState.AddTransition(idleState);
        tAlertOut.hasExitTime = true;
        tAlertOut.exitTime = 0.9f;
        tAlertOut.duration = 0.2f;

        // Any -> Hit
        var tHit = rootSM.AddAnyStateTransition(hitState);
        tHit.AddCondition(AnimatorConditionMode.If, 0, "Hit");
        tHit.hasExitTime = false;
        tHit.duration = 0.1f;
        tHit.canTransitionToSelf = false;

        // Hit -> Idle
        var tHitOut = hitState.AddTransition(idleState);
        tHitOut.hasExitTime = true;
        tHitOut.exitTime = 0.8f;
        tHitOut.duration = 0.15f;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success",
            "Created ZombieAnimator.controller at:\n" +
            "Assets/Animations/Zombie/ZombieAnimator.controller\n\n" +
            "Animations loaded:\n" +
            $"- Idle: {(idleClip != null ? "Yes" : "No")}\n" +
            $"- Walk: {(walkClip != null ? "Yes" : "No")}\n" +
            $"- Attack: {(attackClip != null ? "Yes" : "No")}\n" +
            $"- Death: {(deathClip != null ? "Yes" : "No")}\n" +
            $"- Alert: {(alertClip != null ? "Yes" : "No")}\n" +
            $"- Hit/Damage: {(damageClip != null ? "Yes" : "No")}\n\n" +
            "Assign this controller to your zombie prefabs!",
            "OK");

        Selection.activeObject = controller;
        EditorGUIUtility.PingObject(controller);
    }

    private static AnimationClip LoadClipFromFBX(string fbxPath)
    {
        // Load all assets from FBX
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);

        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                return clip;
            }
        }

        return null;
    }
}
#endif
