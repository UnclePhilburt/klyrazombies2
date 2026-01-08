using UnityEngine;
using System;

/// <summary>
/// Health component for zombies. Handles damage, death, and headshots.
/// </summary>
public class ZombieHealth : MonoBehaviour, IHealth
{
    [Header("Health")]
    [SerializeField] private float m_MaxHealth = 100f;
    [SerializeField] private float m_CurrentHealth;

    [Header("Damage Modifiers")]
    [SerializeField] private float m_HeadshotMultiplier = 10f;
    [SerializeField] private float m_BodyshotMultiplier = 1f;
    [SerializeField] private float m_LimbshotMultiplier = 0.5f;

    [Header("References")]
    [SerializeField] private ZombieAI m_ZombieAI;
    [SerializeField] private Collider m_HeadCollider;

    [Header("Effects")]
    [SerializeField] private GameObject m_BloodEffectPrefab;
    [SerializeField] private AudioClip[] m_HitSounds;
    [SerializeField] private AudioClip[] m_DeathSounds;

    public event Action<float, float> OnHealthChanged; // current, max
    public event Action OnDeath;

    public float CurrentHealth => m_CurrentHealth;
    public float MaxHealth => m_MaxHealth;
    public bool IsDead => m_CurrentHealth <= 0;

    private AudioSource m_AudioSource;

    private void Awake()
    {
        m_CurrentHealth = m_MaxHealth;
        m_AudioSource = GetComponent<AudioSource>();

        if (m_ZombieAI == null)
            m_ZombieAI = GetComponent<ZombieAI>();
    }

    private void Start()
    {
        // Register with manager
        if (ZombieManager.Instance != null)
        {
            ZombieManager.Instance.RegisterZombie(m_ZombieAI);
        }
    }

    private void OnDestroy()
    {
        if (ZombieManager.Instance != null)
        {
            ZombieManager.Instance.UnregisterZombie(m_ZombieAI);
        }
    }

    public void TakeDamage(float damage, GameObject attacker)
    {
        TakeDamageAtPosition(damage, transform.position + Vector3.up, Vector3.up, attacker);
    }

    private void TakeDamageAtPosition(float damage, Vector3 hitPoint, Vector3 hitNormal, GameObject attacker)
    {
        if (IsDead) return;

        m_CurrentHealth -= damage;
        m_CurrentHealth = Mathf.Max(0, m_CurrentHealth);

        OnHealthChanged?.Invoke(m_CurrentHealth, m_MaxHealth);

        // Play hit sound
        PlaySound(m_HitSounds);

        // Spawn blood particle burst on hit (no ground pools until death)
        if (BloodEffectManager.Instance != null)
        {
            BloodEffectManager.Instance.SpawnBloodBurst(hitPoint, hitNormal);
        }
        // Fallback to prefab if assigned
        else if (m_BloodEffectPrefab != null)
        {
            Instantiate(m_BloodEffectPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
        }

        // Aggro on attacker
        if (m_ZombieAI != null && attacker != null)
        {
            m_ZombieAI.OnDamaged(attacker);
        }

        if (m_CurrentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Take damage with hit location for headshot detection
    /// </summary>
    public void TakeDamageAtPoint(float baseDamage, Vector3 hitPoint, Collider hitCollider, GameObject attacker)
    {
        if (IsDead) return;

        float multiplier = m_BodyshotMultiplier;

        // Check for headshot
        if (m_HeadCollider != null && hitCollider == m_HeadCollider)
        {
            multiplier = m_HeadshotMultiplier;
            Debug.Log("[ZombieHealth] HEADSHOT!");
        }
        else
        {
            // Check if hit was high on the body (approximate headshot)
            float hitHeight = hitPoint.y - transform.position.y;
            float zombieHeight = 1.8f; // Approximate height

            if (hitHeight > zombieHeight * 0.8f)
            {
                multiplier = m_HeadshotMultiplier;
                Debug.Log("[ZombieHealth] HEADSHOT (height-based)!");
            }
            else if (hitHeight < zombieHeight * 0.3f)
            {
                multiplier = m_LimbshotMultiplier;
            }
        }

        // Calculate hit normal (direction from zombie center to hit point)
        Vector3 hitNormal = (hitPoint - transform.position).normalized;
        if (hitNormal.sqrMagnitude < 0.01f)
            hitNormal = Vector3.up;

        float finalDamage = baseDamage * multiplier;
        TakeDamageAtPosition(finalDamage, hitPoint, hitNormal, attacker);
    }

    private void Die()
    {
        if (m_ZombieAI != null)
        {
            m_ZombieAI.Die();
        }

        PlaySound(m_DeathSounds);

        // Spawn multiple large blood pools on death
        if (BloodEffectManager.Instance != null)
        {
            // Spawn 3-5 pools around the body
            int poolCount = UnityEngine.Random.Range(3, 6);
            for (int i = 0; i < poolCount; i++)
            {
                Vector3 offset = new Vector3(
                    UnityEngine.Random.Range(-0.5f, 0.5f),
                    0f,
                    UnityEngine.Random.Range(-0.5f, 0.5f)
                );
                BloodEffectManager.Instance.SpawnBloodPool(transform.position + offset, true); // true = large pool
            }
        }

        OnDeath?.Invoke();
    }

    private void PlaySound(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;

        var clip = clips[UnityEngine.Random.Range(0, clips.Length)];
        if (clip != null && m_AudioSource != null)
        {
            m_AudioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Heal the zombie (for special zombies or debugging)
    /// </summary>
    public void Heal(float amount)
    {
        if (IsDead) return;

        m_CurrentHealth = Mathf.Min(m_CurrentHealth + amount, m_MaxHealth);
        OnHealthChanged?.Invoke(m_CurrentHealth, m_MaxHealth);
    }

    /// <summary>
    /// Set max health (for zombie variants)
    /// </summary>
    public void SetMaxHealth(float maxHealth, bool healToFull = true)
    {
        m_MaxHealth = maxHealth;
        if (healToFull)
        {
            m_CurrentHealth = m_MaxHealth;
        }
        OnHealthChanged?.Invoke(m_CurrentHealth, m_MaxHealth);
    }
}
