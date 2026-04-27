using UnityEngine;
using MCGame.Core;
using MCGame.Gameplay.Player;
using MCGame.Gameplay.Interaction; 

namespace MCGame.Gameplay.Vehicle
{
    /// <summary>
    /// Handles player proximity and mount/dismount coordination for a single motorcycle.
    /// Implements IInteractable.
    /// </summary>
    [RequireComponent(typeof(MotorcycleController))]
    public class MotorcycleInteraction : MonoBehaviour, IInteractable
    {
        [Header("References")]
        [SerializeField] private MotorcycleController motorcycleController;
        [Tooltip("Where the player is placed when mounting (parent transform).")]
        [SerializeField] private Transform seatPosition;
        [Tooltip("Where the player is placed when dismounting.")]
        [SerializeField] private Transform dismountPosition;

        [Header("Proximity")]
        [SerializeField] private float interactRange = 2f;

        [Header("Priority")]
        [SerializeField] private int mountPriority = 10;
        [SerializeField] private int dismountPriority = 100;

        private Transform _player;
        private bool _isRegistered;

        private void Awake()
        {
            if (motorcycleController == null)
                motorcycleController = GetComponent<MotorcycleController>();
        }

        private void Start()
        {
            _player = PlayerService.PlayerTransform;
            if (_player == null)
            {
                Debug.LogError("[MotorcycleInteraction] PlayerService has no registered player. Motorcycle disabled.", this);
                enabled = false;
                return;
            }

            if (motorcycleController != null)
                motorcycleController.enabled = false;
        }

        private void OnDisable()
        {
            if (_isRegistered && InteractionManager.Instance != null)
            {
                InteractionManager.Instance.Unregister(this);
                _isRegistered = false;
            }
        }

        private void Update()
        {
            if (_player == null) return;
            if (InteractionManager.Instance == null) return;

            UpdateRegistration();
        }

        private void UpdateRegistration()
        {
            bool shouldBeRegistered = ShouldBeRegistered();

            if (shouldBeRegistered && !_isRegistered)
            {
                InteractionManager.Instance.Register(this);
                _isRegistered = true;
            }
            else if (!shouldBeRegistered && _isRegistered)
            {
                InteractionManager.Instance.Unregister(this);
                _isRegistered = false;
            }
        }

        private bool ShouldBeRegistered()
        {
            if (IsPlayerMountedOnThisBike()) return true;

            if (PlayerStateManager.Instance != null && PlayerStateManager.Instance.IsInVehicle)
                return false;

            float distance = Vector3.Distance(transform.position, _player.position);
            return distance <= interactRange;
        }

        private bool IsPlayerMountedOnThisBike()
        {
            if (PlayerStateManager.Instance == null) return false;
            if (!PlayerStateManager.Instance.IsInVehicle) return false;
            return PlayerStateManager.Instance.CurrentVehicle == (MonoBehaviour)motorcycleController;
        }

        // --- IInteractable implementation ---

        public int Priority =>
            IsPlayerMountedOnThisBike() ? dismountPriority : mountPriority;

        public Vector3 GetPosition() => transform.position;

        public string GetPromptText() => "Mount";

        public bool ShouldShowPrompt() => !IsPlayerMountedOnThisBike();

        public bool CanInteract()
        {
            if (PlayerStateManager.Instance == null) return false;

            if (IsPlayerMountedOnThisBike()) return true;

            if (PlayerStateManager.Instance.IsInVehicle) return false;
            if (seatPosition == null) return false;

            return true;
        }

        public void OnInteract()
        {
            if (!CanInteract()) return;

            if (IsPlayerMountedOnThisBike())
                TryDismount();
            else
                TryMount();
        }

        private void TryMount()
        {
            if (seatPosition == null)
            {
                Debug.LogError($"[MotorcycleInteraction] SeatPosition not assigned on {name}.");
                return;
            }

            PlayerStateManager.Instance.EnterVehicle(motorcycleController, seatPosition);

            motorcycleController.enabled = true;
        }

        private void TryDismount()
        {
            if (dismountPosition == null)
            {
                Debug.LogError($"[MotorcycleInteraction] DismountPosition not assigned on {name}.");
                return;
            }

            motorcycleController.enabled = false;

            PlayerStateManager.Instance.ExitVehicle(dismountPosition);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}