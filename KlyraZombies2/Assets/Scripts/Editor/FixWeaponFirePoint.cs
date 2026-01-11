using UnityEngine;
using UnityEditor;

public class FixWeaponFirePoint : EditorWindow
{
    private GameObject m_WeaponPrefab;

    [MenuItem("Project Klyra/Weapons/Fix Weapon Fire Point")]
    public static void ShowWindow()
    {
        GetWindow<FixWeaponFirePoint>("Fix Weapon Fire Point");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Fix Weapon Fire Point", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This tool adds a FirePoint child to your weapon and configures it for shooting.\n\n" +
            "1. Drag your weapon PREFAB here\n" +
            "2. Click 'Add Fire Point'\n" +
            "3. Adjust the FirePoint position in the prefab to be at the muzzle",
            MessageType.Info);

        EditorGUILayout.Space();

        m_WeaponPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Weapon Prefab", m_WeaponPrefab, typeof(GameObject), false);

        EditorGUILayout.Space();

        if (m_WeaponPrefab == null)
        {
            EditorGUILayout.HelpBox("Please assign your weapon prefab", MessageType.Warning);
            return;
        }

        // Check if it's a prefab
        if (PrefabUtility.GetPrefabAssetType(m_WeaponPrefab) == PrefabAssetType.NotAPrefab)
        {
            EditorGUILayout.HelpBox("Please assign a PREFAB, not a scene object", MessageType.Error);
            return;
        }

        if (GUILayout.Button("Add Fire Point", GUILayout.Height(40)))
        {
            AddFirePoint();
        }
    }

    private void AddFirePoint()
    {
        // Open prefab for editing
        string prefabPath = AssetDatabase.GetAssetPath(m_WeaponPrefab);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        // Find the visual mesh (should be first child or named SM_Wep_*)
        Transform visualMesh = null;
        foreach (Transform child in prefabRoot.transform)
        {
            if (child.name.StartsWith("SM_Wep") || child.GetComponent<MeshRenderer>() != null)
            {
                visualMesh = child;
                break;
            }
        }

        if (visualMesh == null)
        {
            visualMesh = prefabRoot.transform;
        }

        // Check if FirePoint already exists
        Transform existingFirePoint = visualMesh.Find("FirePoint");
        if (existingFirePoint != null)
        {
            EditorUtility.DisplayDialog("Fire Point Exists",
                "A FirePoint already exists on this weapon. Please adjust its position manually in the prefab.",
                "OK");
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            return;
        }

        // Create FirePoint
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(visualMesh);
        // Position at approximate muzzle location (will need manual adjustment)
        firePoint.transform.localPosition = new Vector3(0, 0.05f, 0.15f);
        firePoint.transform.localRotation = Quaternion.identity;

        // Find ShootableAction and configure it
        var shootableAction = prefabRoot.GetComponent<Opsive.UltimateCharacterController.Items.Actions.ShootableAction>();
        if (shootableAction != null)
        {
            // Use SerializedObject to set the fire point
            var so = new SerializedObject(shootableAction);

            // Navigate to the HitscanShooter module's FirePointLocation
            var shooterModuleGroup = so.FindProperty("m_ShooterModuleGroup");
            if (shooterModuleGroup != null)
            {
                var modules = shooterModuleGroup.FindPropertyRelative("m_Modules");
                if (modules != null && modules.arraySize > 0)
                {
                    // The modules use Unity's SerializeReference, so we need to access via the references
                    Debug.Log("Found shooter modules. FirePoint created - please manually assign it in the Shootable Action > Hitscan Shooter > Fire Point Location > Third Person > Object");
                }
            }

            so.ApplyModifiedProperties();
        }

        // Save prefab
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        EditorUtility.DisplayDialog("Fire Point Added",
            "FirePoint child has been added to the weapon.\n\n" +
            "Next steps:\n" +
            "1. Open the prefab\n" +
            "2. Position the FirePoint at the muzzle\n" +
            "3. In Shootable Action > Shooter Module Group > Hitscan Shooter\n" +
            "4. Set Fire Point Location > Third Person > Object to the FirePoint",
            "OK");

        // Select the prefab
        Selection.activeObject = m_WeaponPrefab;
        EditorGUIUtility.PingObject(m_WeaponPrefab);
    }
}
