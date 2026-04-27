using UnityEngine;
using MCGame.Input;
using MCGame.Gameplay.Camera;

namespace MCGame.Gameplay.Player
{
    /// <summary>
    /// Player movement controller. Reads movement tuning from PlayerConfig (via
    /// PlayerDataController) — single source of truth shared with PlayerAnimationController.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputReader inputReader;

        private CharacterController _characterController;
        private ThirdPersonCamera _thirdPersonCamera;
        private Vector3 _verticalVelocity;

        // Movement values come from PlayerConfig (set in PlayerDataController).
        // Fallback defaults used only if config is unavailable (e.g., very early Awake).
        private float WalkSpeed => PlayerDataController.Instance?.Config?.walkSpeed ?? 3f;
        private float RunSpeed => PlayerDataController.Instance?.Config?.runSpeed ?? 6f;
        private float RotationSpeed => PlayerDataController.Instance?.Config?.rotationSpeed ?? 10f;
        private float Gravity => PlayerDataController.Instance?.Config?.gravity ?? -20f;

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

                float speed = inputReader.SprintInput ? RunSpeed : WalkSpeed;

                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    RotationSpeed * Time.deltaTime
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
                _verticalVelocity.y += Gravity * Time.deltaTime;
            }

            _characterController.Move(_verticalVelocity * Time.deltaTime);
        }
    }
}