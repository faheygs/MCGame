using UnityEngine;

namespace MCGame.Police
{
    /// <summary>
    /// Simple civilian NPC that walks back and forth between two points.
    /// Acts as a witness for crime detection. No reactions in V1 - just walks.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CivilianNPC : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The walk path this civilian follows")]
        public CivilianWalkPath walkPath;

        [Header("Movement Settings")]
        [Tooltip("Walk speed in meters per second")]
        [Range(0.5f, 3f)]
        public float walkSpeed = 1.2f;

        [Tooltip("How close to target before considering it 'reached'")]
        [Range(0.1f, 1f)]
        public float arrivalThreshold = 0.2f;

        // Internal state
        private CharacterController _controller;
        private Animator _animator;
        private Transform _currentTarget;
        private bool _isPaused;
        private float _pauseTimer;
        private float _currentMovementSpeed; // Track our actual movement speed

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _animator = GetComponentInChildren<Animator>();
        }

        private void Start()
        {
            // Validate setup
            if (walkPath == null)
            {
                Debug.LogError($"[CivilianNPC] '{gameObject.name}' has no walkPath assigned. NPC will not move.", this);
                enabled = false;
                return;
            }

            if (walkPath.pointA == null || walkPath.pointB == null)
            {
                Debug.LogError($"[CivilianNPC] '{gameObject.name}' walkPath is missing endpoints. NPC will not move.", this);
                enabled = false;
                return;
            }

            // Start walking toward point B (assumes NPC spawns at/near point A)
            _currentTarget = walkPath.pointB;
        }

        private void Update()
        {
            if (_isPaused)
            {
                HandlePause();
                _currentMovementSpeed = 0f;
            }
            else
            {
                WalkTowardTarget();
                _currentMovementSpeed = walkSpeed;
            }

            // Update animator every frame
            UpdateAnimator();
        }

        private void WalkTowardTarget()
        {
            if (_currentTarget == null) return;

            // Get direction to target (ignoring Y to keep movement horizontal)
            Vector3 currentPos = transform.position;
            Vector3 targetPos = _currentTarget.position;
            Vector3 direction = (targetPos - currentPos);
            direction.y = 0; // Keep movement on ground plane
            float distanceToTarget = direction.magnitude;

            // Check if we've arrived
            if (distanceToTarget <= arrivalThreshold)
            {
                OnArrival();
                return;
            }

            // Move toward target
            Vector3 moveVector = direction.normalized * walkSpeed;
            _controller.Move(moveVector * Time.deltaTime);

            // Apply gravity (simple constant for civilians)
            _controller.Move(Vector3.down * 9.81f * Time.deltaTime);

            // Face movement direction
            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized);
            }
        }

        private void OnArrival()
        {
            // Start pause at this endpoint
            _isPaused = true;
            _pauseTimer = walkPath.pauseDuration;
        }

        private void HandlePause()
        {
            _pauseTimer -= Time.deltaTime;

            if (_pauseTimer <= 0f)
            {
                // Pause finished - switch target and resume walking
                _isPaused = false;
                _currentTarget = (_currentTarget == walkPath.pointA) ? walkPath.pointB : walkPath.pointA;
            }
        }

        private void UpdateAnimator()
        {
            if (_animator == null) return;

            // Set animator Speed parameter based on our tracked movement speed
            _animator.SetFloat("Speed", _currentMovementSpeed);
        }

        private void OnDrawGizmosSelected()
        {
            // Show current target when selected
            if (_currentTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, _currentTarget.position);
            }
        }
    }
}