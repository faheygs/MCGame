using UnityEngine;
using MCGame.Input;

public class ThirdPersonCamera : MonoBehaviour
{
    [System.Serializable]
    public struct CameraConfig
    {
        public float followDistance;
        public float followHeight;
        public float fieldOfView;
    }

    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Foot Config")]
    [SerializeField] private CameraConfig footConfig = new CameraConfig
    {
        followDistance = 3.5f,
        followHeight = 0.5f,
        fieldOfView = 60f
    };

    [Header("Vehicle Config")]
    [SerializeField] private CameraConfig vehicleConfig = new CameraConfig
    {
        followDistance = 5.5f,
        followHeight = 1.2f,
        fieldOfView = 70f
    };

    [Header("Transition")]
    [Tooltip("Seconds for the camera to blend between foot and vehicle configs. Lower = snappier.")]
    [SerializeField] private float configTransitionTime = 0.5f;

    [Header("Camera Behavior")]
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float positionSmoothTime = 0.05f;

    [Header("Vertical Angle Limits")]
    [SerializeField] private float minVerticalAngle = -20f;
    [SerializeField] private float maxVerticalAngle = 60f;

    [Header("Input")]
    [SerializeField] private InputReader inputReader;

    [Header("References")]
    [Tooltip("Camera component whose FOV is animated. Auto-assigned from this GameObject if empty.")]
    [SerializeField] private Camera cam;

    // --- Runtime state ---
    private float _yaw;
    private float _pitch;
    private Vector3 _smoothVelocity;
    private bool _inputEnabled = true;

    // Current interpolated camera values (chase the active config each frame).
    private float _currentDistance;
    private float _currentHeight;
    private float _currentFov;

    // Velocity refs for SmoothDamp on each config value.
    private float _distanceVel;
    private float _heightVel;
    private float _fovVel;

    private void Awake()
    {
        if (cam == null)
            cam = GetComponent<Camera>();

        // Initialize current values from foot config so there's no first-frame snap.
        _currentDistance = footConfig.followDistance;
        _currentHeight = footConfig.followHeight;
        _currentFov = footConfig.fieldOfView;

        if (cam != null)
            cam.fieldOfView = _currentFov;
    }

    private void OnEnable()
    {
        if (PlayerStateManager.Instance != null)
            PlayerStateManager.Instance.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        if (PlayerStateManager.Instance != null)
            PlayerStateManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    private void Start()
    {
        // PlayerStateManager may not have existed during OnEnable (script execution order).
        // Ensure subscription is in place. Double-subscribe is prevented by removing first.
        if (PlayerStateManager.Instance != null)
        {
            PlayerStateManager.Instance.OnStateChanged -= HandleStateChanged;
            PlayerStateManager.Instance.OnStateChanged += HandleStateChanged;
        }
    }

    private void LateUpdate()
    {
        HandleRotation();
        UpdateConfigValues();
        HandlePosition();
        ApplyFov();
    }

    private void HandleRotation()
    {
        if (!_inputEnabled) return;

        Vector2 lookInput = inputReader.LookInput;

        _yaw += lookInput.x * rotationSpeed * Time.deltaTime;
        _pitch -= lookInput.y * rotationSpeed * Time.deltaTime;
        _pitch = Mathf.Clamp(_pitch, minVerticalAngle, maxVerticalAngle);
    }

    private void UpdateConfigValues()
    {
        CameraConfig targetConfig = GetActiveConfig();

        _currentDistance = Mathf.SmoothDamp(_currentDistance, targetConfig.followDistance,
            ref _distanceVel, configTransitionTime);
        _currentHeight = Mathf.SmoothDamp(_currentHeight, targetConfig.followHeight,
            ref _heightVel, configTransitionTime);
        _currentFov = Mathf.SmoothDamp(_currentFov, targetConfig.fieldOfView,
            ref _fovVel, configTransitionTime);
    }

    private CameraConfig GetActiveConfig()
    {
        if (PlayerStateManager.Instance != null && PlayerStateManager.Instance.IsInVehicle)
            return vehicleConfig;
        return footConfig;
    }

    private void HandlePosition()
    {
        if (!_inputEnabled) return;
        if (target == null) return;

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        Vector3 targetPosition = target.position + Vector3.up * _currentHeight;
        Vector3 desiredPosition = targetPosition + rotation * Vector3.back * _currentDistance;

        // Smooth the camera position to eliminate stutter
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref _smoothVelocity,
            positionSmoothTime
        );

        transform.LookAt(targetPosition);
    }

    private void ApplyFov()
    {
        if (cam != null)
            cam.fieldOfView = _currentFov;
    }

    private void HandleStateChanged(PlayerStateManager.PlayerState newState)
    {
        // No immediate action needed — UpdateConfigValues() picks up the new target next frame
        // and the per-frame SmoothDamp handles the transition.
        // This handler exists for future needs (e.g., snap-to on certain transitions, sound cues).
    }

    public Quaternion GetCameraRotation()
    {
        return Quaternion.Euler(0f, _yaw, 0f);
    }

    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
    }

    public void ConsumeAccumulatedInput()
    {
        // Reset any pending look input so closing the map doesn't cause a snap
        // We do this by reading and discarding the current mouse delta
        if (inputReader != null)
            inputReader.ResetLookInput();
    }
}