using System;
using UnityEngine;

/// <summary>
/// Owns the player's current state (OnFoot vs InVehicle) and handles transitions.
/// Single entry point for all state changes — nothing else should directly enable
/// or disable PlayerController / MotorcycleController.
///
/// Lives on the Player GameObject. Exposes a static Instance for convenience.
/// </summary>
public class PlayerStateManager : MonoBehaviour
{
    public enum PlayerState
    {
        OnFoot,
        InVehicle
    }

    public static PlayerStateManager Instance { get; private set; }

    [Header("Player Components")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CharacterController characterController;

    [Header("Current State (Read-Only)")]
    [SerializeField] private PlayerState currentState = PlayerState.OnFoot;

    // Currently mounted vehicle (null when OnFoot).
    private MonoBehaviour _currentVehicle;
    // Timestamp of last state change — used to suppress input consumers immediately after a transition.
    private float _lastStateChangeTime = -999f;

    [Tooltip("Grace period (seconds) after a state change during which other interactables ignore inputs.")]
    [SerializeField] private float stateChangeCooldown = 0.2f;

    /// <summary>
    /// True if the player state changed within the last stateChangeCooldown seconds.
    /// Other interaction scripts should check this and bail out if true, to prevent
    /// the same button press being consumed by both mount/dismount and another interaction.
    /// </summary>
    public bool JustChangedState => (Time.time - _lastStateChangeTime) < stateChangeCooldown;

    /// <summary>Current player state. Read-only to outside callers.</summary>
    public PlayerState CurrentState => currentState;

    /// <summary>True when the player is currently mounted on a vehicle.</summary>
    public bool IsInVehicle => currentState == PlayerState.InVehicle;

    /// <summary>Reference to the currently mounted vehicle, or null.</summary>
    public MonoBehaviour CurrentVehicle => _currentVehicle;

    /// <summary>
    /// Fires whenever the state changes. Passes the new state.
    /// Camera, animation, and HUD systems should subscribe to this.
    /// </summary>
    public event Action<PlayerState> OnStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[PlayerStateManager] Duplicate instance on {name}. Destroying this one.");
            Destroy(this);
            return;
        }

        Instance = this;

        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        if (playerController == null)
            Debug.LogError($"[PlayerStateManager] PlayerController reference missing on {name}.");
        if (characterController == null)
            Debug.LogError($"[PlayerStateManager] CharacterController reference missing on {name}.");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Transition the player into a vehicle. Called by MotorcycleInteraction
    /// (or any future vehicle interaction script) when the player mounts.
    /// </summary>
    /// <param name="vehicle">The vehicle being mounted. Must expose a SeatPosition transform.</param>
    /// <param name="seatPosition">Where the player should be parented to.</param>
    public void EnterVehicle(MonoBehaviour vehicle, Transform seatPosition)
    {
        if (currentState == PlayerState.InVehicle)
        {
            Debug.LogWarning("[PlayerStateManager] EnterVehicle called but player already in a vehicle. Ignoring.");
            return;
        }

        if (vehicle == null || seatPosition == null)
        {
            Debug.LogError("[PlayerStateManager] EnterVehicle called with null vehicle or seat position.");
            return;
        }

        // 1. Disable on-foot movement + collision
        if (playerController != null) playerController.enabled = false;
        if (characterController != null) characterController.enabled = false;

        // 2. Parent player to seat and snap to seat position/rotation
        transform.SetParent(seatPosition, worldPositionStays: false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // 3. Record state
        _currentVehicle = vehicle;
        SetState(PlayerState.InVehicle);
    }

    /// <summary>
    /// Transition the player out of a vehicle. Called by MotorcycleInteraction
    /// when the player dismounts.
    /// </summary>
    /// <param name="dismountPosition">Where the player should be placed on exit.</param>
    public void ExitVehicle(Transform dismountPosition)
    {
        if (currentState == PlayerState.OnFoot)
        {
            Debug.LogWarning("[PlayerStateManager] ExitVehicle called but player already on foot. Ignoring.");
            return;
        }

        if (dismountPosition == null)
        {
            Debug.LogError("[PlayerStateManager] ExitVehicle called with null dismount position.");
            return;
        }

        // 1. Unparent from vehicle (back to scene root)
        transform.SetParent(null, worldPositionStays: false);

        // 2. Move to dismount position in world space
        transform.position = dismountPosition.position;
        transform.rotation = dismountPosition.rotation;

        // 3. Re-enable on-foot movement + collision
        // NOTE: CharacterController must be re-enabled BEFORE PlayerController,
        // because PlayerController.Update() reads from the CharacterController.
        if (characterController != null) characterController.enabled = true;
        if (playerController != null) playerController.enabled = true;

        // 4. Clear state
        _currentVehicle = null;
        SetState(PlayerState.OnFoot);
    }

    private void SetState(PlayerState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        _lastStateChangeTime = Time.time;
        OnStateChanged?.Invoke(currentState);
    }
}