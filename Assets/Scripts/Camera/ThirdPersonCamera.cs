using UnityEngine;

// ThirdPersonCamera orbits around the player based on Look input.
// It maintains a fixed follow distance and clamps vertical angle
// to prevent the camera from flipping.
//
// This script lives on the Main Camera object.

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target; // The player transform

    [Header("Camera Settings")]
    [SerializeField] private float followDistance = 5f;
    [SerializeField] private float followHeight = 2f;
    [SerializeField] private float rotationSpeed = 180f;

    [Header("Vertical Angle Limits")]
    [SerializeField] private float minVerticalAngle = -20f;
    [SerializeField] private float maxVerticalAngle = 60f;

    [Header("Input")]
    [SerializeField] private InputReader inputReader;

    private float _yaw;   // Horizontal rotation (left/right)
    private float _pitch; // Vertical rotation (up/down)

    private void LateUpdate()
    {
        HandleRotation();
        HandlePosition();
    }

    private void HandleRotation()
    {
        Vector2 lookInput = inputReader.LookInput;

        // Accumulate rotation from input
        _yaw += lookInput.x * rotationSpeed * Time.deltaTime;
        _pitch -= lookInput.y * rotationSpeed * Time.deltaTime;

        // Clamp vertical angle to prevent flipping
        _pitch = Mathf.Clamp(_pitch, minVerticalAngle, maxVerticalAngle);
    }

    private void HandlePosition()
    {
        // Build a rotation from our yaw and pitch values
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        // Calculate camera position: start at player, step back by followDistance
        Vector3 targetPosition = target.position + Vector3.up * followHeight;
        Vector3 offset = rotation * Vector3.back * followDistance;

        transform.position = targetPosition + offset;
        transform.LookAt(targetPosition);
    }

    // Returns the camera's horizontal (yaw) rotation as a direction
    // PlayerController will use this for camera-relative movement
    public Quaternion GetCameraRotation()
    {
        return Quaternion.Euler(0f, _yaw, 0f);
    }
}