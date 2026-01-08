using UnityEngine;
using Opsive.Shared.Game;
using Opsive.UltimateCharacterController.Traits;
using Opsive.UltimateCharacterController.Traits.Damage;

/// <summary>
/// Bridge between Opsive's damage system and our ZombieHealth.
/// Add this component to zombies so Opsive weapons can damage them.
/// </summary>
[RequireComponent(typeof(ZombieHealth))]
public class ZombieDamageBridge : MonoBehaviour, IDamageTarget
{
    private ZombieHealth m_ZombieHealth;

    public GameObject Owner => gameObject;
    public GameObject HitGameObject => gameObject;
    public bool Invincible { get; set; } = false;

    private void Awake()
    {
        m_ZombieHealth = GetComponent<ZombieHealth>();
    }

    /// <summary>
    /// Called by Opsive weapons when they hit this object
    /// </summary>
    public void Damage(DamageData damageData)
    {
        if (m_ZombieHealth == null || m_ZombieHealth.IsDead || Invincible)
            return;

        Debug.Log($"[ZombieDamageBridge] {gameObject.name} received {damageData.Amount} damage from Opsive weapon");

        // Get hit info
        Vector3 hitPoint = damageData.Position;
        Collider hitCollider = damageData.HitCollider;

        // Find the player as the attacker (most likely source of damage)
        GameObject attacker = GameObject.FindGameObjectWithTag("Player");

        // Apply damage through our health system
        if (hitCollider != null)
        {
            m_ZombieHealth.TakeDamageAtPoint(damageData.Amount, hitPoint, hitCollider, attacker);
        }
        else
        {
            m_ZombieHealth.TakeDamage(damageData.Amount, attacker);
        }
    }

    /// <summary>
    /// Check if this target is alive
    /// </summary>
    public bool IsAlive()
    {
        return m_ZombieHealth != null && !m_ZombieHealth.IsDead;
    }

    /// <summary>
    /// Heal the target (required by IDamageTarget interface)
    /// </summary>
    public bool Heal(float amount)
    {
        if (m_ZombieHealth != null && !m_ZombieHealth.IsDead)
        {
            m_ZombieHealth.Heal(amount);
            return true;
        }
        return false;
    }
}
