using UnityEngine;

/// <summary>
/// AI state machine for police NPCs. Handles pursuit, engagement,
/// disengagement, and knockout states.
/// 
/// Spawned and tracked by PoliceManager. Reads player position each frame
/// and acts based on current state and distance.
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
    [SerializeField] private float runDistance = 15f;

    [Header("Combat")]
    [Tooltip("Distance to enter Engage state and start attacking")]
    [SerializeField] private float engageRange = 2f;

    [Tooltip("Damage dealt per punch")]
    [SerializeField] private int attackDamage = 10;

    [Tooltip("Seconds between attacks")]
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Disengage")]
    [Tooltip("Seconds to walk away before being destroyed")]
    [SerializeField] private float disengageTime = 8f;

    [Header("Knockout")]
    [Tooltip("Seconds before knocked out police NPC is destroyed")]
    [SerializeField] private float knockoutDestroyDelay = 10f;

    // =========================================================================
    // RUNTIME STATE
    // =========================================================================

    private CharacterController _controller;
    private Animator _animator;
    private Transform _player;
    private Health _playerHealth;
    private PoliceState _currentState;
    private float _attackTimer;
    private float _disengageTimer;
    private Vector3 _disengageDirection;
    private bool _isKnockedOut;

    // =========================================================================
    // PUBLIC ACCESSORS
    // =========================================================================

    public PoliceState CurrentState => _currentState;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        // Find the player
        PlayerController playerController = FindAnyObjectByType<PlayerController>();
        if (playerController != null)
        {
            _player = playerController.transform;
            _playerHealth = _player.GetComponent<Health>();
        }
        else
        {
            Debug.LogError("[PoliceNPC] Cannot find PlayerController. Police AI disabled.", this);
            enabled = false;
            return;
        }

        // Start in pursue state
        SetState(PoliceState.Pursue);
    }

    private void Update()
    {
        if (_isKnockedOut) return;

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

        // Tick attack cooldown
        if (_attackTimer > 0f)
        {
            _attackTimer -= Time.deltaTime;
        }
    }

    // =========================================================================
    // STATE MANAGEMENT
    // =========================================================================

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
                // Walk away from player
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

    // =========================================================================
    // PURSUE — Move toward player
    // =========================================================================

    private void UpdatePursue()
    {
        if (_player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, _player.position);

        // Switch to Engage if close enough
        if (distanceToPlayer <= engageRange)
        {
            SetState(PoliceState.Engage);
            return;
        }

        // Determine speed based on distance
        float currentSpeed = distanceToPlayer > runDistance ? runSpeed : walkSpeed;

        // Move toward player
        Vector3 direction = (_player.position - transform.position);
        direction.y = 0;
        direction = direction.normalized;

        _controller.Move(direction * currentSpeed * Time.deltaTime);
        _controller.Move(Vector3.down * 9.81f * Time.deltaTime);

        // Face player
        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // Update animator
        if (_animator != null)
        {
            _animator.SetFloat("Speed", currentSpeed);
        }
    }

    // =========================================================================
    // ENGAGE — Attack player when in range
    // =========================================================================

    private void UpdateEngage()
    {
        if (_player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, _player.position);

        // If player moves out of range, go back to pursue
        if (distanceToPlayer > engageRange * 1.5f)
        {
            SetState(PoliceState.Pursue);
            return;
        }

        // Face player
        Vector3 direction = (_player.position - transform.position);
        direction.y = 0;
        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized);
        }

        // Stop moving
        _controller.Move(Vector3.down * 9.81f * Time.deltaTime);

        if (_animator != null)
        {
            _animator.SetFloat("Speed", 0f);
        }

        // Attack if cooldown is ready
        if (_attackTimer <= 0f)
        {
            PerformAttack();
        }
    }

    private void PerformAttack()
    {
        _attackTimer = attackCooldown;

        // Play attack animation
        if (_animator != null)
        {
            _animator.SetTrigger("Attack");
        }

        // Deal damage to player
        if (_playerHealth != null && !_playerHealth.IsDead)
        {
            Vector3 hitDir = (_player.position - transform.position).normalized;

            DamageInfo info = new DamageInfo(
                attackDamage,
                gameObject,
                hitDir,
                false // police punches are not heavy attacks
            );

            _playerHealth.TakeDamage(info);

            Debug.Log($"[PoliceNPC] '{gameObject.name}' attacked player for {attackDamage} damage.");
        }
    }

    // =========================================================================
    // DISENGAGE — Walk away and despawn
    // =========================================================================

    private void UpdateDisengage()
    {
        _disengageTimer -= Time.deltaTime;

        if (_disengageTimer <= 0f)
        {
            // Time's up — remove from PoliceManager and destroy
            if (PoliceManager.Instance != null)
            {
                PoliceManager.Instance.UnregisterPolice(gameObject);
            }
            Destroy(gameObject);
            return;
        }

        // Walk away from player
        _controller.Move(_disengageDirection * walkSpeed * Time.deltaTime);
        _controller.Move(Vector3.down * 9.81f * Time.deltaTime);

        // Face movement direction
        if (_disengageDirection.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(_disengageDirection);
        }

        if (_animator != null)
        {
            _animator.SetFloat("Speed", walkSpeed);
        }
    }

    // =========================================================================
    // KNOCKOUT — Called by PoliceHealth when health reaches 0
    // =========================================================================

    private void HandleKnockout()
    {
        if (_isKnockedOut) return;
        _isKnockedOut = true;

        Debug.Log($"[PoliceNPC] '{gameObject.name}' knocked out.");

        // Stop movement
        if (_animator != null)
        {
            _animator.SetFloat("Speed", 0f);
            _animator.SetTrigger("Knockout");
        }

        // Disable this AI
        enabled = false;

        // Destroy after delay
        Destroy(gameObject, knockoutDestroyDelay);
    }

    // =========================================================================
    // PUBLIC — Called by PoliceManager when heat clears
    // =========================================================================

    /// <summary>
    /// Tell this police NPC to disengage. Called by PoliceManager when heat drops to 0.
    /// </summary>
    public void Disengage()
    {
        if (_isKnockedOut) return;
        SetState(PoliceState.Disengage);
    }
}