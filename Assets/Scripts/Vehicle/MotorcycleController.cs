using UnityEngine;

/// <summary>
/// Handles all motorcycle movement. Throttle, steering, lean, wheel spin.
/// Uses direct velocity control on the Rigidbody (not AddForce) for predictable
/// arcade feel. Raycast-based ground detection gates throttle.
///
/// Disabled until PlayerStateManager.EnterVehicle() is called on mount.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MotorcycleController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputReader inputReader;

    [Header("Visual References")]
    [Tooltip("The body mesh that leans into turns. Usually the 'Body' capsule child.")]
    [SerializeField] private Transform bodyVisual;
    [SerializeField] private Transform frontWheel;
    [SerializeField] private Transform rearWheel;

    [Header("Speed & Acceleration")]
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float brakeForce = 25f;
    [SerializeField] private float reverseMaxSpeed = 8f;
    [Tooltip("Speed multiplier while SprintInput is held.")]
    [SerializeField] private float boostMultiplier = 1.4f;
    [Tooltip("Deceleration applied when no throttle input, simulates engine braking + drag.")]
    [SerializeField] private float idleDeceleration = 5f;

    [Header("Steering")]
    [Tooltip("Turn rate in degrees per second when stationary.")]
    [SerializeField] private float steerSpeedLowSpeed = 120f;
    [Tooltip("Turn rate in degrees per second at max speed.")]
    [SerializeField] private float steerSpeedHighSpeed = 40f;

    [Header("Lean (Visual Only)")]
    [SerializeField] private float maxLeanAngle = 35f;
    [Tooltip("Higher = snappier lean response. Lower = smoother.")]
    [SerializeField] private float leanSmoothing = 5f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer = ~0; // default to everything; tighten later
    [SerializeField] private float groundCheckDistance = 1.0f;
    [Tooltip("Local offset from bike root where the ground ray originates.")]
    [SerializeField] private Vector3 groundCheckOrigin = new Vector3(0f, 0f, 0f);

    [Header("Wheel Spin (Visual)")]
    [Tooltip("Degrees of wheel spin per meter of travel. Tune for visual feel.")]
    [SerializeField] private float wheelSpinRate = 360f;

    // --- Runtime state (visible in Inspector for debugging) ---
    private Rigidbody _rb;
    [Header("Debug (Read-Only)")]
    [SerializeField] private bool _isGrounded;
    [SerializeField] private float _currentLean;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        if (inputReader == null)
            Debug.LogError($"[MotorcycleController] InputReader not assigned on {name}.");
        if (bodyVisual == null)
            Debug.LogWarning($"[MotorcycleController] BodyVisual not assigned on {name}. Lean will not be visible.");
    }

    private void FixedUpdate()
    {
        if (inputReader == null) return;

        Vector2 input = inputReader.MoveInput;
        bool boosting = inputReader.SprintInput;

        CheckGrounded();
        ApplyThrottle(input.y, boosting);
        ApplySteering(input.x);
    }

    private void Update()
    {
        // Visual updates run in Update for smooth framerate-tied rotation.
        if (inputReader == null) return;

        ApplyVisualLean(inputReader.MoveInput.x);
        ApplyWheelSpin();
    }

    // --- Ground detection ---
    private void CheckGrounded()
    {
        // Start the ray slightly ABOVE the bike's pivot to avoid starting inside the collider.
        // Cast downward for groundCheckDistance + that offset.
        float originOffset = 0.5f;
        Vector3 origin = transform.TransformPoint(groundCheckOrigin) + Vector3.up * originOffset;
        float castDistance = groundCheckDistance + originOffset;

        // Raycast ignoring trigger colliders.
        _isGrounded = Physics.Raycast(origin, Vector3.down, castDistance, groundLayer,
            QueryTriggerInteraction.Ignore);
    }

    // --- Throttle / reverse / brake ---
    private void ApplyThrottle(float throttleInput, bool boosting)
    {
        // Preserve vertical velocity (gravity) — only modify horizontal.
        Vector3 velocity = _rb.linearVelocity;
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
        float currentSpeed = Vector3.Dot(horizontal, transform.forward); // signed: + forward, - reverse

        // No throttle or airborne → natural deceleration (engine braking / air drag).
        if (!_isGrounded || Mathf.Approximately(throttleInput, 0f))
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, idleDeceleration * Time.fixedDeltaTime);
        }
        else if (throttleInput > 0f)
        {
            // Accelerate forward
            float targetMax = maxSpeed * (boosting ? boostMultiplier : 1f);
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetMax, acceleration * Time.fixedDeltaTime);
        }
        else // throttleInput < 0
        {
            // If moving forward, apply brakes. If stopped or moving backward, apply reverse.
            if (currentSpeed > 0.1f)
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeForce * Time.fixedDeltaTime);
            else
                currentSpeed = Mathf.MoveTowards(currentSpeed, -reverseMaxSpeed, acceleration * Time.fixedDeltaTime);
        }

        // Rebuild velocity: forward * speed, preserving gravity.
        Vector3 newHorizontal = transform.forward * currentSpeed;
        _rb.linearVelocity = new Vector3(newHorizontal.x, velocity.y, newHorizontal.z);
    }

    // --- Steering ---
    private void ApplySteering(float steerInput)
    {
        // Scale turn rate by current forward speed (agile at low speed, weighty at high speed).
        float forwardSpeed = Vector3.Dot(_rb.linearVelocity, transform.forward);
        float speedNorm = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / maxSpeed);
        float effectiveSteerSpeed = Mathf.Lerp(steerSpeedLowSpeed, steerSpeedHighSpeed, speedNorm);

        // Reverse steering direction when moving backward — feels natural.
        float directionSign = forwardSpeed < -0.1f ? -1f : 1f;

        float yawDelta = steerInput * effectiveSteerSpeed * directionSign * Time.fixedDeltaTime;
        if (Mathf.Abs(yawDelta) > 0.0001f)
        {
            Quaternion deltaRot = Quaternion.Euler(0f, yawDelta, 0f);
            _rb.MoveRotation(_rb.rotation * deltaRot);
        }
    }

    // --- Visual lean (LeanPivot tilts into the turn) ---
    private void ApplyVisualLean(float steerInput)
    {
        if (bodyVisual == null) return;

        // Target lean: airborne = no lean, else proportional to steer input.
        float targetLean = _isGrounded ? -steerInput * maxLeanAngle : 0f;
        _currentLean = Mathf.Lerp(_currentLean, targetLean, leanSmoothing * Time.deltaTime);

        // LeanPivot is axis-aligned (no base rotation), so Z-axis rotation = pure side-to-side tilt.
        bodyVisual.localRotation = Quaternion.Euler(0f, 0f, _currentLean);
    }

    // --- Wheel spin (visual) ---
    private void ApplyWheelSpin()
    {
        float forwardSpeed = Vector3.Dot(_rb.linearVelocity, transform.forward);
        float spinThisFrame = forwardSpeed * wheelSpinRate * Time.deltaTime;

        // Negative sign flips rotation direction — top of wheel moves forward when bike moves forward.
        if (frontWheel != null)
            frontWheel.Rotate(Vector3.up, -spinThisFrame, Space.Self);
        if (rearWheel != null)
            rearWheel.Rotate(Vector3.up, -spinThisFrame, Space.Self);
    }

    // --- Debug gizmos ---
    private void OnDrawGizmosSelected()
    {
        // Ground check ray — matches CheckGrounded() origin/distance.
        Gizmos.color = Application.isPlaying && _isGrounded ? Color.green : Color.red;
        float originOffset = 0.5f;
        Vector3 origin = transform.TransformPoint(groundCheckOrigin) + Vector3.up * originOffset;
        float castDistance = groundCheckDistance + originOffset;
        Gizmos.DrawLine(origin, origin + Vector3.down * castDistance);
    }
}