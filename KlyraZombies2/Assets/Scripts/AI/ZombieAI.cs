using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Surroundead-style zombie AI.
/// States: Idle -> Wander -> Alert -> Chase -> Attack
/// Detects players by sight and sound (gunshots).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class ZombieAI : MonoBehaviour
{
    public enum ZombieState { Idle, Wander, Alert, Chase, Attack, Dead }
    public enum ZombieType { Walker, Runner, Crawler }

    [Header("Zombie Type")]
    [SerializeField] private ZombieType m_ZombieType = ZombieType.Walker;

    [Header("Detection")]
    [SerializeField] private float m_SightRange = 15f;
    [SerializeField] private float m_SightAngle = 120f;
    [SerializeField] private float m_HearingRange = 30f;
    [SerializeField] private float m_GunshotHearingRange = 80f;
    [SerializeField] private LayerMask m_PlayerLayer;
    [SerializeField] private LayerMask m_ObstacleLayer;

    [Header("Movement")]
    [SerializeField] private float m_WalkSpeed = 1f;
    [SerializeField] private float m_RunSpeed = 4f;
    [SerializeField] private float m_WanderRadius = 10f;
    [SerializeField] private float m_WanderWaitTime = 3f;

    [Header("Combat")]
    [SerializeField] private float m_AttackRange = 2f;
    [SerializeField] private float m_AttackDamage = 10f;
    [SerializeField] private float m_AttackCooldown = 1.5f;

    [Header("Behavior")]
    [SerializeField] private float m_AlertDuration = 5f;
    [SerializeField] private float m_LoseInterestDistance = 40f;
    [SerializeField] private float m_LoseInterestTime = 10f;

    [Header("Audio")]
    [SerializeField] private AudioClip[] m_IdleSounds;
    [SerializeField] private AudioClip[] m_AlertSounds;
    [SerializeField] private AudioClip[] m_AttackSounds;
    [SerializeField] private AudioSource m_AudioSource;

    [Header("Debug")]
    [SerializeField] private bool m_DebugMode = false;

    // State
    public ZombieState CurrentState { get; private set; } = ZombieState.Idle;
    private Transform m_Target;
    private Vector3 m_LastKnownTargetPosition;
    private NavMeshAgent m_Agent;
    private Animator m_Animator;

    // Timers
    private float m_StateTimer;
    private float m_WanderTimer;
    private float m_AttackTimer;
    private float m_SoundTimer;
    private float m_ChaseTimer;

    // Animation hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DeadHash = Animator.StringToHash("Dead");
    private static readonly int AlertHash = Animator.StringToHash("Alert");

    private void Awake()
    {
        m_Agent = GetComponent<NavMeshAgent>();
        m_Animator = GetComponentInChildren<Animator>();
        m_AudioSource = GetComponent<AudioSource>();

        if (m_Agent == null)
        {
            Debug.LogError($"[ZombieAI] {gameObject.name} has no NavMeshAgent! Adding one.");
            m_Agent = gameObject.AddComponent<NavMeshAgent>();
        }

        if (m_AudioSource == null)
            m_AudioSource = gameObject.AddComponent<AudioSource>();

        // Set speed based on type
        switch (m_ZombieType)
        {
            case ZombieType.Walker:
                m_WalkSpeed = 1f;
                m_RunSpeed = 3.5f;
                break;
            case ZombieType.Runner:
                m_WalkSpeed = 2f;
                m_RunSpeed = 6f;
                break;
            case ZombieType.Crawler:
                m_WalkSpeed = 0.5f;
                m_RunSpeed = 1.5f;
                break;
        }

        // Ensure agent is enabled
        m_Agent.enabled = true;
    }

    private void Start()
    {
        m_Agent.speed = m_WalkSpeed;

        // Validate NavMeshAgent is on NavMesh
        if (!m_Agent.isOnNavMesh)
        {
            Debug.LogError($"[ZombieAI] {gameObject.name} is NOT on NavMesh! Bake NavMesh and ensure zombie is placed on walkable surface.");
        }
        else if (m_DebugMode)
        {
            Debug.Log($"[ZombieAI] {gameObject.name} is on NavMesh. Agent enabled: {m_Agent.enabled}, isStopped: {m_Agent.isStopped}");
        }

        SetState(ZombieState.Idle);

        // Register for gunshot events
        ZombieManager.OnGunshotFired += OnGunshotHeard;

        // Find player if not using layer detection
        if (m_PlayerLayer == 0 && m_DebugMode)
        {
            Debug.Log($"[ZombieAI] {gameObject.name} has no PlayerLayer set. Will search by 'Player' tag.");
        }
    }

    private void OnDestroy()
    {
        ZombieManager.OnGunshotFired -= OnGunshotHeard;
    }

    private void Update()
    {
        if (CurrentState == ZombieState.Dead) return;

        // Always check for player detection
        CheckForPlayer();

        // Update current state
        switch (CurrentState)
        {
            case ZombieState.Idle:
                UpdateIdle();
                break;
            case ZombieState.Wander:
                UpdateWander();
                break;
            case ZombieState.Alert:
                UpdateAlert();
                break;
            case ZombieState.Chase:
                UpdateChase();
                break;
            case ZombieState.Attack:
                UpdateAttack();
                break;
        }

        // Update animator
        UpdateAnimator();

        // Random idle sounds
        UpdateSounds();
    }

    #region State Updates

    private void UpdateIdle()
    {
        m_StateTimer -= Time.deltaTime;
        if (m_StateTimer <= 0)
        {
            SetState(ZombieState.Wander);
        }
    }

    private void UpdateWander()
    {
        // Make sure agent can move
        if (!m_Agent.isOnNavMesh)
        {
            if (m_DebugMode)
                Debug.LogWarning($"[ZombieAI] {gameObject.name} UpdateWander: Not on NavMesh!");
            return;
        }

        if (!m_Agent.hasPath || m_Agent.remainingDistance < 0.5f)
        {
            m_WanderTimer -= Time.deltaTime;
            if (m_WanderTimer <= 0)
            {
                // Pick new wander destination
                Vector3 randomDirection = Random.insideUnitSphere * m_WanderRadius;
                randomDirection += transform.position;

                if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, m_WanderRadius, NavMesh.AllAreas))
                {
                    m_Agent.SetDestination(hit.position);
                    if (m_DebugMode)
                        Debug.Log($"[ZombieAI] {gameObject.name} wandering to {hit.position}");
                }
                else if (m_DebugMode)
                {
                    Debug.LogWarning($"[ZombieAI] {gameObject.name} couldn't find valid wander position!");
                }

                m_WanderTimer = m_WanderWaitTime + Random.Range(-1f, 1f);
            }
        }
    }

    private void UpdateAlert()
    {
        // Look towards last known position
        Vector3 lookDir = m_LastKnownTargetPosition - transform.position;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
        }

        m_StateTimer -= Time.deltaTime;
        if (m_StateTimer <= 0)
        {
            // Lost interest, go back to wandering
            SetState(ZombieState.Wander);
        }
    }

    private void UpdateChase()
    {
        if (m_Target == null)
        {
            SetState(ZombieState.Alert);
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, m_Target.position);

        // Check if close enough to attack
        if (distanceToTarget <= m_AttackRange)
        {
            SetState(ZombieState.Attack);
            return;
        }

        // Check if lost target
        if (distanceToTarget > m_LoseInterestDistance)
        {
            m_ChaseTimer += Time.deltaTime;
            if (m_ChaseTimer > m_LoseInterestTime)
            {
                m_Target = null;
                SetState(ZombieState.Alert);
                return;
            }
        }
        else
        {
            m_ChaseTimer = 0;
        }

        // Update destination
        m_Agent.SetDestination(m_Target.position);
        m_LastKnownTargetPosition = m_Target.position;
    }

    private void UpdateAttack()
    {
        if (m_Target == null)
        {
            SetState(ZombieState.Alert);
            return;
        }

        // Face target
        Vector3 lookDir = m_Target.position - transform.position;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
        }

        // Stop moving during attack
        m_Agent.isStopped = true;

        m_AttackTimer -= Time.deltaTime;
        if (m_AttackTimer <= 0)
        {
            PerformAttack();
            m_AttackTimer = m_AttackCooldown;
        }

        // Check if target moved out of range
        float distanceToTarget = Vector3.Distance(transform.position, m_Target.position);
        if (distanceToTarget > m_AttackRange * 1.5f)
        {
            m_Agent.isStopped = false;
            SetState(ZombieState.Chase);
        }
    }

    #endregion

    #region Detection

    private void CheckForPlayer()
    {
        if (CurrentState == ZombieState.Attack) return;

        Transform foundPlayer = null;

        // Method 1: Use layer mask if configured
        if (m_PlayerLayer != 0)
        {
            Collider[] playersInRange = Physics.OverlapSphere(transform.position, m_SightRange, m_PlayerLayer);

            foreach (var playerCollider in playersInRange)
            {
                Vector3 directionToPlayer = playerCollider.transform.position - transform.position;
                float distanceToPlayer = directionToPlayer.magnitude;

                // Check if within sight angle
                float angle = Vector3.Angle(transform.forward, directionToPlayer);
                if (angle < m_SightAngle / 2f)
                {
                    // Check line of sight
                    if (!Physics.Raycast(transform.position + Vector3.up, directionToPlayer.normalized,
                        distanceToPlayer, m_ObstacleLayer))
                    {
                        // Player spotted!
                        OnPlayerDetected(playerCollider.transform);
                        return;
                    }
                }

                // Can also hear nearby players (footsteps, etc)
                if (distanceToPlayer < m_HearingRange * 0.5f)
                {
                    OnPlayerDetected(playerCollider.transform);
                    return;
                }
            }
        }

        // Method 2: Fallback to finding player by tag
        if (foundPlayer == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, playerObject.transform.position);

                // Check sight range
                if (distanceToPlayer <= m_SightRange)
                {
                    Vector3 directionToPlayer = playerObject.transform.position - transform.position;
                    float angle = Vector3.Angle(transform.forward, directionToPlayer);

                    // Within sight cone
                    if (angle < m_SightAngle / 2f)
                    {
                        // Check line of sight (ignore obstacles check if no obstacle layer set)
                        if (m_ObstacleLayer == 0 || !Physics.Raycast(transform.position + Vector3.up,
                            directionToPlayer.normalized, distanceToPlayer, m_ObstacleLayer))
                        {
                            OnPlayerDetected(playerObject.transform);
                            return;
                        }
                    }
                }

                // Hearing range check
                if (distanceToPlayer < m_HearingRange * 0.5f)
                {
                    OnPlayerDetected(playerObject.transform);
                    return;
                }
            }
        }
    }

    private void OnPlayerDetected(Transform player)
    {
        m_Target = player;
        m_LastKnownTargetPosition = player.position;
        m_ChaseTimer = 0;

        if (CurrentState != ZombieState.Chase && CurrentState != ZombieState.Attack)
        {
            if (m_DebugMode)
                Debug.Log($"[ZombieAI] {gameObject.name} detected player {player.name}! Starting chase.");
            PlaySound(m_AlertSounds);
            SetState(ZombieState.Chase);
        }
    }

    /// <summary>
    /// Called when this zombie takes damage - aggro on attacker
    /// </summary>
    public void OnDamaged(GameObject attacker)
    {
        if (CurrentState == ZombieState.Dead) return;

        if (attacker != null)
        {
            m_Target = attacker.transform;
            m_LastKnownTargetPosition = attacker.transform.position;
            m_ChaseTimer = 0;

            if (m_DebugMode)
                Debug.Log($"[ZombieAI] {gameObject.name} was hit! Aggroing on {attacker.name}");

            if (CurrentState != ZombieState.Chase && CurrentState != ZombieState.Attack)
            {
                PlaySound(m_AlertSounds);
                SetState(ZombieState.Chase);
            }
        }
    }

    private void OnGunshotHeard(Vector3 position, float range)
    {
        if (CurrentState == ZombieState.Dead) return;

        float distance = Vector3.Distance(transform.position, position);
        if (distance < m_GunshotHearingRange)
        {
            m_LastKnownTargetPosition = position;

            // If not already chasing, become alert
            if (CurrentState != ZombieState.Chase && CurrentState != ZombieState.Attack)
            {
                PlaySound(m_AlertSounds);
                SetState(ZombieState.Alert);

                // Move towards gunshot
                m_Agent.SetDestination(position);
            }
        }
    }

    #endregion

    #region Combat

    private void PerformAttack()
    {
        if (m_Animator != null)
            m_Animator.SetTrigger(AttackHash);

        PlaySound(m_AttackSounds);

        // Damage player if still in range
        if (m_Target != null)
        {
            float distance = Vector3.Distance(transform.position, m_Target.position);
            if (distance <= m_AttackRange * 1.2f)
            {
                // Try to damage player
                var health = m_Target.GetComponent<IHealth>();
                if (health != null)
                {
                    health.TakeDamage(m_AttackDamage, gameObject);
                }
            }
        }
    }

    public void Die()
    {
        if (CurrentState == ZombieState.Dead) return;

        SetState(ZombieState.Dead);
        m_Agent.isStopped = true;
        m_Agent.enabled = false;

        if (m_Animator != null)
            m_Animator.SetTrigger(DeadHash);

        // Disable collider after death
        var collider = GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;

        // Destroy after delay
        Destroy(gameObject, 10f);
    }

    #endregion

    #region State Management

    private void SetState(ZombieState newState)
    {
        if (m_DebugMode)
            Debug.Log($"[ZombieAI] {gameObject.name}: {CurrentState} -> {newState}");

        CurrentState = newState;

        switch (newState)
        {
            case ZombieState.Idle:
                m_StateTimer = Random.Range(2f, 5f);
                m_Agent.isStopped = true;
                m_Agent.speed = m_WalkSpeed;
                break;

            case ZombieState.Wander:
                m_Agent.isStopped = false;
                m_Agent.speed = m_WalkSpeed;
                m_WanderTimer = 0;
                break;

            case ZombieState.Alert:
                m_StateTimer = m_AlertDuration;
                m_Agent.speed = m_WalkSpeed;
                m_Agent.isStopped = false;
                if (m_Animator != null)
                    m_Animator.SetTrigger(AlertHash);
                break;

            case ZombieState.Chase:
                m_Agent.isStopped = false;
                m_Agent.speed = m_RunSpeed;
                break;

            case ZombieState.Attack:
                m_AttackTimer = 0.5f; // Small delay before first attack
                break;

            case ZombieState.Dead:
                break;
        }
    }

    #endregion

    #region Animation & Sound

    private void UpdateAnimator()
    {
        if (m_Animator == null) return;

        float speed = m_Agent.velocity.magnitude;
        m_Animator.SetFloat(SpeedHash, speed);
    }

    private void UpdateSounds()
    {
        if (CurrentState == ZombieState.Dead) return;

        m_SoundTimer -= Time.deltaTime;
        if (m_SoundTimer <= 0)
        {
            if (CurrentState == ZombieState.Idle || CurrentState == ZombieState.Wander)
            {
                PlaySound(m_IdleSounds, 0.3f);
            }
            m_SoundTimer = Random.Range(5f, 15f);
        }
    }

    private void PlaySound(AudioClip[] clips, float volume = 1f)
    {
        if (clips == null || clips.Length == 0 || m_AudioSource == null) return;

        var clip = clips[Random.Range(0, clips.Length)];
        if (clip != null)
        {
            m_AudioSource.PlayOneShot(clip, volume);
        }
    }

    #endregion

    #region Debug

    private void OnDrawGizmosSelected()
    {
        // Sight range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, m_SightRange);

        // Hearing range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, m_HearingRange);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, m_AttackRange);

        // Sight cone
        Gizmos.color = Color.green;
        Vector3 leftRay = Quaternion.Euler(0, -m_SightAngle / 2, 0) * transform.forward * m_SightRange;
        Vector3 rightRay = Quaternion.Euler(0, m_SightAngle / 2, 0) * transform.forward * m_SightRange;
        Gizmos.DrawRay(transform.position + Vector3.up, leftRay);
        Gizmos.DrawRay(transform.position + Vector3.up, rightRay);
    }

    #endregion
}

/// <summary>
/// Interface for damageable objects
/// </summary>
public interface IHealth
{
    void TakeDamage(float damage, GameObject attacker);
}
