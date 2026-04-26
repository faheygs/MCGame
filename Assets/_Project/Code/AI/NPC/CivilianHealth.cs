using UnityEngine;
using System.Collections;
using MCGame.Combat;

/// <summary>
/// Bridges Health component on civilians to the crime reporting system.
/// When a civilian is knocked out, reports the crime if witnesses are present.
/// Also handles hit reactions, knockout animations, and recovery after being KO'd.
/// </summary>
[RequireComponent(typeof(Health))]
public class CivilianHealth : MonoBehaviour
{
    [Header("Crime Reporting")]
    [Tooltip("The crime type to report when this civilian is knocked out")]
    [SerializeField] private CrimeType assaultCrimeType;

    [Header("Recovery")]
    [Tooltip("Time in seconds before civilian recovers from knockout")]
    [SerializeField] private float recoveryTime = 30f;

    [Header("Animation")]
    [Tooltip("Animator on the character model")]
    [SerializeField] private Animator animator;

    private Health _health;
    private bool _isKnockedOut;
    private Coroutine _recoveryCoroutine;

    private void Awake()
    {
        _health = GetComponent<Health>();
        
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
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

    private void HandleDamaged(DamageInfo info)
    {
        if (_isKnockedOut) return;

        // Play hit animation when damaged but not dead
        if (animator != null && !_health.IsDead)
        {
            animator.SetTrigger("Hit");
        }
    }

    private void HandleDied()
    {
        if (_isKnockedOut) return;
        _isKnockedOut = true;

        // Disable the civilian's movement AI
        CivilianNPC civilianAI = GetComponent<CivilianNPC>();
        if (civilianAI != null)
        {
            civilianAI.enabled = false;
        }

        // Set animator Speed to 0 and trigger knockout
        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            StartCoroutine(TriggerKnockoutNextFrame());
        }

        // Report the crime at this position, excluding this civilian as a witness
        if (assaultCrimeType != null)
        {
            CrimeReporter.ReportCrime(assaultCrimeType, transform.position, gameObject);
        }
        else
        {
            Debug.LogWarning($"[CivilianHealth] '{gameObject.name}' has no assaultCrimeType assigned. Crime not reported.", this);
        }

        // Start recovery countdown
        if (_recoveryCoroutine != null)
        {
            StopCoroutine(_recoveryCoroutine);
        }
        _recoveryCoroutine = StartCoroutine(RecoveryCountdown());
    }

    private IEnumerator TriggerKnockoutNextFrame()
    {
        yield return null;
        if (animator != null)
        {
            animator.SetTrigger("Knockout");
        }
    }

    private IEnumerator RecoveryCountdown()
    {
        Debug.Log($"[CivilianHealth] '{gameObject.name}' knocked out. Will recover in {recoveryTime} seconds.");
        
        yield return new WaitForSeconds(recoveryTime);
        
        Recover();
    }

    private void Recover()
    {
        if (!_isKnockedOut) return;

        Debug.Log($"[CivilianHealth] '{gameObject.name}' recovering from knockout.");

        // Reset knocked out state
        _isKnockedOut = false;

        // Reset health to full
        _health.Reset();

        // Trigger getup animation
        if (animator != null)
        {
            animator.SetTrigger("Getup");
            StartCoroutine(WaitForGetupToComplete());
        }
        else
        {
            ReEnableMovement();
        }
    }

    private IEnumerator WaitForGetupToComplete()
    {
        // DISABLE root motion so the animation plays in place without rotating
        animator.applyRootMotion = false;

        // Wait one frame for trigger to register
        yield return null;

        // Wait until we're in the Getup state
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Getup"))
        {
            yield return null;
        }

        // Wait for getup animation to complete
        while (animator.GetCurrentAnimatorStateInfo(0).IsName("Getup") && 
               animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.95f)
        {
            yield return null;
        }

        // RE-ENABLE root motion for walking
        animator.applyRootMotion = true;

        Debug.Log($"[CivilianHealth] Getup complete. Resuming movement.");

        // Re-enable movement
        ReEnableMovement();
    }

    private void ReEnableMovement()
    {
        CivilianNPC civilianAI = GetComponent<CivilianNPC>();
        if (civilianAI != null)
        {
            civilianAI.enabled = true;
        }
    }

    public void CancelRecovery()
    {
        if (_recoveryCoroutine != null)
        {
            StopCoroutine(_recoveryCoroutine);
            _recoveryCoroutine = null;
        }
    }
}