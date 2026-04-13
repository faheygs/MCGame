using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float gravity = -20f;

    [Header("References")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;

    private CharacterController _characterController;
    private Vector3 _verticalVelocity;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void LateUpdate()
    {
        HandleGravity();
        HandleMovement();
    }

    private void HandleMovement()
    {
        Vector2 input = inputReader.MoveInput;
        Vector3 moveDirection = new Vector3(input.x, 0f, input.y);

        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion cameraRotation = thirdPersonCamera.GetCameraRotation();
            moveDirection = cameraRotation * moveDirection;

            // Use run speed when sprinting, walk speed otherwise
            float speed = inputReader.SprintInput ? runSpeed : walkSpeed;

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            _characterController.Move(moveDirection * speed * Time.deltaTime);
        }
    }

    private void HandleGravity()
    {
        if (_characterController.isGrounded)
        {
            _verticalVelocity.y = -2f;
        }
        else
        {
            _verticalVelocity.y += gravity * Time.deltaTime;
        }

        _characterController.Move(_verticalVelocity * Time.deltaTime);
    }
}