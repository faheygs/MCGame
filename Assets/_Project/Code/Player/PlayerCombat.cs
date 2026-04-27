using System.Collections;
using UnityEngine;
using MCGame.Core;
using MCGame.Combat;
using MCGame.Input;

namespace MCGame.Gameplay.Player
{
    /// <summary>
    /// Handles player melee combat: light/heavy attacks, hit reactions, knockout.
    /// </summary>
    [RequireComponent(typeof(Health))]
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

            int triggerHash = Random.value > 0.5f ? AnimatorParams.LightPunch : AnimatorParams.LightKick;
            StartCoroutine(AttackCoroutine(triggerHash, lightDamage, lightHitDelay, lightTotalDuration, false));
        }

        private void HandleHeavyAttack()
        {
            if (!CanAttack()) return;

            int triggerHash = Random.value > 0.5f ? AnimatorParams.HeavyPunch : AnimatorParams.HeavyKick;
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
            animator.ResetTrigger(AnimatorParams.LightPunch);
            animator.ResetTrigger(AnimatorParams.LightKick);
            animator.ResetTrigger(AnimatorParams.HeavyPunch);
            animator.ResetTrigger(AnimatorParams.HeavyKick);
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
                if (_playerController != null) _playerController.enabled = true;
            }

            StartCoroutine(HitStunCoroutine());
        }

        private IEnumerator HitStunCoroutine()
        {
            _isStunned = true;

            ClearCombatTriggers();
            animator.SetTrigger(AnimatorParams.Hit);

            if (_playerController != null) _playerController.enabled = false;

            yield return new WaitForSeconds(hitStunDuration);

            if (_ownHealth.IsDead) yield break;

            if (_playerController != null) _playerController.enabled = true;

            _isStunned = false;
        }

        private void HandleDied()
        {
            StopAllCoroutines();
            _isAttacking = false;
            _isStunned = true;

            ClearCombatTriggers();
            animator.SetTrigger(AnimatorParams.Knockout);

            if (_playerController != null) _playerController.enabled = false;
        }
    }
}