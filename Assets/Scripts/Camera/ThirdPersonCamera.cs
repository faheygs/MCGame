using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Camera Settings")]
    [SerializeField] private float followDistance = 3.5f;
    [SerializeField] private float followHeight = 0.5f;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float positionSmoothTime = 0.05f;

    [Header("Vertical Angle Limits")]
    [SerializeField] private float minVerticalAngle = -20f;
    [SerializeField] private float maxVerticalAngle = 60f;

    [Header("Input")]
    [SerializeField] private InputReader inputReader;

    private float _yaw;
    private float _pitch;
    private Vector3 _smoothVelocity;
    private bool _inputEnabled = true;

    private void LateUpdate()
    {
        HandleRotation();
        HandlePosition();
    }

    private void HandleRotation()
    {
        if (!_inputEnabled) return;
        
        Vector2 lookInput = inputReader.LookInput;

        _yaw += lookInput.x * rotationSpeed * Time.deltaTime;
        _pitch -= lookInput.y * rotationSpeed * Time.deltaTime;
        _pitch = Mathf.Clamp(_pitch, minVerticalAngle, maxVerticalAngle);
    }

    private void HandlePosition()
    {
        if (!_inputEnabled) return;

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        Vector3 targetPosition = target.position + Vector3.up * followHeight;
        Vector3 desiredPosition = targetPosition + rotation * Vector3.back * followDistance;

        // Smooth the camera position to eliminate stutter
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref _smoothVelocity,
            positionSmoothTime
        );

        transform.LookAt(targetPosition);
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