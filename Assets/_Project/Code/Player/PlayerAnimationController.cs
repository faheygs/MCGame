using UnityEngine;
using MCGame.Core;

namespace MCGame.Gameplay.Player
{
    /// <summary>
    /// Drives the Animator's Speed parameter from CharacterController velocity.
    /// Normalizes to runSpeed from PlayerConfig — single source of truth shared
    /// with PlayerController.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimationController : MonoBehaviour
    {
        [SerializeField] private CharacterController characterController;

        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void Update()
        {
            Vector3 horizontalVelocity = new Vector3(
                characterController.velocity.x,
                0f,
                characterController.velocity.z
            );

            // Normalize by runSpeed from config — same source PlayerController uses.
            // Fallback to 6 if config not yet available (very early Awake).
            float runSpeed = PlayerDataController.Instance?.Config?.runSpeed ?? 6f;
            float speed = horizontalVelocity.magnitude / runSpeed;

            _animator.SetFloat(AnimatorParams.Speed, speed, 0.15f, Time.deltaTime);
        }
    }
}