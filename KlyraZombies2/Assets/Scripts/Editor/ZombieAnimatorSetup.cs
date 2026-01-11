#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

/// <summary>
/// Creates a zombie Animator Controller with proper states and transitions.
/// </summary>
public class ZombieAnimatorSetup : EditorWindow
{
    private AnimationClip m_IdleClip;
    private AnimationClip m_WalkClip;
    private AnimationClip m_RunClip;
    private AnimationClip m_AttackClip;
    private AnimationClip m_DeathClip;
    private AnimationClip m_AlertClip;

    [MenuItem("Project Klyra/Zombies/Create Animator Controller")]
    public static void ShowWindow()
    {
        GetWindow<ZombieAnimatorSetup>("Zombie Animator Setup");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Zombie Animator Controller Creator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Drag your animation clips here to create a complete Animator Controller.\n\n" +
            "Get free zombie animations from Mixamo.com:\n" +
            "- Search 'Zombie Idle', 'Zombie Walk', etc.\n" +
            "- Download as 'FBX for Unity'",
            MessageType.Info);

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Animation Clips", EditorStyles.boldLabel);

        m_IdleClip = (AnimationClip)EditorGUILayout.ObjectField("Idle", m_IdleClip, typeof(AnimationClip), false);
        m_WalkClip = (AnimationClip)EditorGUILayout.ObjectField("Walk", m_WalkClip, typeof(AnimationClip), false);
        m_RunClip = (AnimationClip)EditorGUILayout.ObjectField("Run", m_RunClip, typeof(AnimationClip), false);
        m_AttackClip = (AnimationClip)EditorGUILayout.ObjectField("Attack", m_AttackClip, typeof(AnimationClip), false);
        m_DeathClip = (AnimationClip)EditorGUILayout.ObjectField("Death", m_DeathClip, typeof(AnimationClip), false);
        m_AlertClip = (AnimationClip)EditorGUILayout.ObjectField("Alert (Optional)", m_AlertClip, typeof(AnimationClip), false);

        EditorGUILayout.Space(10);

        // Check minimum requirements
        bool hasMinimum = m_IdleClip != null && m_WalkClip != null;

        if (!hasMinimum)
        {
            EditorGUILayout.HelpBox("At minimum, provide Idle and Walk clips.", MessageType.Warning);
        }

        GUI.enabled = hasMinimum;
        if (GUILayout.Button("Create Animator Controller", GUILayout.Height(30)))
        {
            CreateAnimatorController();
        }
        GUI.enabled = true;

        EditorGUILayout.Space(20);

