using UnityEngine;
using MCGame.Input;

namespace MCGame.Gameplay.Vehicle
{
    /// <summary>
    /// Handles all motorcycle movement. Throttle, steering, lean, wheel spin.
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
        [SerializeField] private float boostMultiplier = 1.4f;
        [SerializeField] private float idleDeceleration = 5f;

        [Header("Steering")]
        [SerializeField] private float steerSpeedLowSpeed = 120f;
        [SerializeField] private float steerSpeedHighSpeed = 40f;

        [Header("Lean (Visual Only)")]
        [SerializeField] private float maxLeanAngle = 35f;
        [SerializeField] private float leanSmoothing = 5f;

        [Header("Ground Detection")]
        [SerializeField] private LayerMask groundLayer = ~0;
        [SerializeField] private float groundCheckDistance = 1.0f;
        [SerializeField] private Vector3 groundCheckOrigin = new Vector3(0f, 0f, 0f);

        [Header("Wheel Spin (Visual)")]
        [SerializeField] private float wheelSpinRate = 360f;

        private Rigidbody _rb;
        [Header("Debug (Read-Only)")]
        [SerializeField] private bool _isGrounded;
        [SerializeField] private float _currentLean;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
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
            if (inputReader == null) return;

            ApplyVisualLean(inputReader.MoveInput.x);
            ApplyWheelSpin();
        }

        private void CheckGrounded()
        {
            float originOffset = 0.5f;
            Vector3 origin = transform.TransformPoint(groundCheckOrigin) + Vector3.up * originOffset;
            float castDistance = groundCheckDistance + originOffset;

            _isGrounded = Physics.Raycast(origin, Vector3.down, castDistance, groundLayer,
                QueryTriggerInteraction.Ignore);
        }

        private void ApplyThrottle(float throttleInput, bool boosting)
        {
            Vector3 velocity = _rb.linearVelocity;
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            float currentSpeed = Vector3.Dot(horizontal, transform.forward);

            if (!_isGrounded || Mathf.Approximately(throttleInput, 0f))
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, idleDeceleration * Time.fixedDeltaTime);
            }
            else if (throttleInput > 0f)
            {
                float targetMax = maxSpeed * (boosting ? boostMultiplier : 1f);
                currentSpeed = Mathf.MoveTowards(currentSpeed, targetMax, acceleration * Time.fixedDeltaTime);
            }
            else
            {
                if (currentSpeed > 0.1f)
                    currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeForce * Time.fixedDeltaTime);
                else
                    currentSpeed = Mathf.MoveTowards(currentSpeed, -reverseMaxSpeed, acceleration * Time.fixedDeltaTime);
            }

            Vector3 newHorizontal = transform.forward * currentSpeed;
            _rb.linearVelocity = new Vector3(newHorizontal.x, velocity.y, newHorizontal.z);
        }

        private void ApplySteering(float steerInput)
        {
            float forwardSpeed = Vector3.Dot(_rb.linearVelocity, transform.forward);
            float speedNorm = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / maxSpeed);
            float effectiveSteerSpeed = Mathf.Lerp(steerSpeedLowSpeed, steerSpeedHighSpeed, speedNorm);

            float directionSign = forwardSpeed < -0.1f ? -1f : 1f;

            float yawDelta = steerInput * effectiveSteerSpeed * directionSign * Time.fixedDeltaTime;
            if (Mathf.Abs(yawDelta) > 0.0001f)
            {
                Quaternion deltaRot = Quaternion.Euler(0f, yawDelta, 0f);
                _rb.MoveRotation(_rb.rotation * deltaRot);
            }
        }

        private void ApplyVisualLean(float steerInput)
        {
            if (bodyVisual == null) return;

            float targetLean = _isGrounded ? -steerInput * maxLeanAngle : 0f;
            _currentLean = Mathf.Lerp(_currentLean, targetLean, leanSmoothing * Time.deltaTime);

            bodyVisual.localRotation = Quaternion.Euler(0f, 0f, _currentLean);
        }

        private void ApplyWheelSpin()
        {
            float forwardSpeed = Vector3.Dot(_rb.linearVelocity, transform.forward);
            float spinThisFrame = forwardSpeed * wheelSpinRate * Time.deltaTime;

            if (frontWheel != null)
                frontWheel.Rotate(Vector3.up, -spinThisFrame, Space.Self);
            if (rearWheel != null)
                rearWheel.Rotate(Vector3.up, -spinThisFrame, Space.Self);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Application.isPlaying && _isGrounded ? Color.green : Color.red;
            float originOffset = 0.5f;
            Vector3 origin = transform.TransformPoint(groundCheckOrigin) + Vector3.up * originOffset;
            float castDistance = groundCheckDistance + originOffset;
            Gizmos.DrawLine(origin, origin + Vector3.down * castDistance);
        }
    }
}