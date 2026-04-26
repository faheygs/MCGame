using UnityEngine;
using System.Collections;
using MCGame.Core;
using MCGame.Gameplay.Player;
using MCGame.Gameplay.UI;

namespace MCGame.Gameplay.Mission
{
    /// <summary>
    /// MissionGiver is an NPC in the world that can offer missions to the player.
    /// </summary>
    public class MissionGiver : MonoBehaviour, IInteractable
    {
        [Header("NPC Identity")]
        [SerializeField] private string giverName = "Contact";

        [Header("Missions")]
        [Tooltip("Missions this NPC can give, checked in order. First Available one is offered.")]
        [SerializeField] private MissionData[] missions;

        [Header("Interaction Settings")]
        [SerializeField] private float interactRange = 3f;
        [Tooltip("Higher priority wins over lower interactables when both are in range.")]
        [SerializeField] private int priority = 5;

        private Transform _player;
        private bool _isBlinking;
        private bool _isRegistered;
        private int _minimapMarkerId = -1;
        private MissionData _currentOffering;
        private bool _hadMissionLastFrame;

        public int MinimapMarkerId => _minimapMarkerId;
        public string GiverName => giverName;

        public MissionData MissionData => _currentOffering;

        private IEnumerator Start()
        {
            _player = PlayerService.PlayerTransform;
            if (_player == null)
            {
                Debug.LogError("[MissionGiver] PlayerService has no registered player. Mission giver disabled.", this);
                yield break;
            }

            yield return new WaitUntil(() =>
                MinimapMarkerManager.Instance != null &&
                MinimapMarkerManager.Instance.IsReady);

            _minimapMarkerId = MinimapMarkerManager.Instance.RegisterMissionMarker(
                () => transform.position
            );

            _currentOffering = GetFirstAvailableMission();
            _hadMissionLastFrame = _currentOffering != null;

            UpdateMarkerVisibility();
        }

        private void OnDisable()
        {
            if (_isRegistered && InteractionManager.Instance != null)
            {
                InteractionManager.Instance.Unregister(this);
                _isRegistered = false;
            }

            if (_minimapMarkerId >= 0 && MinimapMarkerManager.Instance != null)
            {
                MinimapMarkerManager.Instance.UnregisterMissionMarker(_minimapMarkerId);
                _minimapMarkerId = -1;
            }
        }

        private void Update()
        {
            if (_player == null) return;
            if (MissionManager.Instance == null) return;

            _currentOffering = GetFirstAvailableMission();
            bool hasMissionNow = _currentOffering != null;

            if (hasMissionNow && !_hadMissionLastFrame)
            {
                if (_minimapMarkerId >= 0 && MinimapMarkerManager.Instance != null)
                {
                    _isBlinking = true;
                    MinimapMarkerManager.Instance.BlinkMissionMarker(_minimapMarkerId, 3, 0.3f);
                    StartCoroutine(ClearBlinkFlag(3 * 2 * 0.3f));
                }
            }

            _hadMissionLastFrame = hasMissionNow;

            UpdateMarkerVisibility();
            UpdateRegistration();
        }

        private MissionData GetFirstAvailableMission()
        {
            if (missions == null) return null;

            foreach (MissionData mission in missions)
            {
                if (mission == null) continue;
                if (MissionManager.Instance.GetMissionState(mission) == MissionState.Available)
                    return mission;
            }

            return null;
        }

        public bool HasAvailableMission()
        {
            return _currentOffering != null;
        }

        private void UpdateMarkerVisibility()
        {
            if (_minimapMarkerId < 0 || MinimapMarkerManager.Instance == null) return;
            if (_isBlinking) return;

            MinimapMarkerManager.Instance.SetMissionMarkerVisible(_minimapMarkerId, HasAvailableMission());
        }

        private void UpdateRegistration()
        {
            if (InteractionManager.Instance == null) return;

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
            float distance = Vector3.Distance(transform.position, _player.position);
            if (distance > interactRange) return false;

            if (!HasAvailableMission()) return false;

            if (PlayerStateManager.Instance != null && PlayerStateManager.Instance.IsInVehicle)
                return false;

            if (PlayerDataController.Instance != null && PlayerDataController.Instance.IsLayingLow)
                return false;

            return true;
        }

        // --- IInteractable implementation ---

        public int Priority => priority;

        public Vector3 GetPosition() => transform.position;

        public string GetPromptText() => $"Talk to {giverName}";

        public bool ShouldShowPrompt() => true;

        public bool CanInteract()
        {
            if (MissionManager.Instance == null) return false;
            if (MissionManager.Instance.IsMissionActive) return false;
            if (!HasAvailableMission()) return false;
            return true;
        }

        public void OnInteract()
        {
            if (!CanInteract()) return;
            MissionManager.Instance.StartMission(_currentOffering);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }

        private IEnumerator ClearBlinkFlag(float duration)
        {
            yield return new WaitForSeconds(duration);
            _isBlinking = false;
        }
    }
}