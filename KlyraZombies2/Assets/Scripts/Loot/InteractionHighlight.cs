using UnityEngine;
using Opsive.UltimateInventorySystem.Core.InventoryCollections;

/// <summary>
/// Proximity-based interaction highlighting.
/// Creates a trigger zone - when player enters, object highlights.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class InteractionHighlight : MonoBehaviour
{
    // Static tracking - only one icon shows at a time
    private static InteractionHighlight s_CurrentTarget;
    private static readonly System.Collections.Generic.HashSet<InteractionHighlight> s_ActiveHighlights = new();

    [Header("Detection Settings")]
    [Tooltip("Radius of the trigger zone")]
    [SerializeField] private float m_TriggerRadius = 2.5f;

    [Tooltip("Layer mask for the player")]
    [SerializeField] private LayerMask m_PlayerLayer = 1 << 31;

    [Header("Icon Settings")]
    [Tooltip("The search icon sprite to display")]
    [SerializeField] private Sprite m_SearchIcon;

    [Tooltip("Size of the icon in world units")]
    [SerializeField] private float m_IconSize = 0.5f;

    [Tooltip("Vertical offset above object center")]
    [SerializeField] private float m_IconHeightOffset = 0.5f;

    [Header("Outline Settings")]
    [Tooltip("Outline color when container has loot")]
    [SerializeField] private Color m_OutlineColor = new Color(0.3f, 0.7f, 1f, 0.8f);

    [Tooltip("Outline color when container is empty/looted")]
    [SerializeField] private Color m_LootedColor = new Color(0.8f, 0.3f, 0.3f, 0.8f);

    [Tooltip("Outline thickness (1.01 = thin, 1.1 = thick)")]
    [Range(1.01f, 1.2f)]
    [SerializeField] private float m_OutlineThickness = 1.05f;

    [Header("Crosshair Detection")]
    [Tooltip("Radius of the spherecast for icon targeting")]
    [SerializeField] private float m_CrosshairRadius = 0.3f;

    // Runtime references
    private Camera m_PlayerCamera;
    private GameObject m_IconObject;
    private SpriteRenderer m_IconRenderer;
    private GameObject m_OutlineObject;
    private Renderer[] m_OriginalRenderers;
    private Material m_OutlineMaterial;
    private bool m_IsHighlighted = false;
    private bool m_IsTargeted = false;
    private Bounds m_ObjectBounds;
    private Inventory m_Inventory;
    private bool m_IsLooted = false;
    private bool m_HasBeenOpened = false;
    private SphereCollider m_TriggerCollider;
    private int m_IgnoreRaycastLayers;

    private void Start()
    {
        // Setup trigger collider
        SetupTrigger();

        // Find inventory on this object or parent
        FindInventory();

        // Calculate object bounds for icon positioning
        CalculateBounds();

        // Create the icon
        CreateIcon();

        // Create outline effect
        CreateOutline();

        // Setup ignore layers for raycast
        int playerLayer = LayerMask.NameToLayer("Player");
        int characterLayer = LayerMask.NameToLayer("Character");
        m_IgnoreRaycastLayers = (1 << playerLayer) | (1 << characterLayer);

        // Start hidden
        SetHighlightActive(false);
    }

    private void SetupTrigger()
    {
        m_TriggerCollider = GetComponent<SphereCollider>();
        if (m_TriggerCollider == null)
        {
            m_TriggerCollider = gameObject.AddComponent<SphereCollider>();
        }

        m_TriggerCollider.isTrigger = true;
        m_TriggerCollider.radius = m_TriggerRadius;
        m_TriggerCollider.center = Vector3.zero;
    }

    private void FindInventory()
    {
        m_Inventory = GetComponent<Inventory>();
        if (m_Inventory == null)
        {
            m_Inventory = GetComponentInParent<Inventory>();
        }
    }

    /// <summary>
    /// Call this when the container is opened by the player.
    /// </summary>
    public void MarkAsOpened()
    {
        m_HasBeenOpened = true;
        UpdateLootedState();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if it's the player
        if (IsPlayer(other))
        {
            s_ActiveHighlights.Add(this);
            SetHighlightActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if it's the player
        if (IsPlayer(other))
        {
            s_ActiveHighlights.Remove(this);
            if (s_CurrentTarget == this)
            {
                s_CurrentTarget = null;
            }
            SetHighlightActive(false);
        }
    }

    private bool IsPlayer(Collider other)
    {
        // Check layer mask
        if (((1 << other.gameObject.layer) & m_PlayerLayer) != 0)
        {
            return true;
        }

        // Fallback: check common player layer names
        int playerLayer = LayerMask.NameToLayer("Player");
        int characterLayer = LayerMask.NameToLayer("Character");

        return other.gameObject.layer == playerLayer || other.gameObject.layer == characterLayer;
    }

    private void CalculateBounds()
    {
        m_ObjectBounds = new Bounds(transform.position, Vector3.zero);

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            if (renderer is SpriteRenderer) continue;
            m_ObjectBounds.Encapsulate(renderer.bounds);
        }

        if (m_ObjectBounds.size == Vector3.zero)
        {
            Collider[] cols = GetComponentsInChildren<Collider>();
            foreach (var col in cols)
            {
                if (col.isTrigger) continue; // Skip our trigger
                m_ObjectBounds.Encapsulate(col.bounds);
            }

            if (m_ObjectBounds.size == Vector3.zero)
            {
                m_ObjectBounds = new Bounds(transform.position, Vector3.one);
            }
        }
    }

    private void CreateIcon()
    {
        if (m_SearchIcon == null)
        {
            m_SearchIcon = Resources.Load<Sprite>("SearchIcon");
            if (m_SearchIcon == null)
            {
                m_SearchIcon = Resources.Load<Sprite>("Icons/Search");
            }
            if (m_SearchIcon == null)
            {
                // Create a fallback circle sprite
                m_SearchIcon = CreateCircleSprite(64, Color.white);
                Debug.Log("[InteractionHighlight] Created fallback circle sprite");
            }
        }

        m_IconObject = new GameObject("SearchIcon");
        m_IconObject.transform.SetParent(transform);
        m_IconObject.transform.localPosition = Vector3.up * (m_ObjectBounds.extents.y + m_IconHeightOffset);

        m_IconRenderer = m_IconObject.AddComponent<SpriteRenderer>();
        m_IconRenderer.sprite = m_SearchIcon;
        m_IconRenderer.sortingOrder = 32767;
        m_IconRenderer.color = Color.white;

        Material iconMaterial = CreateOverlaySpriteMaterial();
        if (iconMaterial != null)
        {
            m_IconRenderer.material = iconMaterial;
        }

        if (m_SearchIcon != null)
        {
            float pixelsPerUnit = m_SearchIcon.pixelsPerUnit;
            float spriteSize = m_SearchIcon.rect.width / pixelsPerUnit;
            float scale = m_IconSize / spriteSize;
            m_IconObject.transform.localScale = Vector3.one * scale;
        }
        else
        {
            m_IconObject.transform.localScale = Vector3.one * m_IconSize;
        }
    }

    private Sprite CreateCircleSprite(int size, Color color)
    {
        // Create a magnifying glass icon
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

        // Clear the texture
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, Color.clear);

        float center = size * 0.38f; // Offset center for the glass part
        float glassRadius = size * 0.32f;
        float ringThickness = size * 0.08f;

        // Draw the magnifying glass circle (ring)
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - (size - center);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Outer edge of ring
                if (dist <= glassRadius + ringThickness / 2 && dist >= glassRadius - ringThickness / 2)
                {
                    float edgeDist = Mathf.Min(
                        Mathf.Abs(dist - (glassRadius - ringThickness / 2)),
                        Mathf.Abs(dist - (glassRadius + ringThickness / 2))
                    );
                    float alpha = Mathf.Clamp01(edgeDist / 1.5f + 0.3f);
                    texture.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha));
                }
                // Glass interior (slightly transparent)
                else if (dist < glassRadius - ringThickness / 2)
                {
                    texture.SetPixel(x, y, new Color(color.r, color.g, color.b, 0.15f));
                }
            }
        }

        // Draw the handle (diagonal line from bottom-left of glass)
        float handleStartX = center - glassRadius * 0.7f;
        float handleStartY = size - center - glassRadius * 0.7f;
        float handleLength = size * 0.35f;
        float handleThickness = size * 0.1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Distance from handle line (45 degree angle going to bottom-left)
                float px = x - handleStartX;
                float py = y - handleStartY;

                // Project onto handle direction (normalized: -0.707, -0.707)
                float proj = (-px - py) * 0.707f;

                if (proj > 0 && proj < handleLength)
                {
                    // Distance from handle center line
                    float perpDist = Mathf.Abs((-py + px) * 0.707f);

                    if (perpDist < handleThickness / 2)
                    {
                        float alpha = Mathf.Clamp01(1f - perpDist / (handleThickness / 2) * 0.3f);
                        Color existing = texture.GetPixel(x, y);
                        if (existing.a < alpha)
                            texture.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha));
                    }
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Material CreateOverlaySpriteMaterial()
    {
        Shader overlayShader = Shader.Find("Klyra/SpriteOverlay");
        if (overlayShader == null)
        {
            overlayShader = Shader.Find("Sprites/Default");
        }

        if (overlayShader != null)
        {
            return new Material(overlayShader);
        }
        return null;
    }

    private void CreateOutline()
    {
        m_OriginalRenderers = GetComponentsInChildren<Renderer>();

        var validRenderers = new System.Collections.Generic.List<Renderer>();
        foreach (var r in m_OriginalRenderers)
        {
            if (r is SpriteRenderer) continue;
            if (r is ParticleSystemRenderer) continue;
            if (r is TrailRenderer) continue;
            if (r is LineRenderer) continue;
            validRenderers.Add(r);
        }
        m_OriginalRenderers = validRenderers.ToArray();

        if (m_OriginalRenderers.Length == 0)
        {
            return;
        }

        m_OutlineMaterial = CreateOutlineMaterial();

        m_OutlineObject = new GameObject("OutlineEffect");
        m_OutlineObject.transform.SetParent(transform);
        m_OutlineObject.transform.localPosition = Vector3.zero;
        m_OutlineObject.transform.localRotation = Quaternion.identity;
        m_OutlineObject.transform.localScale = Vector3.one;

        foreach (var renderer in m_OriginalRenderers)
        {
            Mesh meshToUse = null;

            if (renderer is MeshRenderer)
            {
                MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    meshToUse = meshFilter.sharedMesh;
                }
            }
            else if (renderer is SkinnedMeshRenderer)
            {
                // Skip skinned meshes - they deform with animation/ragdoll
                // and would show T-pose outline instead of current pose
                continue;
            }

            if (meshToUse == null) continue;

            GameObject outlineMesh = new GameObject("Outline_" + renderer.name);
            outlineMesh.transform.SetParent(m_OutlineObject.transform);
            outlineMesh.transform.position = renderer.transform.position;
            outlineMesh.transform.rotation = renderer.transform.rotation;
            outlineMesh.transform.localScale = renderer.transform.lossyScale * m_OutlineThickness;

            MeshFilter outlineFilter = outlineMesh.AddComponent<MeshFilter>();
            outlineFilter.sharedMesh = meshToUse;

            MeshRenderer outlineRenderer = outlineMesh.AddComponent<MeshRenderer>();
            outlineRenderer.sharedMaterial = m_OutlineMaterial;
            outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
        }
    }

    private Material CreateOutlineMaterial()
    {
        Shader outlineShader = Shader.Find("Klyra/OutlineEffect");
        if (outlineShader == null)
        {
            outlineShader = Shader.Find("Unlit/Color");
            if (outlineShader == null)
            {
                outlineShader = Shader.Find("Universal Render Pipeline/Unlit");
            }
        }

        Material mat = new Material(outlineShader);
        mat.SetColor("_OutlineColor", m_OutlineColor);
        mat.SetColor("_Color", m_OutlineColor);
        mat.SetColor("_BaseColor", m_OutlineColor);

        return mat;
    }

    private void Update()
    {
        if (m_PlayerCamera == null)
        {
            m_PlayerCamera = Camera.main;
        }

        // Update looted state
        if (m_IsHighlighted)
        {
            UpdateLootedState();

            // Determine which highlight (if any) the crosshair is targeting
            InteractionHighlight newTarget = GetCrosshairTarget();

            // Update targeting state - only this instance if it's the new target
            bool wasTargeted = m_IsTargeted;
            bool shouldBeTargeted = (newTarget == this);

            // If target changed globally, update the old target
            if (s_CurrentTarget != newTarget)
            {
                if (s_CurrentTarget != null && s_CurrentTarget.m_IsTargeted)
                {
                    s_CurrentTarget.m_IsTargeted = false;
                    s_CurrentTarget.UpdateIconVisibility();
                }
                s_CurrentTarget = newTarget;
            }

            // Update this instance's targeted state
            if (shouldBeTargeted != wasTargeted)
            {
                m_IsTargeted = shouldBeTargeted;
                UpdateIconVisibility();
            }
        }

        // Billboard the icon to face camera
        if (m_IsTargeted && m_IconObject != null && m_PlayerCamera != null)
        {
            m_IconObject.transform.LookAt(
                m_IconObject.transform.position + m_PlayerCamera.transform.forward,
                Vector3.up
            );
        }
    }

    /// <summary>
    /// Returns the InteractionHighlight that the crosshair is currently pointing at (from active highlights only).
    /// </summary>
    private InteractionHighlight GetCrosshairTarget()
    {
        if (m_PlayerCamera == null) return null;

        Ray ray = m_PlayerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        int layerMask = ~m_IgnoreRaycastLayers;

        if (Physics.SphereCast(ray, m_CrosshairRadius, out RaycastHit hit, 10f, layerMask, QueryTriggerInteraction.Ignore))
        {
            // Check parent hierarchy
            var hitHighlight = hit.transform.GetComponentInParent<InteractionHighlight>();
            if (hitHighlight != null && s_ActiveHighlights.Contains(hitHighlight))
            {
                return hitHighlight;
            }

            // Check children (if we hit a parent of the highlight object)
            hitHighlight = hit.transform.GetComponentInChildren<InteractionHighlight>();
            if (hitHighlight != null && s_ActiveHighlights.Contains(hitHighlight))
            {
                return hitHighlight;
            }

            // Check if hit object is sibling/cousin (same root) - find matching active highlight
            Transform hitRoot = hit.transform.root;
            foreach (var highlight in s_ActiveHighlights)
            {
                if (highlight != null && highlight.transform.root == hitRoot)
                {
                    return highlight;
                }
            }
        }

        return null;
    }

    private void UpdateIconVisibility()
    {
        if (m_IconObject != null)
        {
            m_IconObject.SetActive(m_IsTargeted);
        }

        // Update crosshair color
        if (Crosshair.Instance != null)
        {
            Crosshair.Instance.SetHighlighted(m_IsTargeted);
        }
    }

    private void UpdateLootedState()
    {
        if (m_Inventory == null) return;

        if (!m_HasBeenOpened)
        {
            if (m_IsLooted)
            {
                m_IsLooted = false;
                UpdateOutlineColor();
            }
            return;
        }

        bool isEmpty = true;
        var mainCollection = m_Inventory.MainItemCollection;

        if (mainCollection != null && mainCollection.GetAllItemStacks().Count > 0)
        {
            isEmpty = false;
        }

        if (isEmpty != m_IsLooted)
        {
            m_IsLooted = isEmpty;
            UpdateOutlineColor();
        }
    }

    private void UpdateOutlineColor()
    {
        if (m_OutlineMaterial == null) return;

        Color targetColor = m_IsLooted ? m_LootedColor : m_OutlineColor;

        m_OutlineMaterial.SetColor("_OutlineColor", targetColor);
        m_OutlineMaterial.SetColor("_Color", targetColor);
        m_OutlineMaterial.SetColor("_BaseColor", targetColor);
    }

    public void SetHighlightActive(bool active)
    {
        m_IsHighlighted = active;

        // Outline shows when in proximity
        if (m_OutlineObject != null)
        {
            m_OutlineObject.SetActive(active);
        }

        // Icon only shows when targeted by crosshair (reset when leaving proximity)
        if (!active)
        {
            m_IsTargeted = false;
            if (m_IconObject != null)
            {
                m_IconObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Force the icon to show/hide (used by LootableInteraction for proximity-based detection)
    /// </summary>
    public void ForceShowIcon(bool show)
    {
        Debug.Log($"[InteractionHighlight] ForceShowIcon({show}) on {gameObject.name}, m_IconObject: {(m_IconObject != null ? "EXISTS" : "NULL")}");

        m_IsTargeted = show;

        // If icon hasn't been created yet (Start() hasn't run), create it now
        if (m_IconObject == null && show)
        {
            Debug.Log($"[InteractionHighlight] Creating icon for {gameObject.name}");
            // Initialize everything we need
            m_PlayerCamera = Camera.main;
            CalculateBounds();
            CreateIcon();
            Debug.Log($"[InteractionHighlight] Icon created: {(m_IconObject != null ? "SUCCESS" : "FAILED")}, sprite: {(m_SearchIcon != null ? m_SearchIcon.name : "NULL")}");
        }

        if (m_IconObject != null)
        {
            m_IconObject.SetActive(show);
            Debug.Log($"[InteractionHighlight] Icon active: {m_IconObject.activeSelf}, position: {m_IconObject.transform.position}");
        }
    }

    private void OnDestroy()
    {
        // Clean up static tracking
        s_ActiveHighlights.Remove(this);
        if (s_CurrentTarget == this)
        {
            s_CurrentTarget = null;
        }

        if (m_OutlineMaterial != null)
        {
            Destroy(m_OutlineMaterial);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize trigger radius
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, m_TriggerRadius);
    }
}
