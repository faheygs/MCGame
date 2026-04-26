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

        // --- Mission ---

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