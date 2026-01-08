using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Central manager for zombie systems.
/// Handles gunshot events, zombie tracking, and global settings.
/// </summary>
public class ZombieManager : MonoBehaviour
{
    public static ZombieManager Instance { get; private set; }

    // Event for gunshots that zombies listen to
    public static event Action<Vector3, float> OnGunshotFired;

    [Header("Global Settings")]
    [SerializeField] private int m_MaxZombiesInWorld = 100;
    [SerializeField] private float m_DespawnDistance = 100f;
    [SerializeField] private bool m_DebugMode = false;

    [Header("Gunshot Settings")]
    [SerializeField] private float m_GunshotBaseRange = 50f;
    [SerializeField] private float m_SilencedMultiplier = 0.2f;

    // Track all active zombies
    private List<ZombieAI> m_ActiveZombies = new List<ZombieAI>();

    public int ActiveZombieCount => m_ActiveZombies.Count;
    public int MaxZombies => m_MaxZombiesInWorld;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Call this when a gun is fired to alert nearby zombies
    /// </summary>
    public static void FireGunshot(Vector3 position, bool silenced = false)
    {
        if (Instance == null) return;

        float range = Instance.m_GunshotBaseRange;
        if (silenced)
            range *= Instance.m_SilencedMultiplier;

        if (Instance.m_DebugMode)
            Debug.Log($"[ZombieManager] Gunshot at {position}, range: {range}");

        OnGunshotFired?.Invoke(position, range);
    }

    /// <summary>
    /// Register a zombie with the manager
    /// </summary>
    public void RegisterZombie(ZombieAI zombie)
    {
        if (!m_ActiveZombies.Contains(zombie))
        {
            m_ActiveZombies.Add(zombie);
        }
    }

    /// <summary>
    /// Unregister a zombie (when it dies or despawns)
    /// </summary>
    public void UnregisterZombie(ZombieAI zombie)
    {
        m_ActiveZombies.Remove(zombie);
    }

    /// <summary>
    /// Check if we can spawn more zombies
    /// </summary>
    public bool CanSpawnZombie()
    {
        return m_ActiveZombies.Count < m_MaxZombiesInWorld;
    }

    /// <summary>
    /// Get all zombies within range of a position
    /// </summary>
    public List<ZombieAI> GetZombiesInRange(Vector3 position, float range)
    {
        var result = new List<ZombieAI>();
        float rangeSqr = range * range;

        foreach (var zombie in m_ActiveZombies)
        {
            if (zombie == null) continue;
            if ((zombie.transform.position - position).sqrMagnitude <= rangeSqr)
            {
                result.Add(zombie);
            }
        }

        return result;
    }

    /// <summary>
    /// Clean up null references (dead zombies)
    /// </summary>
    private void LateUpdate()
    {
        m_ActiveZombies.RemoveAll(z => z == null);
    }
}
