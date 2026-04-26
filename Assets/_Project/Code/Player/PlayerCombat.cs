using UnityEngine;
using System.Collections;
using MCGame.Input;
using MCGame.Combat;

namespace MCGame.Gameplay.Player
{
    /// <summary>
    /// Handles all player combat — attacking AND taking hits.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputReader inputReader;
        [SerializeField] private Animator animator;

        [Header("Light Attack")]
        [SerializeField] private int lightDamage = 15;
        [Tooltip("Seconds from animation start to when the hit lands.")]
        [SerializeField] private float lightHitDelay = 0.3f;
        [Tooltip("Total attack duration — should match the animation clip length.")]
        [SerializeField] private float lightTotalDuration = 0.8f;

        [Header("Heavy Attack")]
        [SerializeField] private int heavyDamage = 35;
        [SerializeField] private float heavyHitDelay = 0.5f;
        [SerializeField] private float heavyTotalDuration = 1.2f;

        [Header("Hit Detection")]
        [SerializeField] private float attackRange = 1.8f;
        [SerializeField] private float attackRadius = 0.6f;
        [SerializeField] private float attackHeightOffset = 1.0f;
        [SerializeField] private LayerMask hitLayers;

        [Header("Hit Reaction")]
        [SerializeField] private float hitStunDuration = 0.5f;

        private bool _isAttacking;
        private bool _isStunned;
        private Health _ownHealth;
        private PlayerController _playerController;

        // Animator trigger hashes
        private static readonly int LightPunchHash = Animator.StringToHash("LightPunch");
        private static readonly int LightKickHash = Animator.StringToHash("LightKick");
        private static readonly int HeavyPunchHash = Animator.StringToHash("HeavyPunch");
        private static readonly int HeavyKickHash = Animator.StringToHash("HeavyKick");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int KnockoutHash = Animator.StringToHash("Knockout");

        public bool IsAttacking => _isAttacking;

        private void Awake()
        {
            _ownHealth = GetComponent<Health>();
            _playerController = GetComponent<PlayerController>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        private void OnEnable()
        {
            if (inputReader != null)
            {
                inputReader.LightAttackPressed += HandleLightAttack;
                inputReader.HeavyAttackPressed += HandleHeavyAttack;
            }

            if (_ownHealth != null)
            {
                _ownHealth.OnDamaged += HandleDamaged;
                _ownHealth.OnDied += HandleDied;
            }
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.LightAttackPressed -= HandleLightAttack;
                inputReader.HeavyAttackPressed -= HandleHeavyAttack;
            }

            if (_ownHealth != null)
            {
                _ownHealth.OnDamaged -= HandleDamaged;
                _ownHealth.OnDied -= HandleDied;
            }
        }

        // ===================
        // ATTACKING
        // ===================

        private void HandleLightAttack()
        {
            if (!CanAttack()) return;

            int triggerHash = Random.value > 0.5f ? LightPunchHash : LightKickHash;
            StartCoroutine(AttackCoroutine(triggerHash, lightDamage, lightHitDelay, lightTotalDuration, false));
        }

        private void HandleHeavyAttack()
        {
            if (!CanAttack()) return;

            int triggerHash = Random.value > 0.5f ? HeavyPunchHash : HeavyKickHash;
            StartCoroutine(AttackCoroutine(triggerHash, heavyDamage, heavyHitDelay, heavyTotalDuration, true));
        }

        private bool CanAttack()
        {
            if (_isAttacking) return false;
            if (_isStunned) return false;
            if (_ownHealth != null && _ownHealth.IsDead) return false;
            if (PlayerStateManager.Instance != null && PlayerStateManager.Instance.IsInVehicle) return false;
            return true;
        }

        private void ClearCombatTriggers()
        {
            animator.ResetTrigger(LightPunchHash);
            animator.ResetTrigger(LightKickHash);
            animator.ResetTrigger(HeavyPunchHash);
            animator.ResetTrigger(HeavyKickHash);
        }

        private IEnumerator AttackCoroutine(int animTrigger, int damage, float hitDelay, float totalDuration, bool isHeavy)
        {
            _isAttacking = true;

            if (_playerController != null) _playerController.enabled = false;

            ClearCombatTriggers();
            animator.SetTrigger(animTrigger);

            yield return new WaitForSeconds(hitDelay);

            PerformHitDetection(damage, isHeavy);

            float remaining = totalDuration - hitDelay;
            if (remaining > 0)
                yield return new WaitForSeconds(remaining);

            if (_playerController != null) _playerController.enabled = true;

            _isAttacking = false;
        }

        private void PerformHitDetection(int damage, bool isHeavy)
        {
            Vector3 origin = transform.position + Vector3.up * attackHeightOffset;
            Vector3 direction = transform.forward;

            if (Physics.SphereCast(origin, attackRadius, direction, out RaycastHit hit, attackRange, hitLayers))
            {
                Health targetHealth = hit.collider.GetComponentInParent<Health>();
                if (targetHealth != null && !targetHealth.IsDead)
                {
                    DamageInfo info = new DamageInfo(
                        damage,
                        gameObject,
                        direction,
                        isHeavy
                    );

                    targetHealth.TakeDamage(info);
                }
            }
        }

        // ===================
        // TAKING HITS
        // ===================

        private void HandleDamaged(DamageInfo info)
        {
            if (_ownHealth.IsDead) return;

            if (_isAttacking)
            {
                StopAllCoroutines();
                _isAttacking = false;
            }

            animator.SetTrigger(HitHash);

            StartCoroutine(HitStunCoroutine());
        }

        private IEnumerator HitStunCoroutine()
        {
            _isStunned = true;

            if (_playerController != null) _playerController.enabled = false;

            yield return new WaitForSeconds(hitStunDuration);

            if (_ownHealth != null && !_ownHealth.IsDead)
            {
                if (_playerController != null) _playerController.enabled = true;
            }

            _isStunned = false;
        }

        private void HandleDied()
        {
            StopAllCoroutines();
            _isAttacking = false;
            _isStunned = true;

            animator.SetTrigger(KnockoutHash);

            if (_playerController != null) _playerController.enabled = false;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position + Vector3.up * attackHeightOffset;
            Vector3 end = origin + transform.forward * attackRange;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(origin, attackRadius);
            Gizmos.DrawLine(origin, end);
            Gizmos.DrawWireSphere(end, attackRadius);
        }
    }
}