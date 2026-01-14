using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Dog companion AI - follows player, attacks on command, silent melee alternative to guns.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class DogCompanion : MonoBehaviour
{
    public enum DogState
    {
        Following,
        Staying,
        Attacking,
        Returning,
        Searching,
        Idle
    }

    [Header("References")]
    [SerializeField] private Transform m_Owner;
    [SerializeField] private Animator m_Animator;

    [Header("Following")]
    [SerializeField] private float m_FollowDistance = 2.5f;
    [SerializeField] private float m_TeleportDistance = 30f;
    [SerializeField] private float m_RunDistance = 8f;
    [SerializeField] private float m_WalkSpeed = 2f;
    [SerializeField] private float m_RunSpeed = 6f;

    [Header("Combat")]
    [SerializeField] private float m_AttackRange = 1.5f;
    [SerializeField] private float m_AttackDamage = 50f;
    [SerializeField] private float m_AttackCooldown = 2f;
    [SerializeField] private float m_MaxAttackDistance = 25f;

    [Header("Search")]
    [SerializeField] private float m_SearchRadius = 10f;
    [SerializeField] private float m_SearchDuration = 3f;

    [Header("Audio")]
    [SerializeField] private AudioClip[] m_BarkClips;
    [SerializeField] private AudioClip[] m_GrowlClips;
    [SerializeField] private AudioClip[] m_WhineClips;
    [SerializeField] private AudioSource m_AudioSource;

    // State
    private DogState m_CurrentState = DogState.Following;
    private NavMeshAgent m_Agent;
    private Transform m_AttackTarget;
    private float m_LastAttackTime;
    private bool m_IsAttacking;
    private Vector3 m_StayPosition;

    // Animation parameters
    private static readonly int MovementFloat = Animator.StringToHash("Movement_f");
    private static readonly int AttackReadyBool = Animator.StringToHash("AttackReady_b");
    private static readonly int AttackTypeInt = Animator.StringToHash("AttackType_int");
    private static readonly int ActionTypeInt = Animator.StringToHash("ActionType_int");
    private static readonly int SitBool = Animator.StringToHash("Sit_b");
    private static readonly int JumpTrigger = Animator.StringToHash("Jump_tr");
    private static readonly int DeathBool = Animator.StringToHash("Death_b");

    // Singleton for easy access
    public static DogCompanion Instance { get; private set; }

    // Public properties
    public DogState CurrentState => m_CurrentState;
    public Transform Owner => m_Owner;
    public bool IsFollowing => m_CurrentState == DogState.Following;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        m_Agent = GetComponent<NavMeshAgent>();

        if (m_Animator == null)
            m_Animator = GetComponentInChildren<Animator>();

        if (m_AudioSource == null)
            m_AudioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        // Find owner if not assigned
        if (m_Owner == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                m_Owner = player.transform;
        }

        m_Agent.speed = m_WalkSpeed;
        m_Agent.stoppingDistance = m_FollowDistance;

        // Start in following state
        SetState(DogState.Following);
    }

    private void Update()
    {
        if (m_Owner == null)
        {
            // Try to find owner
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                m_Owner = player.transform;
            else
                return;
        }

        switch (m_CurrentState)
        {
            case DogState.Following:
                UpdateFollowing();
                break;
            case DogState.Staying:
                UpdateStaying();
                break;
            case DogState.Attacking:
                UpdateAttacking();
                break;
            case DogState.Returning:
                UpdateReturning();
                break;
            case DogState.Searching:
                // Handled by coroutine
                break;
        }

        UpdateAnimator();
    }

    private void UpdateFollowing()
    {
        float distanceToOwner = Vector3.Distance(transform.position, m_Owner.position);

        // Teleport if too far
        if (distanceToOwner > m_TeleportDistance)
        {
            TeleportToOwner();
            return;
        }

        // Run if far, walk if close
        m_Agent.speed = distanceToOwner > m_RunDistance ? m_RunSpeed : m_WalkSpeed;

        // Follow owner
        if (distanceToOwner > m_FollowDistance)
        {
            m_Agent.SetDestination(m_Owner.position);
        }
        else
        {
            m_Agent.ResetPath();
        }
    }

    private void UpdateStaying()
    {
        // Stay at position, look at owner
        if (m_Owner != null)
        {
            Vector3 lookDir = m_Owner.position - transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 2f);
        }
    }

    private void UpdateAttacking()
    {
        if (m_AttackTarget == null || !m_AttackTarget.gameObject.activeInHierarchy)
        {
            // Target gone, return to owner
            CommandCome();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, m_AttackTarget.position);

        if (distanceToTarget <= m_AttackRange)
        {
            // In range - attack!
            m_Agent.ResetPath();

            // Face target
            Vector3 lookDir = m_AttackTarget.position - transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookDir);

            // Attack if cooldown ready
            if (Time.time - m_LastAttackTime >= m_AttackCooldown && !m_IsAttacking)
            {
                StartCoroutine(PerformAttack());
            }
        }
        else if (distanceToTarget > m_MaxAttackDistance)
        {
            // Target too far, give up
            CommandCome();
        }
        else
        {
            // Move to target
            m_Agent.speed = m_RunSpeed;
            m_Agent.SetDestination(m_AttackTarget.position);
        }
    }

    private void UpdateReturning()
    {
        float distanceToOwner = Vector3.Distance(transform.position, m_Owner.position);

        if (distanceToOwner <= m_FollowDistance)
        {
            SetState(DogState.Following);
        }
        else
        {
            m_Agent.speed = m_RunSpeed;
            m_Agent.SetDestination(m_Owner.position);
        }
    }

    private IEnumerator PerformAttack()
    {
        m_IsAttacking = true;
        m_LastAttackTime = Time.time;

        // Play attack animation
        if (m_Animator != null)
        {
            m_Animator.SetBool(AttackReadyBool, true);
            m_Animator.SetInteger(AttackTypeInt, Random.Range(1, 3)); // Bite or claw
        }

        // Play growl sound
        PlaySound(m_GrowlClips);

        // Wait for attack to land
        yield return new WaitForSeconds(0.5f);

        // Deal damage
        if (m_AttackTarget != null)
        {
            // Try to damage zombie (using ZombieHealth component)
            var zombieHealth = m_AttackTarget.GetComponent<ZombieHealth>();
            if (zombieHealth == null)
                zombieHealth = m_AttackTarget.GetComponentInParent<ZombieHealth>();

            if (zombieHealth != null)
            {
                zombieHealth.TakeDamage(m_AttackDamage, gameObject);
                Debug.Log($"[DogCompanion] Dealt {m_AttackDamage} damage to {m_AttackTarget.name}");

                // Check if target died
                if (zombieHealth.IsDead)
                {
                    Debug.Log($"[DogCompanion] Killed {m_AttackTarget.name}!");
                    m_AttackTarget = null;

                    // Bark in triumph
                    PlaySound(m_BarkClips);

                    // Return to owner
                    yield return new WaitForSeconds(0.5f);
                    CommandCome();
                }
            }
        }

        // Reset attack animation
        if (m_Animator != null)
        {
            m_Animator.SetBool(AttackReadyBool, false);
            m_Animator.SetInteger(AttackTypeInt, 0);
        }

        m_IsAttacking = false;
    }

    private void UpdateAnimator()
    {
        if (m_Animator == null) return;

        // Set movement speed for animator
        float speed = m_Agent.velocity.magnitude / m_RunSpeed;
        m_Animator.SetFloat(MovementFloat, speed);
    }

    private void TeleportToOwner()
    {
        if (m_Owner == null) return;

        // Find position behind owner
        Vector3 behindOwner = m_Owner.position - m_Owner.forward * m_FollowDistance;

        // Sample NavMesh
        if (NavMesh.SamplePosition(behindOwner, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            m_Agent.Warp(hit.position);
            Debug.Log("[DogCompanion] Teleported to owner");
        }
    }

    private void SetState(DogState newState)
    {
        m_CurrentState = newState;
        Debug.Log($"[DogCompanion] State changed to: {newState}");

        // Handle state entry
        switch (newState)
        {
            case DogState.Staying:
                m_StayPosition = transform.position;
                m_Agent.ResetPath();
                if (m_Animator != null)
                    m_Animator.SetBool(SitBool, true);
                break;

            case DogState.Following:
            case DogState.Returning:
                if (m_Animator != null)
                    m_Animator.SetBool(SitBool, false);
                break;
        }
    }

    // === PUBLIC COMMANDS (called from radial menu) ===

    /// <summary>
    /// Command dog to attack a specific target
    /// </summary>
    public void CommandAttack(Transform target)
    {
        if (target == null) return;

        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > m_MaxAttackDistance)
        {
            Debug.Log("[DogCompanion] Target too far!");
            PlaySound(m_WhineClips);
            return;
        }

        m_AttackTarget = target;
        SetState(DogState.Attacking);
        PlaySound(m_BarkClips);
        Debug.Log($"[DogCompanion] Attacking {target.name}");
    }

    /// <summary>
    /// Command dog to attack whatever is under crosshair
    /// </summary>
    public void CommandAttackCrosshairTarget()
    {
        // Use the InteractionHighlight current target system
        var target = InteractionHighlight.CurrentTarget;

        if (target != null)
        {
            // Check if it's a zombie (has ZombieHealth component)
            var zombieHealth = target.GetComponent<ZombieHealth>() ?? target.GetComponentInParent<ZombieHealth>();
            if (zombieHealth != null && !zombieHealth.IsDead)
            {
                CommandAttack(zombieHealth.transform);
                return;
            }
        }

        // Fallback: raycast from camera
        Camera cam = Camera.main;
        if (cam != null)
        {
            Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, m_MaxAttackDistance))
            {
                var zombieHealth = hit.collider.GetComponent<ZombieHealth>() ?? hit.collider.GetComponentInParent<ZombieHealth>();
                if (zombieHealth != null && !zombieHealth.IsDead)
                {
                    CommandAttack(zombieHealth.transform);
                    return;
                }
            }
        }

        // No valid target
        PlaySound(m_WhineClips);
        Debug.Log("[DogCompanion] No valid attack target");
    }

    /// <summary>
    /// Toggle between stay and follow
    /// </summary>
    public void CommandStayFollow()
    {
        if (m_CurrentState == DogState.Staying)
        {
            SetState(DogState.Following);
            PlaySound(m_BarkClips);
        }
        else
        {
            SetState(DogState.Staying);
            // Small bark
        }
    }

    /// <summary>
    /// Command dog to come to owner immediately
    /// </summary>
    public void CommandCome()
    {
        m_AttackTarget = null;
        SetState(DogState.Returning);
        PlaySound(m_BarkClips);
    }

    /// <summary>
    /// Command dog to search for nearby loot
    /// </summary>
    public void CommandSearch()
    {
        if (m_CurrentState == DogState.Searching) return;

        StartCoroutine(PerformSearch());
    }

    private IEnumerator PerformSearch()
    {
        SetState(DogState.Searching);

        // Play sniff animation
        if (m_Animator != null)
        {
            m_Animator.SetInteger(ActionTypeInt, 5); // Sniff action
        }

        PlaySound(m_BarkClips);

        // Find nearby lootables
        Collider[] colliders = Physics.OverlapSphere(transform.position, m_SearchRadius);
        int foundCount = 0;

        foreach (var col in colliders)
        {
            // Check for lootable containers
            var lootable = col.GetComponent<SimpleLootableStorage>();
            if (lootable == null)
                lootable = col.GetComponentInParent<SimpleLootableStorage>();

            if (lootable != null)
            {
                // Highlight this lootable
                var highlight = lootable.GetComponent<InteractionHighlight>();
                if (highlight == null)
                    highlight = lootable.GetComponentInChildren<InteractionHighlight>();

                if (highlight != null)
                {
                    highlight.ForceShowIcon(true);
                    foundCount++;
                }
            }
        }

        Debug.Log($"[DogCompanion] Found {foundCount} lootables nearby");

        yield return new WaitForSeconds(m_SearchDuration);

        // Reset animation
        if (m_Animator != null)
        {
            m_Animator.SetInteger(ActionTypeInt, 0);
        }

        // Return to following
        SetState(DogState.Following);

        // Bark if found something
        if (foundCount > 0)
        {
            PlaySound(m_BarkClips);
        }
    }

    private void PlaySound(AudioClip[] clips)
    {
        if (m_AudioSource == null || clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        m_AudioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Set the owner (player) transform
    /// </summary>
    public void SetOwner(Transform owner)
    {
        m_Owner = owner;
        Debug.Log($"[DogCompanion] Owner set to {owner.name}");
    }

    private void OnDrawGizmosSelected()
    {
        // Draw follow distance
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, m_FollowDistance);

        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, m_AttackRange);

        // Draw search radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, m_SearchRadius);
    }
}
