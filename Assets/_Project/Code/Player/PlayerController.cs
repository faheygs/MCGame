using UnityEngine;
using MCGame.Input;
using MCGame.Gameplay.Camera;

namespace MCGame.Gameplay.Player
{
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

        private CharacterController _characterController;
        private ThirdPersonCamera _thirdPersonCamera;
        private Vector3 _verticalVelocity;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            // Find the camera automatically at runtime
            _thirdPersonCamera = UnityEngine.Camera.main.GetComponent<ThirdPersonCamera>();
        }

        private void Update()
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
                Quaternion cameraRotation = _thirdPersonCamera.GetCameraRotation();
                moveDirection = cameraRotation * moveDirection;

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
}