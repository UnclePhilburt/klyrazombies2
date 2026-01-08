using UnityEngine;

/// <summary>
/// Manages blood effects - splatters on hit, pools on death.
/// Place in scene or use as singleton.
/// </summary>
public class BloodEffectManager : MonoBehaviour
{
    public static BloodEffectManager Instance { get; private set; }

    [Header("Blood Splatter (On Hit)")]
    [SerializeField] private GameObject m_BloodSplatterPrefab;
    [SerializeField] private int m_SplatterPoolSize = 20;
    [SerializeField] private float m_SplatterLifetime = 2f;

    [Header("Blood Pool (On Death)")]
    [SerializeField] private GameObject m_BloodPoolPrefab;
    [SerializeField] private GameObject[] m_BloodPoolPrefabs; // Multiple variants for variety
    [SerializeField] private int m_PoolPoolSize = 10;
    [SerializeField] private float m_PoolLifetime = 30f;
    [SerializeField] private float m_PoolGrowDuration = 2f;

    [Header("Audio")]
    [SerializeField] private AudioClip[] m_BloodSplashSounds;
    [SerializeField] private float m_SplashVolume = 0.5f;

    [Header("Auto-Generate Effects")]
    [SerializeField] private bool m_AutoGenerateIfMissing = true;
    [SerializeField] private Color m_BloodColor = new Color(0.5f, 0f, 0f, 1f);

    private GameObject[] m_SplatterPool;
    private GameObject[] m_PoolPool;
    private int m_SplatterIndex;
    private int m_PoolIndex;
    private AudioSource m_AudioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Auto-generate prefabs if not assigned
        if (m_AutoGenerateIfMissing)
        {
            if (m_BloodSplatterPrefab == null)
                m_BloodSplatterPrefab = CreateBloodSplatterPrefab();
            if (m_BloodPoolPrefab == null)
                m_BloodPoolPrefab = CreateBloodPoolPrefab();
        }

        // Setup audio source
        m_AudioSource = GetComponent<AudioSource>();
        if (m_AudioSource == null)
            m_AudioSource = gameObject.AddComponent<AudioSource>();
        m_AudioSource.spatialBlend = 0f; // 2D sound

