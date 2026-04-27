using UnityEngine;
using System.Collections;
using MCGame.Combat;
using MCGame.Core;
using MCGame.Gameplay.Crime;

namespace MCGame.Gameplay.AI
{
    /// <summary>
    /// Bridges Health component on civilians to the crime reporting system.
    /// Handles knockout/recovery cycle and animation routing.
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

            if (animator != null && !_health.IsDead)
            {
                animator.SetTrigger(AnimatorParams.Hit);
            }
        }

        private void HandleDied()
        {
            if (_isKnockedOut) return;
            _isKnockedOut = true;

            CivilianNPC civilianAI = GetComponent<CivilianNPC>();
            if (civilianAI != null)
            {
                civilianAI.enabled = false;
            }

            if (animator != null)
            {
                animator.SetFloat(AnimatorParams.Speed, 0f);
                StartCoroutine(TriggerKnockoutNextFrame());
            }

            if (assaultCrimeType != null)
            {
                CrimeReporter.ReportCrime(assaultCrimeType, transform.position, gameObject);
            }
            else
            {
                Debug.LogWarning($"[CivilianHealth] '{gameObject.name}' has no assaultCrimeType assigned. Crime not reported.", this);
            }

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
                animator.SetTrigger(AnimatorParams.Knockout);
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

            _isKnockedOut = false;

            _health.Reset();

            if (animator != null)
            {
                animator.SetTrigger(AnimatorParams.Getup);
                StartCoroutine(WaitForGetupToComplete());
            }
            else
            {
                ReEnableMovement();
            }
        }

        private IEnumerator WaitForGetupToComplete()
        {
            animator.applyRootMotion = false;

            yield return null;

            // Note: IsName checks against state names (different namespace from parameter names).
            // Leaving as string for clarity — these aren't AnimatorParams' concern.
            while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Getup"))
            {
                yield return null;
            }

            while (animator.GetCurrentAnimatorStateInfo(0).IsName("Getup") &&
                   animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.95f)
            {
                yield return null;
            }

            animator.applyRootMotion = true;

            Debug.Log($"[CivilianHealth] Getup complete. Resuming movement.");

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
}