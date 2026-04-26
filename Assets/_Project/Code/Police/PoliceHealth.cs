using UnityEngine;
using MCGame.Combat;

/// <summary>
/// Bridges Health component on police NPCs to the crime system.
/// When a police NPC is knocked out, reports "Assault on Officer" crime
/// and unregisters from PoliceManager.
/// </summary>
[RequireComponent(typeof(Health))]
public class PoliceHealth : MonoBehaviour
{
    [Header("Crime Reporting")]
    [Tooltip("Crime type reported when this officer is knocked out")]
    [SerializeField] private CrimeType assaultOfficerCrimeType;

    private Health _health;
    private Animator _animator;
    private bool _isKnockedOut;

    private void Awake()
    {
        _health = GetComponent<Health>();
        _animator = GetComponentInChildren<Animator>();
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

        // Play hit animation
        if (_animator != null && !_health.IsDead)
        {
            _animator.SetTrigger("Hit");
        }

        // Tell PoliceNPC to stun
        PoliceNPC policeAI = GetComponent<PoliceNPC>();
        if (policeAI != null)
        {
            policeAI.ApplyHitStun();
        }
    }

    private void HandleDied()
    {
        if (_isKnockedOut) return;
        _isKnockedOut = true;

        // Tell PoliceNPC to enter knockout state
        PoliceNPC policeAI = GetComponent<PoliceNPC>();
        if (policeAI != null)
        {
            policeAI.SetState(PoliceNPC.PoliceState.KnockedOut);
        }

        // Report crime — assaulting an officer is a serious crime
        if (assaultOfficerCrimeType != null)
        {
            CrimeReporter.ReportCrime(assaultOfficerCrimeType, transform.position, gameObject);
        }
        else
        {
            Debug.LogWarning($"[PoliceHealth] '{gameObject.name}' has no assaultOfficerCrimeType assigned.", this);
        }

        // Unregister from PoliceManager
        if (PoliceManager.Instance != null)
        {
            PoliceManager.Instance.UnregisterPolice(gameObject);
        }
    }
}