        InitializePools();
    }

    private void InitializePools()
    {
        // Initialize splatter pool (only if we have a prefab and want pooling)
        if (m_BloodSplatterPrefab != null && m_SplatterPoolSize > 0)
        {
            // Check if it's a particle system - only pool particle systems
            var testPS = m_BloodSplatterPrefab.GetComponent<ParticleSystem>();
            if (testPS != null)
            {
                m_SplatterPool = new GameObject[m_SplatterPoolSize];
                for (int i = 0; i < m_SplatterPoolSize; i++)
                {
                    m_SplatterPool[i] = Instantiate(m_BloodSplatterPrefab, transform);
                    m_SplatterPool[i].SetActive(false);
                }
            }
            // For non-particle splatters, we'll instantiate directly
        }

        // Initialize pool pool only if we don't have pool variants
        // (If we have m_BloodPoolPrefabs, we'll instantiate directly for variety)
        if (m_BloodPoolPrefab != null && (m_BloodPoolPrefabs == null || m_BloodPoolPrefabs.Length == 0))
        {
            m_PoolPool = new GameObject[m_PoolPoolSize];
            for (int i = 0; i < m_PoolPoolSize; i++)
            {
                m_PoolPool[i] = Instantiate(m_BloodPoolPrefab, transform);
                m_PoolPool[i].SetActive(false);
            }
        }
    }

    /// <summary>
    /// Spawn blood particle burst at hit point (particles fly out and disappear)
    /// </summary>
    public void SpawnBloodBurst(Vector3 position, Vector3 direction)
    {
        if (m_BloodSplatterPrefab == null) return;

        // Spawn at hit position, facing outward from zombie
        Quaternion rotation = direction.sqrMagnitude > 0.01f
            ? Quaternion.LookRotation(direction)
            : Quaternion.identity;

        GameObject burst = Instantiate(m_BloodSplatterPrefab, position, rotation);
        DisableColliders(burst);

        var ps = burst.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
        }

        // Short lifetime - just for the particle burst
        Destroy(burst, 2f);

        PlayRandomSound(m_BloodSplashSounds, m_SplashVolume);
    }

    private void PlayRandomSound(AudioClip[] clips, float volume)
    {
        if (clips == null || clips.Length == 0 || m_AudioSource == null) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip != null)
        {
            m_AudioSource.PlayOneShot(clip, volume);
        }
    }

    /// <summary>
    /// Spawn blood pool at position
    /// </summary>
    /// <param name="position">World position</param>
    /// <param name="large">If true, spawn larger pool (for death)</param>
    public void SpawnBloodPool(Vector3 position, bool large = false)
    {
        // Use pool variants if available, otherwise use object pool
        if (m_BloodPoolPrefabs != null && m_BloodPoolPrefabs.Length > 0)
        {
            // Spawn a random pool variant directly
            GameObject prefab = m_BloodPoolPrefabs[Random.Range(0, m_BloodPoolPrefabs.Length)];
            if (prefab != null)
            {
                // Raycast down to find ground
                if (Physics.Raycast(position + Vector3.up, Vector3.down, out RaycastHit hit, 3f))
                {
                    position = hit.point + Vector3.up * 0.02f; // Slightly above ground
                }

                GameObject pool = Instantiate(prefab, position, Quaternion.Euler(0, Random.Range(0f, 360f), 0));

                // Disable all colliders so blood doesn't block player
                DisableColliders(pool);

                // Scale based on whether it's a death pool or hit pool
                float scale;
                if (large)
                {
                    scale = Random.Range(1.5f, 2.5f); // Large pools for death
                }
                else
                {
                    scale = Random.Range(0.4f, 0.8f); // Smaller pools for hits
                }
                pool.transform.localScale = Vector3.one * scale;

                // Destroy after lifetime
                Destroy(pool, m_PoolLifetime);
            }
            return;
        }

        // Fallback to object pool
        if (m_PoolPool == null || m_PoolPool.Length == 0) return;

        GameObject poolObj = m_PoolPool[m_PoolIndex];
        m_PoolIndex = (m_PoolIndex + 1) % m_PoolPool.Length;

        // Raycast down to find ground
        if (Physics.Raycast(position + Vector3.up, Vector3.down, out RaycastHit groundHit, 3f))
        {
            position = groundHit.point + Vector3.up * 0.01f;
        }

        poolObj.transform.position = position;
        poolObj.transform.rotation = Quaternion.Euler(90, Random.Range(0f, 360f), 0);
        poolObj.transform.localScale = Vector3.zero;
        poolObj.SetActive(true);

        // Animate pool growing
        float targetScale = large ? Random.Range(1.5f, 2.5f) : Random.Range(0.5f, 1f);
        StartCoroutine(GrowPool(poolObj, targetScale));

        // Auto-hide after lifetime
        StartCoroutine(HideAfterDelay(poolObj, m_PoolLifetime));
    }

    private System.Collections.IEnumerator GrowPool(GameObject pool, float targetScaleValue = 1f)
    {
        float elapsed = 0f;
        Vector3 targetScale = Vector3.one * targetScaleValue;

        while (elapsed < m_PoolGrowDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / m_PoolGrowDuration;
            pool.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            yield return null;
        }

        pool.transform.localScale = targetScale;
    }

    private System.Collections.IEnumerator HideAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null)
            obj.SetActive(false);
    }

    /// <summary>
    /// Disable all colliders on an object so it doesn't block the player
    /// </summary>
    private void DisableColliders(GameObject obj)
    {
        // Disable all colliders on the object and children
        Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }
    }

    #region Auto-Generate Prefabs

    private GameObject CreateBloodSplatterPrefab()
    {
        GameObject splatter = new GameObject("BloodSplatter_Generated");
        splatter.transform.SetParent(transform);

        // Add particle system
        var ps = splatter.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = 0.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor = m_BloodColor;
        main.gravityModifier = 1f;
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 20, 40)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.1f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(m_BloodColor, 0f), new GradientColorKey(m_BloodColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = gradient;

        // Use default particle material
        var renderer = splatter.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetDefaultParticleMaterial();

        splatter.SetActive(false);
        return splatter;
    }

    private GameObject CreateBloodPoolPrefab()
    {
        GameObject pool = new GameObject("BloodPool_Generated");
        pool.transform.SetParent(transform);

        // Create a quad for the pool
        var meshFilter = pool.AddComponent<MeshFilter>();
        meshFilter.mesh = CreateQuadMesh();

        var meshRenderer = pool.AddComponent<MeshRenderer>();

        // Create blood material
        Material bloodMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (bloodMat.shader == null)
            bloodMat = new Material(Shader.Find("Unlit/Color"));

        bloodMat.color = m_BloodColor;
        meshRenderer.material = bloodMat;

        // Set shadows off
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        pool.SetActive(false);
        return pool;
    }

    private Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "BloodPoolQuad";

        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-0.5f, 0, -0.5f),
            new Vector3(0.5f, 0, -0.5f),
            new Vector3(-0.5f, 0, 0.5f),
            new Vector3(0.5f, 0, 0.5f)
        };

        int[] tris = new int[6] { 0, 2, 1, 2, 3, 1 };

        Vector3[] normals = new Vector3[4]
        {
            Vector3.up, Vector3.up, Vector3.up, Vector3.up
        };

        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.normals = normals;
        mesh.uv = uv;

        return mesh;
    }

    private Material GetDefaultParticleMaterial()
    {
        // Try to find a particle material
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        if (mat.shader == null)
            mat = new Material(Shader.Find("Particles/Standard Unlit"));
        if (mat.shader == null)
            mat = new Material(Shader.Find("Unlit/Color"));

        mat.color = m_BloodColor;
        return mat;
    }

    #endregion
}
