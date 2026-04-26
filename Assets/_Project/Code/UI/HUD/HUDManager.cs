using UnityEngine;
using MCGame.Core;
using MCGame.Gameplay.Mission;

namespace MCGame.Gameplay.UI
{
    // HUDManager is the single entry point for all HUD operations.
    // All game systems call HUDManager — never touch panel scripts directly.

    public class HUDManager : Singleton<HUDManager>
    {
        [Header("Panels")]
        [SerializeField] private HUDMissionPanel missionPanel;
        [SerializeField] private HUDIdentityPanel identityPanel;
        [SerializeField] private HUDVitalsPanel vitalsPanel;

        [Header("Notifications")]
        [SerializeField] private HUDNotificationSystem notificationSystem;

        [Header("Heat")]
        [SerializeField] private HUDHeatPanel heatPanel;

        [Header("Data")]
        [SerializeField] private PlayerStats playerStats;

        // -----------------------------------------------------------------
        // Lifecycle
        // -----------------------------------------------------------------

        private void OnEnable()
        {
            // GameManager may not exist on first OnEnable in some startup orders.
            // We try here, and Start() will catch it if not.
            TrySubscribeToGameManager();
        }

        private void Start()
        {
            // Belt-and-suspenders subscription in case OnEnable ran before GameManager Awake.
            TrySubscribeToGameManager();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= HandleGameStateChanged;
        }

        private bool _subscribed;

        private void TrySubscribeToGameManager()
        {
            if (_subscribed) return;
            if (GameManager.Instance == null) return;

            GameManager.Instance.OnStateChanged += HandleGameStateChanged;
            _subscribed = true;
        }

        private void HandleGameStateChanged(GameState previous, GameState current)
        {
            // Smoke test for A3.3 — verifies HUD receives state change events.
            // Future: pause UI overlay, death screen UI, etc. will react here.
            Debug.Log($"[HUDManager] Game state changed: {previous} → {current}");
        }

        // -----------------------------------------------------------------
        // Mission
        // -----------------------------------------------------------------

        public void OnMissionStarted(MissionData mission)
        {
            missionPanel.ShowMission(mission.missionName, mission.briefingText);
            ShowToast("NEW JOB: " + mission.missionName.ToUpper());
        }

        public void OnMissionObjectiveUpdated(string newObjective)
        {
            missionPanel.UpdateObjective(newObjective);
        }

        public void OnMissionCompleted(MissionData mission)
        {
            missionPanel.HidePanel();

            string message = $"JOB DONE — {mission.missionName.ToUpper()}";

            if (mission.moneyReward > 0 || mission.reputationReward > 0)
            {
                message += $"\n${mission.moneyReward:N0} EARNED";
                if (mission.reputationReward > 0)
                    message += $"  |  +{mission.reputationReward} REP";
            }

            notificationSystem.ShowSuccessNotification(message);
        }

        public void OnMissionFailed(MissionData mission)
        {
            missionPanel.HidePanel();
            ShowToast("JOB FAILED");
        }

        // --- Notifications ---

        public void ShowToast(string message)
        {
            notificationSystem.ShowNotification(message);
        }

        public void ShowFloatingNumber(string text, Vector2 position, Color color)
        {
            // Floating numbers not yet implemented — placeholder
        }

        // Heat
        public void SetHeatLevel(int level)
        {
            heatPanel.SetHeatLevel(level);
        }
    }
}