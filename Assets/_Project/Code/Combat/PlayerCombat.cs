using UnityEngine;
using System.Collections;

/// <summary>
/// Handles all player combat — attacking AND taking hits.
///
/// Attacking: Listens for light/heavy attack input, triggers animations,
/// performs hit detection via SphereCast after a tunable delay.
///
/// Taking hits: Listens to Health.OnDamaged, plays Hit animation,
/// briefly stuns the player (disables movement and interrupts attacks).
///
/// Light attack: random punch or kick, fast, low damage.
/// Heavy attack: random heavy punch or kick, slower, more damage.
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
    [Tooltip("Seconds from animation start to when the hit lands.")]
    [SerializeField] private float heavyHitDelay = 0.5f;
    [Tooltip("Total attack duration — should match the animation clip length.")]
    [SerializeField] private float heavyTotalDuration = 1.2f;

    [Header("Hit Detection")]
    [Tooltip("How far in front of the player the hit check reaches.")]
    [SerializeField] private float attackRange = 1.8f;
    [Tooltip("Radius of the hit sphere.")]
    [SerializeField] private float attackRadius = 0.6f;
    [Tooltip("Vertical offset for the sphere origin (chest height).")]
    [SerializeField] private float attackHeightOffset = 1.0f;
    [SerializeField] private LayerMask hitLayers;

    [Header("Hit Reaction")]
    [Tooltip("How long the player is stunned after being hit.")]
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

        // Wait until the fist/foot connects
        yield return new WaitForSeconds(hitDelay);

        // Perform hit detection at the contact moment
        PerformHitDetection(damage, isHeavy);

        // Wait for the rest of the animation
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

        // Interrupt current attack if we get hit mid-swing
        if (_isAttacking)
        {
            StopAllCoroutines();
            _isAttacking = false;
        }

        // Play hit animation
        animator.SetTrigger(HitHash);

        // Start hit stun
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
        // Stop everything
        StopAllCoroutines();
        _isAttacking = false;
        _isStunned = true;

        // Play knockout
        animator.SetTrigger(KnockoutHash);

        // Disable movement permanently until respawn
        if (_playerController != null) _playerController.enabled = false;
    }

    // ===================
    // DEBUG
    // ===================

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