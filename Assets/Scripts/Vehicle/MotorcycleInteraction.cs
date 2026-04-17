using UnityEngine;

/// <summary>
/// Handles player proximity, mount/dismount prompt, and E-press to mount/dismount.
/// Lives on the Motorcycle GameObject alongside MotorcycleController.
///
/// Does not own state — PlayerStateManager does. This script just requests transitions.
/// Does not control camera — ThirdPersonCamera subscribes to PlayerStateManager.OnStateChanged.
/// </summary>
[RequireComponent(typeof(MotorcycleController))]
public class MotorcycleInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private MotorcycleController motorcycleController;
    [Tooltip("Where the player is placed when mounting (parent transform).")]
    [SerializeField] private Transform seatPosition;
    [Tooltip("Where the player is placed when dismounting.")]
    [SerializeField] private Transform dismountPosition;

    [Header("Proximity")]
    [SerializeField] private float interactRange = 3f;

    private Transform _player;
    private bool _playerInRange;
    private bool _isRegistered;

    private void Awake()
    {
        if (motorcycleController == null)
            motorcycleController = GetComponent<MotorcycleController>();
    }

    private void Start()
    {
        // Cache player reference once.
        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO == null)
        {
            Debug.LogError($"[MotorcycleInteraction] No GameObject tagged 'Player' in scene.");
            enabled = false;
            return;
        }
        _player = playerGO.transform;

        // Safety: ensure the MotorcycleController starts disabled.
        if (motorcycleController != null)
            motorcycleController.enabled = false;
    }

    private void OnEnable()
    {
        if (inputReader != null)
            inputReader.InteractPressed += OnInteractPressed;
    }

    private void OnDisable()
    {
        if (inputReader != null)
            inputReader.InteractPressed -= OnInteractPressed;

        // Safety: unregister prompt if this script is disabled mid-proximity.
        if (_isRegistered && UIManager.Instance != null)
        {
            UIManager.Instance.UnregisterVehicleInteractable();
            _isRegistered = false;
        }
    }

    private void Update()
    {
        if (_player == null) return;
        if (UIManager.Instance == null) return;
        if (PlayerStateManager.Instance == null) return;

        UpdateProximityPrompt();
    }

    private void UpdateProximityPrompt()
    {
        // While player is mounted, do not show "Mount" prompt.
        bool playerIsOnFoot = !PlayerStateManager.Instance.IsInVehicle;

        float distance = Vector3.Distance(transform.position, _player.position);
        bool inRange = distance <= interactRange;
        _playerInRange = inRange;

        bool shouldBeRegistered = inRange && playerIsOnFoot;

        if (shouldBeRegistered && !_isRegistered)
        {
            UIManager.Instance.RegisterVehicleInteractable();
            _isRegistered = true;
        }
        else if (!shouldBeRegistered && _isRegistered)
        {
            UIManager.Instance.UnregisterVehicleInteractable();
            _isRegistered = false;
        }
    }

    // --- Input callback ---

    private void OnInteractPressed()
    {
        if (PlayerStateManager.Instance == null) return;

        bool isMounted = PlayerStateManager.Instance.IsInVehicle;
        bool mountedOnThisBike = isMounted &&
            PlayerStateManager.Instance.CurrentVehicle == (MonoBehaviour)motorcycleController;

        if (!isMounted && _playerInRange)
        {
            TryMount();
        }
        else if (mountedOnThisBike)
        {
            TryDismount();
        }
        // Otherwise: not our bike, ignore. Another bike (or mission giver) will handle it.
    }

    // --- Mount ---

    private void TryMount()
    {
        if (seatPosition == null)
        {
            Debug.LogError($"[MotorcycleInteraction] SeatPosition not assigned on {name}.");
            return;
        }

        // 1. Unregister the prompt FIRST so it doesn't flicker during the state change.
        if (_isRegistered)
        {
            UIManager.Instance.UnregisterVehicleInteractable();
            _isRegistered = false;
        }

        // 2. Tell PlayerStateManager to enter the vehicle. It handles:
        //    - Disabling PlayerController + CharacterController
        //    - Parenting player to seat
        //    - Firing OnStateChanged event
        PlayerStateManager.Instance.EnterVehicle(motorcycleController, seatPosition);

        // 3. Enable the motorcycle controller so the bike responds to input.
        motorcycleController.enabled = true;
    }

    // --- Dismount ---

    private void TryDismount()
    {
        if (dismountPosition == null)
        {
            Debug.LogError($"[MotorcycleInteraction] DismountPosition not assigned on {name}.");
            return;
        }

        // 1. Disable the motorcycle controller so the bike stops responding to input.
        motorcycleController.enabled = false;

        // 2. Tell PlayerStateManager to exit the vehicle. It handles:
        //    - Unparenting player
        //    - Moving player to dismount position
        //    - Re-enabling CharacterController + PlayerController
        //    - Firing OnStateChanged event
        PlayerStateManager.Instance.ExitVehicle(dismountPosition);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}