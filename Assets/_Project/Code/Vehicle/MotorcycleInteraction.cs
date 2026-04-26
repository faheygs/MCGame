using UnityEngine;
using MCGame.Core;

/// <summary>
/// Handles player proximity and mount/dismount coordination for a single motorcycle.
/// Implements IInteractable — registers with InteractionManager when the player is
/// either (a) in range and on foot, or (b) currently mounted on this bike.
///
/// Does not subscribe to input directly. InteractionManager routes the interact press
/// to OnInteract() when this is the currently-selected interactable.
/// </summary>
[RequireComponent(typeof(MotorcycleController))]
public class MotorcycleInteraction : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private MotorcycleController motorcycleController;
    [Tooltip("Where the player is placed when mounting (parent transform).")]
    [SerializeField] private Transform seatPosition;
    [Tooltip("Where the player is placed when dismounting.")]
    [SerializeField] private Transform dismountPosition;

    [Header("Proximity")]
    [SerializeField] private float interactRange = 2f;

    [Header("Priority")]
    [Tooltip("Priority when player is on foot and able to mount.")]
    [SerializeField] private int mountPriority = 10;
    [Tooltip("Priority when player is mounted on this bike. Typically very high so dismount always wins.")]
    [SerializeField] private int dismountPriority = 100;

    private Transform _player;
    private bool _isRegistered;

    private void Awake()
    {
        if (motorcycleController == null)
            motorcycleController = GetComponent<MotorcycleController>();
    }

    private void Start()
    {
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

    private void OnDisable()
    {
        // Always ensure we unregister on disable.
        if (_isRegistered && InteractionManager.Instance != null)
        {
            InteractionManager.Instance.Unregister(this);
            _isRegistered = false;
        }
    }

    private void Update()
    {
        if (_player == null) return;
        if (InteractionManager.Instance == null) return;

        UpdateRegistration();
    }

    private void UpdateRegistration()
    {
        bool shouldBeRegistered = ShouldBeRegistered();

        if (shouldBeRegistered && !_isRegistered)
        {
            InteractionManager.Instance.Register(this);
            _isRegistered = true;
        }
        else if (!shouldBeRegistered && _isRegistered)
        {
            InteractionManager.Instance.Unregister(this);
            _isRegistered = false;
        }
    }

    private bool ShouldBeRegistered()
    {
        // If the player is mounted on THIS bike, always stay registered (for dismount).
        if (IsPlayerMountedOnThisBike()) return true;

        // If the player is mounted on a DIFFERENT bike, we're not relevant.
        if (PlayerStateManager.Instance != null && PlayerStateManager.Instance.IsInVehicle)
            return false;

        // Otherwise, register if player is on foot and within range.
        float distance = Vector3.Distance(transform.position, _player.position);
        return distance <= interactRange;
    }

    private bool IsPlayerMountedOnThisBike()
    {
        if (PlayerStateManager.Instance == null) return false;
        if (!PlayerStateManager.Instance.IsInVehicle) return false;
        return PlayerStateManager.Instance.CurrentVehicle == (MonoBehaviour)motorcycleController;
    }

    // --- IInteractable implementation ---

    public int Priority =>
        IsPlayerMountedOnThisBike() ? dismountPriority : mountPriority;

    public Vector3 GetPosition() => transform.position;

    public string GetPromptText() => "Mount";
    
    // Hide prompt while mounted — the dismount action is learned-by-symmetry.
    // Press E to mount, press E to dismount, no persistent reminder needed.
    public bool ShouldShowPrompt() => !IsPlayerMountedOnThisBike();

    public bool CanInteract()
    {
        if (PlayerStateManager.Instance == null) return false;

        // Can always dismount if mounted on this bike.
        if (IsPlayerMountedOnThisBike()) return true;

        // Can mount only if player is on foot and references are valid.
        if (PlayerStateManager.Instance.IsInVehicle) return false;
        if (seatPosition == null) return false;

        return true;
    }

    public void OnInteract()
    {
        if (!CanInteract()) return;

        if (IsPlayerMountedOnThisBike())
            TryDismount();
        else
            TryMount();
    }

    // --- Mount / Dismount ---

    private void TryMount()
    {
        if (seatPosition == null)
        {
            Debug.LogError($"[MotorcycleInteraction] SeatPosition not assigned on {name}.");
            return;
        }

        // Tell PlayerStateManager to enter the vehicle.
        PlayerStateManager.Instance.EnterVehicle(motorcycleController, seatPosition);

        // Enable the motorcycle controller so the bike responds to input.
        motorcycleController.enabled = true;
    }

    private void TryDismount()
    {
        if (dismountPosition == null)
        {
            Debug.LogError($"[MotorcycleInteraction] DismountPosition not assigned on {name}.");
            return;
        }

        // Disable the motorcycle controller so the bike stops responding to input.
        motorcycleController.enabled = false;

        // Tell PlayerStateManager to exit the vehicle.
        PlayerStateManager.Instance.ExitVehicle(dismountPosition);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}