        EditorGUILayout.LabelField("Or Create Empty Controller", EditorStyles.boldLabel);
        if (GUILayout.Button("Create Empty Controller (Add Clips Later)"))
        {
            CreateEmptyAnimatorController();
        }
    }

    private void CreateAnimatorController()
    {
        // Ensure directory exists
        string dir = "Assets/Animations/Zombie";
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Animations", "Zombie");

        string path = $"{dir}/ZombieAnimator.controller";

        // Create the controller
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        // Add parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dead", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Alert", AnimatorControllerParameterType.Trigger);

        // Get the root state machine
        var rootStateMachine = controller.layers[0].stateMachine;

        // Create states
        var idleState = rootStateMachine.AddState("Idle", new Vector3(0, 0, 0));
        var walkState = rootStateMachine.AddState("Walk", new Vector3(0, 100, 0));
        var runState = rootStateMachine.AddState("Run", new Vector3(0, 200, 0));
        var attackState = rootStateMachine.AddState("Attack", new Vector3(200, 100, 0));
        var deathState = rootStateMachine.AddState("Death", new Vector3(200, 200, 0));
        var alertState = rootStateMachine.AddState("Alert", new Vector3(-200, 100, 0));

        // Assign clips
        idleState.motion = m_IdleClip;
        walkState.motion = m_WalkClip;
        runState.motion = m_RunClip ?? m_WalkClip; // Fallback to walk if no run
        attackState.motion = m_AttackClip ?? m_IdleClip;
        deathState.motion = m_DeathClip ?? m_IdleClip;
        alertState.motion = m_AlertClip ?? m_IdleClip;

        // Set default state
        rootStateMachine.defaultState = idleState;

        // Create transitions

        // Idle -> Walk (Speed > 0.1)
        var idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        idleToWalk.hasExitTime = false;
        idleToWalk.duration = 0.2f;

        // Walk -> Idle (Speed < 0.1)
        var walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        walkToIdle.hasExitTime = false;
        walkToIdle.duration = 0.2f;

        // Walk -> Run (Speed > 2)
        var walkToRun = walkState.AddTransition(runState);
        walkToRun.AddCondition(AnimatorConditionMode.Greater, 2f, "Speed");
        walkToRun.hasExitTime = false;
        walkToRun.duration = 0.2f;

        // Run -> Walk (Speed < 2)
        var runToWalk = runState.AddTransition(walkState);
        runToWalk.AddCondition(AnimatorConditionMode.Less, 2f, "Speed");
        runToWalk.hasExitTime = false;
        runToWalk.duration = 0.2f;

        // Any State -> Attack
        var anyToAttack = rootStateMachine.AddAnyStateTransition(attackState);
        anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        anyToAttack.hasExitTime = false;
        anyToAttack.duration = 0.1f;

        // Attack -> Idle (after animation)
        var attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 0.9f;
        attackToIdle.duration = 0.1f;

        // Any State -> Death
        var anyToDeath = rootStateMachine.AddAnyStateTransition(deathState);
        anyToDeath.AddCondition(AnimatorConditionMode.If, 0, "Dead");
        anyToDeath.hasExitTime = false;
        anyToDeath.duration = 0.1f;

        // Any State -> Alert
        var anyToAlert = rootStateMachine.AddAnyStateTransition(alertState);
        anyToAlert.AddCondition(AnimatorConditionMode.If, 0, "Alert");
        anyToAlert.hasExitTime = false;
        anyToAlert.duration = 0.2f;

        // Alert -> Idle (after animation)
        var alertToIdle = alertState.AddTransition(idleState);
        alertToIdle.hasExitTime = true;
        alertToIdle.exitTime = 0.9f;
        alertToIdle.duration = 0.2f;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success",
            $"Created Animator Controller at:\n{path}\n\n" +
            "Assign this to your zombie prefabs' Animator component.",
            "OK");

        // Ping in project
        EditorGUIUtility.PingObject(controller);
    }

    private void CreateEmptyAnimatorController()
    {
        // Ensure directory exists
        string dir = "Assets/Animations/Zombie";
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Animations", "Zombie");

        string path = $"{dir}/ZombieAnimator.controller";

        // Create the controller
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        // Add parameters that ZombieAI expects
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dead", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Alert", AnimatorControllerParameterType.Trigger);

        // Get the root state machine
        var rootStateMachine = controller.layers[0].stateMachine;

        // Create empty states with placeholder positions
        var idleState = rootStateMachine.AddState("Idle", new Vector3(0, 0, 0));
        var walkState = rootStateMachine.AddState("Walk", new Vector3(0, 100, 0));
        var runState = rootStateMachine.AddState("Run", new Vector3(0, 200, 0));
        var attackState = rootStateMachine.AddState("Attack", new Vector3(250, 100, 0));
        var deathState = rootStateMachine.AddState("Death", new Vector3(250, 200, 0));
        var alertState = rootStateMachine.AddState("Alert", new Vector3(-250, 100, 0));

        rootStateMachine.defaultState = idleState;

        // Idle <-> Walk transitions
        var idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        idleToWalk.hasExitTime = false;
        idleToWalk.duration = 0.25f;

        var walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        walkToIdle.hasExitTime = false;
        walkToIdle.duration = 0.25f;

        // Walk <-> Run transitions
        var walkToRun = walkState.AddTransition(runState);
        walkToRun.AddCondition(AnimatorConditionMode.Greater, 2f, "Speed");
        walkToRun.hasExitTime = false;
        walkToRun.duration = 0.2f;

        var runToWalk = runState.AddTransition(walkState);
        runToWalk.AddCondition(AnimatorConditionMode.Less, 2f, "Speed");
        runToWalk.hasExitTime = false;
        runToWalk.duration = 0.2f;

        // Any -> Attack
        var anyToAttack = rootStateMachine.AddAnyStateTransition(attackState);
        anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        anyToAttack.hasExitTime = false;
        anyToAttack.duration = 0.1f;

        var attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 0.9f;
        attackToIdle.duration = 0.1f;

        // Any -> Death
        var anyToDeath = rootStateMachine.AddAnyStateTransition(deathState);
        anyToDeath.AddCondition(AnimatorConditionMode.If, 0, "Dead");
        anyToDeath.hasExitTime = false;
        anyToDeath.duration = 0.1f;

        // Any -> Alert
        var anyToAlert = rootStateMachine.AddAnyStateTransition(alertState);
        anyToAlert.AddCondition(AnimatorConditionMode.If, 0, "Alert");
        anyToAlert.hasExitTime = false;
        anyToAlert.duration = 0.15f;

        var alertToIdle = alertState.AddTransition(idleState);
        alertToIdle.hasExitTime = true;
        alertToIdle.exitTime = 0.9f;
        alertToIdle.duration = 0.2f;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success",
            $"Created empty Animator Controller at:\n{path}\n\n" +
            "States created: Idle, Walk, Run, Attack, Death, Alert\n" +
            "Parameters: Speed (float), Attack, Dead, Alert (triggers)\n\n" +
            "Open it and drag your animation clips into each state.",
            "OK");

        // Select and ping
        Selection.activeObject = controller;
        EditorGUIUtility.PingObject(controller);
    }
}
#endif
