using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Simple enemy AI for melee combat.
/// States: Idle → Chase → Attack → Dead
///
/// No pathfinding — walks straight toward the player.
/// Uses CharacterController for movement (matches player approach).
/// Reacts to being hit with a brief stagger.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(CombatTarget))]
public class EnemyAI : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Chase,
        Attack,
        Stagger,
        Dead
    }

    [Header("Detection")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float loseRange = 25f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float gravity = -20f;

    [Header("Combat")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackWindup = 0.3f;
    [SerializeField] private float attackActiveWindow = 0.15f;
    [SerializeField] private float attackRecovery = 0.4f;
    [SerializeField] private float attackRadius = 0.5f;
    [SerializeField] private float staggerDuration = 0.5f;

    [Header("Hit Detection")]
    [SerializeField] private LayerMask playerLayer;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private CharacterController _cc;
    private Health _health;
    private Transform _player;
    private EnemyState _state = EnemyState.Idle;
    private float _attackCooldownTimer;
    private float _verticalVelocity;
    private bool _isAttacking;

    // Animator hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("LightPunch");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int KnockoutHash = Animator.StringToHash("Knockout");

    /// <summary>Fires when this enemy is defeated. MissionObjective listens for this.</summary>
    public event Action OnDefeated;

    public EnemyState CurrentState => _state;
    public bool IsDead => _state == EnemyState.Dead;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _health = GetComponent<Health>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        _health.OnDamaged += HandleDamaged;
        _health.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        _health.OnDamaged -= HandleDamaged;
        _health.OnDied -= HandleDied;
    }

    private void Start()
    {
        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
            _player = playerGO.transform;
    }

    private void Update()
    {
        if (_player == null) return;
        if (_state == EnemyState.Dead) return;
        if (_state == EnemyState.Stagger) return;
        if (_isAttacking) return;

        // Cooldown timer
        if (_attackCooldownTimer > 0)
            _attackCooldownTimer -= Time.deltaTime;

        float distToPlayer = Vector3.Distance(transform.position, _player.position);

        switch (_state)
        {
            case EnemyState.Idle:
                UpdateIdle(distToPlayer);
                break;
            case EnemyState.Chase:
                UpdateChase(distToPlayer);
                break;
            case EnemyState.Attack:
                // Attack is handled by coroutine, shouldn't reach here
                break;
        }

        // Apply gravity
        ApplyGravity();

        // Update animator
        UpdateAnimator();
    }

    // --- State Updates ---

    private void UpdateIdle(float distToPlayer)
    {
        if (distToPlayer <= detectionRange)
            _state = EnemyState.Chase;
    }

    private void UpdateChase(float distToPlayer)
    {
        // Lost the player
        if (distToPlayer > loseRange)
        {
            _state = EnemyState.Idle;
            return;
        }

        // In attack range
        if (distToPlayer <= attackRange && _attackCooldownTimer <= 0)
        {
            StartCoroutine(AttackCoroutine());
            return;
        }

        // Move toward player
        FaceTarget();
        MoveTowardPlayer();
    }

    // --- Movement ---

    private void MoveTowardPlayer()
    {
        Vector3 direction = (_player.position - transform.position).normalized;
        direction.y = 0;

        Vector3 move = direction * moveSpeed;
        move.y = _verticalVelocity;

        _cc.Move(move * Time.deltaTime);
    }

    private void FaceTarget()
    {
        Vector3 lookDir = _player.position - transform.position;
        lookDir.y = 0;

        if (lookDir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(lookDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (_cc.isGrounded)
            _verticalVelocity = -2f;
        else
            _verticalVelocity += gravity * Time.deltaTime;

        _cc.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
    }

    // --- Combat ---

    private IEnumerator AttackCoroutine()
    {
        _isAttacking = true;
        _state = EnemyState.Attack;

        // Face the player before swinging
        FaceTarget();

        // Trigger animation
        if (animator != null)
            animator.SetTrigger(AttackHash);

        // Wind-up
        yield return new WaitForSeconds(attackWindup);

        // Hit detection window
        float timer = 0f;
        bool hasHit = false;
        while (timer < attackActiveWindow)
        {
            if (!hasHit)
                hasHit = PerformEnemyHitDetection();

            timer += Time.deltaTime;
            yield return null;
        }

        // Recovery
        yield return new WaitForSeconds(attackRecovery);

        _attackCooldownTimer = attackCooldown;
        _isAttacking = false;
        _state = EnemyState.Chase;
    }

    private bool PerformEnemyHitDetection()
    {
        Vector3 origin = transform.position + Vector3.up * 1.0f;
        Vector3 direction = transform.forward;

        if (Physics.SphereCast(origin, attackRadius, direction, out RaycastHit hit, attackRange, playerLayer))
        {
            Health playerHealth = hit.collider.GetComponentInParent<Health>();
            if (playerHealth != null && !playerHealth.IsDead)
            {
                DamageInfo info = new DamageInfo(
                    attackDamage,
                    gameObject,
                    direction,
                    false
                );

                playerHealth.TakeDamage(info);
                return true;
            }
        }

        return false;
    }

    // --- Damage Reactions ---

    private void HandleDamaged(DamageInfo info)
    {
        if (_state == EnemyState.Dead) return;

        // Interrupt current action and stagger
        StopAllCoroutines();
        _isAttacking = false;
        StartCoroutine(StaggerCoroutine());
    }

    private IEnumerator StaggerCoroutine()
    {
        _state = EnemyState.Stagger;

        if (animator != null)
            animator.SetTrigger(HitHash);

        yield return new WaitForSeconds(staggerDuration);

        // Resume chasing after stagger
        if (_state != EnemyState.Dead)
            _state = EnemyState.Chase;
    }

    private void HandleDied()
    {
        _state = EnemyState.Dead;

        StopAllCoroutines();
        _isAttacking = false;

        if (animator != null)
            animator.SetTrigger(KnockoutHash);

        // Disable the CharacterController so the body doesn't block movement
        _cc.enabled = false;

        // Notify mission system
        OnDefeated?.Invoke();

        // Destroy after a delay so the knockout animation can play
        Destroy(gameObject, 5f);
    }

    // --- Animation ---

    private void UpdateAnimator()
    {
        if (animator == null) return;

        float speed = 0f;
        if (_state == EnemyState.Chase && !_isAttacking)
            speed = 0.5f; // Normalized walk speed

        animator.SetFloat(SpeedHash, speed, 0.1f, Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}