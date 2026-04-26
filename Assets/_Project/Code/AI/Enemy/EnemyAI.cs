using UnityEngine;
using System;
using System.Collections;
using MCGame.Combat;
using MCGame.Gameplay.Player;

namespace MCGame.Gameplay.AI
{
    /// <summary>
    /// Simple enemy AI for melee combat.
    /// States: Idle → Chase → Attack → Dead
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

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("LightPunch");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int KnockoutHash = Animator.StringToHash("Knockout");

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
            _player = PlayerService.PlayerTransform;
            if (_player == null)
            {
                Debug.LogError("[EnemyAI] PlayerService has no registered player. Enemy AI disabled.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (_player == null) return;
            if (_state == EnemyState.Dead) return;
            if (_state == EnemyState.Stagger) return;
            if (_isAttacking) return;

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
                    break;
            }

            ApplyGravity();

            UpdateAnimator();
        }

        private void UpdateIdle(float distToPlayer)
        {
            if (distToPlayer <= detectionRange)
                _state = EnemyState.Chase;
        }

        private void UpdateChase(float distToPlayer)
        {
            if (distToPlayer > loseRange)
            {
                _state = EnemyState.Idle;
                return;
            }

            if (distToPlayer <= attackRange && _attackCooldownTimer <= 0)
            {
                StartCoroutine(AttackCoroutine());
                return;
            }

            FaceTarget();
            MoveTowardPlayer();
        }

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

        private IEnumerator AttackCoroutine()
        {
            _isAttacking = true;
            _state = EnemyState.Attack;

            FaceTarget();

            if (animator != null)
                animator.SetTrigger(AttackHash);

            yield return new WaitForSeconds(attackWindup);

            float timer = 0f;
            bool hasHit = false;
            while (timer < attackActiveWindow)
            {
                if (!hasHit)
                    hasHit = PerformEnemyHitDetection();

                timer += Time.deltaTime;
                yield return null;
            }

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

        private void HandleDamaged(DamageInfo info)
        {
            if (_state == EnemyState.Dead) return;

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

            _cc.enabled = false;

            OnDefeated?.Invoke();

            Destroy(gameObject, 5f);
        }

        private void UpdateAnimator()
        {
            if (animator == null) return;

            float speed = 0f;
            if (_state == EnemyState.Chase && !_isAttacking)
                speed = 0.5f;

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
}