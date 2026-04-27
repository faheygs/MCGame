using System;
using UnityEngine;
using MCGame.Core;

namespace MCGame.Gameplay.Player
{
    /// <summary>
    /// Owns the player's current state (OnFoot vs InVehicle) and handles transitions.
    /// Single entry point for all state changes — nothing else should directly enable
    /// or disable PlayerController / MotorcycleController.
    /// </summary>
    public class PlayerStateManager : Singleton<PlayerStateManager>
    {
        public enum PlayerState
        {
            OnFoot,
            InVehicle
        }

        [Header("Player Components")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private CharacterController characterController;

        [Header("Current State (Read-Only)")]
        [SerializeField] private PlayerState currentState = PlayerState.OnFoot;

        // Currently mounted vehicle (null when OnFoot).
        private MonoBehaviour _currentVehicle;
        private float _lastStateChangeTime = -999f;

        [Tooltip("Grace period (seconds) after a state change during which other interactables ignore inputs.")]
        [SerializeField] private float stateChangeCooldown = 0.2f;

        public bool JustChangedState => (Time.time - _lastStateChangeTime) < stateChangeCooldown;
        public PlayerState CurrentState => currentState;
        public bool IsInVehicle => currentState == PlayerState.InVehicle;
        public MonoBehaviour CurrentVehicle => _currentVehicle;

        public event Action<PlayerState> OnStateChanged;

        protected override void OnAwake()
        {
            if (playerController == null)
                playerController = GetComponent<PlayerController>();

            if (characterController == null)
                characterController = GetComponent<CharacterController>();
        }

        public void EnterVehicle(MonoBehaviour vehicle, Transform seatPosition)
        {
            Debug.Log($"[PSM] EnterVehicle called. frame={Time.frameCount}");

            if (currentState == PlayerState.InVehicle) return;
            if (vehicle == null || seatPosition == null) return;

            if (playerController != null) playerController.enabled = false;
            if (characterController != null) characterController.enabled = false;

            transform.SetParent(seatPosition, worldPositionStays: false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            _currentVehicle = vehicle;
            SetState(PlayerState.InVehicle);
        }

        public void ExitVehicle(Transform dismountPosition)
        {
            Debug.Log($"[PSM] ExitVehicle called. frame={Time.frameCount}");

            if (currentState == PlayerState.OnFoot) return;
            if (dismountPosition == null) return;

            transform.SetParent(null, worldPositionStays: false);

            transform.position = dismountPosition.position;
            transform.rotation = dismountPosition.rotation;

            // CharacterController must be re-enabled BEFORE PlayerController.
            if (characterController != null) characterController.enabled = true;
            if (playerController != null) playerController.enabled = true;

            _currentVehicle = null;
            SetState(PlayerState.OnFoot);
        }

        private void SetState(PlayerState newState)
        {
            if (currentState == newState) return;
            Debug.Log($"[PSM] State changing from {currentState} to {newState}, frame={Time.frameCount}");
            currentState = newState;
            _lastStateChangeTime = Time.time;
            OnStateChanged?.Invoke(currentState);
        }
    }
}