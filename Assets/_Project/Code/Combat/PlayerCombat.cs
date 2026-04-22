using UnityEngine;
using System.Collections;

/// <summary>
/// Handles player melee combat. Listens for light/heavy attack input,
/// triggers animations, and performs hit detection via SphereCast.
///
/// Light attack: random punch or kick, fast, low damage.
/// Heavy attack: random heavy punch or kick, slower, more damage.
///
/// Hit detection fires during a timed window after the animation starts.
/// Only one hit per attack — prevents multi-hit on a single swing.
/// Player movement is briefly disabled during attacks.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Animator animator;

    [Header("Light Attack")]
    [SerializeField] private int lightDamage = 15;
    [SerializeField] private float lightWindup = 0.15f;
    [SerializeField] private float lightActiveWindow = 0.2f;
    [SerializeField] private float lightRecovery = 0.3f;

    [Header("Heavy Attack")]
    [SerializeField] private int heavyDamage = 35;
    [SerializeField] private float heavyWindup = 0.35f;
    [SerializeField] private float heavyActiveWindow = 0.25f;
    [SerializeField] private float heavyRecovery = 0.5f;

    [Header("Hit Detection")]
    [Tooltip("How far in front of the player the hit check reaches.")]
    [SerializeField] private float attackRange = 1.8f;
    [Tooltip("Radius of the hit sphere.")]
    [SerializeField] private float attackRadius = 0.6f;
    [Tooltip("Vertical offset for the sphere origin (chest height).")]
    [SerializeField] private float attackHeightOffset = 1.0f;
    [SerializeField] private LayerMask hitLayers;

    private bool _isAttacking;
    private Health _ownHealth;
    private PlayerController _playerController;

    // Animator trigger hashes
    private static readonly int LightPunchHash = Animator.StringToHash("LightPunch");
    private static readonly int LightKickHash = Animator.StringToHash("LightKick");
    private static readonly int HeavyPunchHash = Animator.StringToHash("HeavyPunch");
    private static readonly int HeavyKickHash = Animator.StringToHash("HeavyKick");

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
    }

    private void OnDisable()
    {
        if (inputReader != null)
        {
            inputReader.LightAttackPressed -= HandleLightAttack;
            inputReader.HeavyAttackPressed -= HandleHeavyAttack;
        }
    }

    private void ClearCombatTriggers()
    {
        animator.ResetTrigger(LightPunchHash);
        animator.ResetTrigger(LightKickHash);
        animator.ResetTrigger(HeavyPunchHash);
        animator.ResetTrigger(HeavyKickHash);
    }

    private void HandleLightAttack()
    {
        if (!CanAttack()) return;

        // Random: punch or kick
        int triggerHash = Random.value > 0.5f ? LightPunchHash : LightKickHash;
        StartCoroutine(AttackCoroutine(triggerHash, lightDamage, lightWindup, lightActiveWindow, lightRecovery, false));
    }

    private void HandleHeavyAttack()
    {
        if (!CanAttack()) return;

        // Random: heavy punch or heavy kick
        int triggerHash = Random.value > 0.5f ? HeavyPunchHash : HeavyKickHash;
        StartCoroutine(AttackCoroutine(triggerHash, heavyDamage, heavyWindup, heavyActiveWindow, heavyRecovery, true));
    }

    private bool CanAttack()
    {
        // Already swinging
        if (_isAttacking) return false;

        // Dead
        if (_ownHealth != null && _ownHealth.IsDead) return false;

        // On a vehicle
        if (PlayerStateManager.Instance != null && PlayerStateManager.Instance.IsInVehicle) return false;

        return true;
    }

    private IEnumerator AttackCoroutine(int animTrigger, int damage, float windup, float activeWindow, float recovery, bool isHeavy)
    {
        _isAttacking = true;

        // Disable movement during attack
        if (_playerController != null) _playerController.enabled = false;

        // Clear any queued triggers before setting the new one
        ClearCombatTriggers();
        // Trigger animation
        animator.SetTrigger(animTrigger);

        // Wind-up phase — animation is starting, no hit yet
        yield return new WaitForSeconds(windup);

        // Active window — perform hit detection
        bool hasHit = false;

        float timer = 0f;
        while (timer < activeWindow)
        {
            if (!hasHit)
                hasHit = PerformHitDetection(damage, isHeavy);

            timer += Time.deltaTime;
            yield return null;
        }

        // Recovery phase — animation finishing, still can't move
        yield return new WaitForSeconds(recovery);

        // Re-enable movement
        if (_playerController != null) _playerController.enabled = true;

        _isAttacking = false;
    }

    /// <summary>
    /// SphereCast forward from chest height. Returns true if an enemy was hit.
    /// </summary>
    private bool PerformHitDetection(int damage, bool isHeavy)
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
                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize attack range in editor
        Vector3 origin = transform.position + Vector3.up * attackHeightOffset;
        Vector3 end = origin + transform.forward * attackRange;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, attackRadius);
        Gizmos.DrawLine(origin, end);
        Gizmos.DrawWireSphere(end, attackRadius);
    }
}