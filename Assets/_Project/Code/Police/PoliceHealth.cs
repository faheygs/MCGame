using UnityEngine;
using MCGame.Combat;
using MCGame.Gameplay.Crime;

namespace MCGame.Gameplay.Police
{
    /// <summary>
    /// Bridges Health component on police NPCs to the crime system.
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

            if (_animator != null && !_health.IsDead)
            {
                _animator.SetTrigger("Hit");
            }

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

            PoliceNPC policeAI = GetComponent<PoliceNPC>();
            if (policeAI != null)
            {
                policeAI.SetState(PoliceNPC.PoliceState.KnockedOut);
            }

            if (assaultOfficerCrimeType != null)
            {
                CrimeReporter.ReportCrime(assaultOfficerCrimeType, transform.position, gameObject);
            }
            else
            {
                Debug.LogWarning($"[PoliceHealth] '{gameObject.name}' has no assaultOfficerCrimeType assigned.", this);
            }

            if (PoliceManager.Instance != null)
            {
                PoliceManager.Instance.UnregisterPolice(gameObject);
            }
        }
    }
}