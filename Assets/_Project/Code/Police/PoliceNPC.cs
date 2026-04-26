using UnityEngine;
using MCGame.Combat;
using MCGame.Gameplay.Player;

namespace MCGame.Gameplay.Police
{
    /// <summary>
    /// AI state machine for police NPCs.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Health))]
    public class PoliceNPC : MonoBehaviour
    {
        public enum PoliceState
        {
            Pursue,
            Engage,
            Disengage,
            KnockedOut
        }

        [Header("Movement")]
        [Tooltip("Walking speed when far from player")]
        [SerializeField] private float walkSpeed = 2.5f;

        [Tooltip("Running speed when closing distance")]
        [SerializeField] private float runSpeed = 5f;

        [Tooltip("Distance threshold to switch from walk to run")]
        [SerializeField] private float runDistance = 2f;

        [Header("Combat")]
        [Tooltip("Distance to enter Engage state and start attacking")]
        [SerializeField] private float engageRange = 1.5f;

        [Tooltip("Damage dealt per punch")]
        [SerializeField] private int attackDamage = 10;

        [Tooltip("Seconds between attacks")]
        [SerializeField] private float attackCooldown = 2.5f;

        [Header("Disengage")]
        [Tooltip("Seconds to walk away before being destroyed")]
        [SerializeField] private float disengageTime = 8f;

        [Header("Knockout")]
        [Tooltip("Seconds before knocked out police NPC is destroyed")]
        [SerializeField] private float knockoutDestroyDelay = 10f;

        [Header("Hit Stun")]
        [Tooltip("How long the police NPC is stunned after being hit")]
        [SerializeField] private float hitStunDuration = 0.8f;

        private bool _isStunned;
        private float _stunTimer;

        private CharacterController _controller;
        private Animator _animator;
        private Transform _player;
        private Health _playerHealth;
        private PoliceState _currentState;
        private float _attackTimer;
        private float _disengageTimer;
        private Vector3 _disengageDirection;
        private bool _isKnockedOut;

        public PoliceState CurrentState => _currentState;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _animator = GetComponentInChildren<Animator>();
        }

        private void Start()
        {
            _player = PlayerService.PlayerTransform;
            _playerHealth = PlayerService.PlayerHealth;

            if (_player == null || _playerHealth == null)
            {
                Debug.LogError("[PoliceNPC] PlayerService has no registered player or health. Police AI disabled.", this);
                enabled = false;
                return;
            }

            SetState(PoliceState.Pursue);
        }

        private void Update()
        {
            if (_isKnockedOut) return;

            if (_isStunned)
            {
                _stunTimer -= Time.deltaTime;
                if (_stunTimer <= 0f)
                {
                    _isStunned = false;
                }
                return;
            }

            switch (_currentState)
            {
                case PoliceState.Pursue:
                    UpdatePursue();
                    break;
                case PoliceState.Engage:
                    UpdateEngage();
                    break;
                case PoliceState.Disengage:
                    UpdateDisengage();
                    break;
            }

            if (_attackTimer > 0f)
            {
                _attackTimer -= Time.deltaTime;
            }
        }

        public void SetState(PoliceState newState)
        {
            if (_isKnockedOut) return;

            _currentState = newState;

            switch (newState)
            {
                case PoliceState.Pursue:
                    Debug.Log($"[PoliceNPC] '{gameObject.name}' → PURSUE");
                    break;

                case PoliceState.Engage:
                    Debug.Log($"[PoliceNPC] '{gameObject.name}' → ENGAGE");
                    break;

                case PoliceState.Disengage:
                    Debug.Log($"[PoliceNPC] '{gameObject.name}' → DISENGAGE");
                    _disengageTimer = disengageTime;
                    if (_player != null)
                    {
                        _disengageDirection = (transform.position - _player.position).normalized;
                        _disengageDirection.y = 0;
                    }
                    else
                    {
                        _disengageDirection = -transform.forward;
                    }
                    break;

                case PoliceState.KnockedOut:
                    HandleKnockout();
                    break;
            }
        }

        private void UpdatePursue()
        {
            if (_player == null) return;

            float distanceToPlayer = Vector3.Distance(transform.position, _player.position);

            if (distanceToPlayer <= engageRange)
            {
                SetState(PoliceState.Engage);
                return;
            }

            float currentSpeed = distanceToPlayer > runDistance ? runSpeed : walkSpeed;

            Vector3 direction = (_player.position - transform.position);
            direction.y = 0;
            direction = direction.normalized;

            _controller.Move(direction * currentSpeed * Time.deltaTime);
            _controller.Move(Vector3.down * 9.81f * Time.deltaTime);

            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            if (_animator != null)
            {
                _animator.SetFloat("Speed", currentSpeed);
            }
        }

        private void UpdateEngage()
        {
            if (_player == null) return;

            float distanceToPlayer = Vector3.Distance(transform.position, _player.position);

            if (distanceToPlayer > engageRange * 1.5f)
            {
                SetState(PoliceState.Pursue);
                return;
            }

            Vector3 direction = (_player.position - transform.position);
            direction.y = 0;
            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized);
            }

            _controller.Move(Vector3.down * 9.81f * Time.deltaTime);

            if (_animator != null)
            {
                _animator.SetFloat("Speed", 0f);
            }

            if (_attackTimer <= 0f)
            {
                PerformAttack();
            }
        }

        private void PerformAttack()
        {
            _attackTimer = attackCooldown;

            if (_animator != null)
            {
                _animator.SetTrigger("Attack");
            }

            StartCoroutine(DealDamageDelayed(0.4f));
        }

        private System.Collections.IEnumerator DealDamageDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (_player == null || _isKnockedOut) yield break;

            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist > engageRange * 1.5f) yield break;

            if (_playerHealth != null && !_playerHealth.IsDead)
            {
                Vector3 hitDir = (_player.position - transform.position).normalized;

                DamageInfo info = new DamageInfo(
                    attackDamage,
                    gameObject,
                    hitDir,
                    false
                );

                _playerHealth.TakeDamage(info);

                Debug.Log($"[PoliceNPC] '{gameObject.name}' hit player for {attackDamage} damage.");
            }
        }

        private void UpdateDisengage()
        {
            _disengageTimer -= Time.deltaTime;

            if (_disengageTimer <= 0f)
            {
                if (PoliceManager.Instance != null)
                {
                    PoliceManager.Instance.UnregisterPolice(gameObject);
                }
                Destroy(gameObject);
                return;
            }

            _controller.Move(_disengageDirection * walkSpeed * Time.deltaTime);
            _controller.Move(Vector3.down * 9.81f * Time.deltaTime);

            if (_disengageDirection.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(_disengageDirection);
            }

            if (_animator != null)
            {
                _animator.SetFloat("Speed", walkSpeed);
            }
        }

        private void HandleKnockout()
        {
            if (_isKnockedOut) return;
            _isKnockedOut = true;

            Debug.Log($"[PoliceNPC] '{gameObject.name}' knocked out.");

            if (_animator != null)
            {
                _animator.SetFloat("Speed", 0f);
                _animator.SetTrigger("Knockout");
            }

            enabled = false;

            Destroy(gameObject, knockoutDestroyDelay);
        }

        public void Disengage()
        {
            if (_isKnockedOut) return;
            SetState(PoliceState.Disengage);
        }

        public void ApplyHitStun()
        {
            if (_isKnockedOut) return;

            _isStunned = true;
            _stunTimer = hitStunDuration;

            _attackTimer = attackCooldown * 0.5f;

            if (_animator != null)
            {
                _animator.SetFloat("Speed", 0f);
            }
        }
    }
}