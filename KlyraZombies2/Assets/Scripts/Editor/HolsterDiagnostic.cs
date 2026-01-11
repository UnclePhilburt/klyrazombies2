using UnityEngine;
using UnityEditor;
using Opsive.UltimateCharacterController.Objects;
using Opsive.UltimateCharacterController.Items;

public class HolsterDiagnostic : EditorWindow
{
    private Vector2 m_ScrollPos;

    [MenuItem("Tools/Holster Diagnostic")]
    public static void ShowWindow()
    {
        GetWindow<HolsterDiagnostic>("Holster Diagnostic");
    }

    private void OnGUI()
    {
        m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

        GUILayout.Label("Holster Diagnostic", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Show where rifles are
        GUILayout.Label("Weapon Location", EditorStyles.boldLabel);
        if (GUILayout.Button("Find AK-47 / Rifle Location"))
        {
            FindWeaponLocation("AK");
        }
        if (GUILayout.Button("Find SR-9 / Pistol Location"))
        {
            FindWeaponLocation("SR-9");
        }

        GUILayout.Space(10);

        GUILayout.Label("ObjectIdentifier Search", EditorStyles.boldLabel);
        if (GUILayout.Button("Find All ObjectIdentifiers with ID 1002 (Rifle)"))
        {
            FindObjectIdentifiersWithID(1002);
        }

        if (GUILayout.Button("Find All ObjectIdentifiers with ID 1003 (Pistol)"))
        {
            FindObjectIdentifiersWithID(1003);
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Find ALL ObjectIdentifiers in Scene"))
        {
            FindAllObjectIdentifiers();
        }

        GUILayout.Space(20);
        GUILayout.Label("Fix Options", EditorStyles.boldLabel);

        if (GUILayout.Button("Remove Duplicate ID 1002 (Keep First on Sidekick)"))
        {
            RemoveDuplicateIDsPreferSidekick(1002);
        }

        if (GUILayout.Button("Remove Duplicate ID 1003 (Keep First on Sidekick)"))
        {
            RemoveDuplicateIDsPreferSidekick(1003);
        }

        EditorGUILayout.EndScrollView();
    }

    private void FindWeaponLocation(string weaponName)
    {
        Debug.Log($"=== Searching for anything containing '{weaponName}' ===");

        // Search ALL transforms, not just CharacterItem
        var allTransforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        bool found = false;

        foreach (var t in allTransforms)
        {
            if (t.name.Contains(weaponName))
            {
                found = true;
                string path = GetGameObjectPath(t.gameObject);
                Debug.Log($"FOUND: {t.name}", t.gameObject);
                Debug.Log($"  Full Path: {path}");
                Debug.Log($"  Parent: {(t.parent != null ? t.parent.name : "NONE")}");
                Debug.Log($"  World Position: {t.position}");
                Debug.Log($"  Local Position: {t.localPosition}");
                Debug.Log($"  Active Self: {t.gameObject.activeSelf}, Active Hierarchy: {t.gameObject.activeInHierarchy}");

                // Check if parent has ObjectIdentifier
                if (t.parent != null)
                {
                    var parentId = t.parent.GetComponent<ObjectIdentifier>();
                    if (parentId != null)
                    {
                        Debug.Log($"  Parent ObjectIdentifier ID: {parentId.ID}");
                    }
                    else
                    {
                        Debug.Log($"  Parent has NO ObjectIdentifier");
                    }
                }
            }
        }

        if (!found)
        {
            Debug.Log($"No objects found containing '{weaponName}'");
        }
    }

    private void FindObjectIdentifiersWithID(uint targetID)
    {
        var allIdentifiers = FindObjectsByType<ObjectIdentifier>(FindObjectsSortMode.None);
        int count = 0;

        Debug.Log($"=== ObjectIdentifiers with ID {targetID} ===");

        foreach (var id in allIdentifiers)
        {
            if (id.ID == targetID)
            {
                count++;
                string path = GetGameObjectPath(id.gameObject);
                Debug.Log($"[{count}] {path}", id.gameObject);
            }
        }

        Debug.Log($"=== Found {count} ObjectIdentifiers with ID {targetID} ===");

        if (count > 1)
        {
            Debug.LogWarning($"WARNING: Multiple ObjectIdentifiers with same ID {targetID}! Opsive will only use the first one found.");
        }
    }

    private void FindAllObjectIdentifiers()
    {
        var allIdentifiers = FindObjectsByType<ObjectIdentifier>(FindObjectsSortMode.None);

        Debug.Log($"=== ALL ObjectIdentifiers in Scene ({allIdentifiers.Length} total) ===");

        foreach (var id in allIdentifiers)
        {
            string path = GetGameObjectPath(id.gameObject);
            Debug.Log($"ID {id.ID}: {path}", id.gameObject);
        }
    }

    private void RemoveDuplicateIDs(uint targetID)
    {
        var allIdentifiers = FindObjectsByType<ObjectIdentifier>(FindObjectsSortMode.None);
        bool foundFirst = false;
        int removed = 0;

        foreach (var id in allIdentifiers)
        {
            if (id.ID == targetID)
            {
                if (!foundFirst)
                {
                    foundFirst = true;
                    Debug.Log($"Keeping: {GetGameObjectPath(id.gameObject)}", id.gameObject);
                }
                else
                {
                    Debug.Log($"Setting ID to 0: {GetGameObjectPath(id.gameObject)}", id.gameObject);
                    id.ID = 0;
                    EditorUtility.SetDirty(id);
                    removed++;
                }
            }
        }

        Debug.Log($"Removed {removed} duplicate ObjectIdentifiers with ID {targetID}");
    }

    private void RemoveDuplicateIDsPreferSidekick(uint targetID)
    {
        var allIdentifiers = FindObjectsByType<ObjectIdentifier>(FindObjectsSortMode.None);
        ObjectIdentifier sidekickOne = null;
        ObjectIdentifier firstOne = null;
        int removed = 0;

        // First pass - find the one on SidekickCharacter (preferred) or first one
        foreach (var id in allIdentifiers)
        {
            if (id.ID == targetID)
            {
                string path = GetGameObjectPath(id.gameObject);
                if (path.Contains("SidekickCharacter"))
                {
                    sidekickOne = id;
                    Debug.Log($"Found on SidekickCharacter: {path}", id.gameObject);
                }
                else if (firstOne == null)
                {
                    firstOne = id;
                    Debug.Log($"Found (not Sidekick): {path}", id.gameObject);
                }
            }
        }

        // Prefer Sidekick one, otherwise use first found
        ObjectIdentifier keepThis = sidekickOne ?? firstOne;

        if (keepThis == null)
        {
            Debug.LogWarning($"No ObjectIdentifier with ID {targetID} found!");
            return;
        }

        Debug.Log($"KEEPING: {GetGameObjectPath(keepThis.gameObject)}", keepThis.gameObject);

        // Second pass - remove all others
        foreach (var id in allIdentifiers)
        {
            if (id.ID == targetID && id != keepThis)
            {
                Debug.Log($"REMOVING (setting to 0): {GetGameObjectPath(id.gameObject)}", id.gameObject);
                id.ID = 0;
                removed++;
            }
        }

        Debug.Log($"Removed {removed} duplicate ObjectIdentifiers with ID {targetID}");
    }

    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        int depth = 0;

        while (parent != null && depth < 10)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
            depth++;
        }

        return path;
    }
}
