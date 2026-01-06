using UnityEngine;
using Opsive.UltimateCharacterController.SurfaceSystem;

public class SurfaceDebugger : MonoBehaviour
{
    [SerializeField] private Transform leftToe;
    [SerializeField] private Transform rightToe;
    [SerializeField] private float rayDistance = 0.5f;
    [SerializeField] private LayerMask groundMask = -1;

    private bool useLeftFoot = true;

    private void Start()
    {
        // Auto-find toe bones if not assigned
        if (leftToe == null || rightToe == null)
        {
            var animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                leftToe = animator.GetBoneTransform(HumanBodyBones.LeftToes);
                rightToe = animator.GetBoneTransform(HumanBodyBones.RightToes);

                // Fallback to foot if no toe
                if (leftToe == null)
                    leftToe = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                if (rightToe == null)
                    rightToe = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            DebugCurrentSurface();
            useLeftFoot = !useLeftFoot; // Alternate feet
        }
    }

    private void DebugCurrentSurface()
    {
        Transform foot = useLeftFoot ? leftToe : rightToe;
        if (foot == null)
        {
            Debug.LogError("No foot bone found! Assign manually or check Animator.");
            return;
        }

        Ray ray = new Ray(foot.position + Vector3.up * 0.1f, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, groundMask))
        {
            // Get renderer and material
            Renderer renderer = hit.collider.GetComponent<Renderer>();
            if (renderer == null)
                renderer = hit.collider.GetComponentInChildren<Renderer>();

            if (renderer != null && renderer.sharedMaterial != null)
            {
                var mat = renderer.sharedMaterial;
                Texture mainTex = mat.mainTexture;

                // Try _BaseMap for URP/HDRP
                if (mainTex == null && mat.HasProperty("_BaseMap"))
                    mainTex = mat.GetTexture("_BaseMap");

                Debug.Log($"=== SURFACE DEBUG ===");
                Debug.Log($"Hit Object: {hit.collider.gameObject.name}");
                Debug.Log($"Material: {mat.name}");
                Debug.Log($"Main Texture: {(mainTex != null ? mainTex.name : "NULL")}");

                // Check for SurfaceIdentifier
                var surfaceId = hit.collider.GetComponent<SurfaceIdentifier>();
                if (surfaceId != null)
                {
                    Debug.Log($"SurfaceIdentifier: {surfaceId.SurfaceType?.name ?? "None"}");
                }
                else
                {
                    Debug.Log("No SurfaceIdentifier on object");
                }
            }
            else
            {
                Debug.Log($"Hit {hit.collider.gameObject.name} but no renderer/material found");
            }
        }
        else
        {
            Debug.Log("No ground hit");
        }
    }
}
