using UnityEngine;

// HUDManager is the single entry point for all HUD operations.
// It owns all HUD panels and coordinates between them.
// Other systems call HUDManager — nothing talks to panels directly.

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private HUDStatsPanel statsPanel;
    [SerializeField] private HUDHealthPanel healthPanel;
    [SerializeField] private HUDMissionPanel missionPanel;
    [SerializeField] private HUDNotificationSystem notificationSystem;

    [Header("References")]
    [SerializeField] private PlayerStats playerStats;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // --- Mission ---

    public void OnMissionStarted(MissionData mission)
    {
        missionPanel?.ShowMission(
            mission.missionName,
            mission.briefingText,
            mission.objectivePosition
        );

        notificationSystem?.ShowToast($"NEW JOB: {mission.missionName.ToUpper()}");
    }

    public void OnMissionCompleted(MissionData mission)
    {
        missionPanel?.HideMission();

        notificationSystem?.ShowToast($"JOB DONE: {mission.missionName.ToUpper()}");

        // Show floating reward numbers
        notificationSystem?.ShowFloatingNumber(
            $"+${mission.moneyReward:N0}",
            new Vector2(-300, -200),
            new Color(0.83f, 0.39f, 0.10f)
        );

        notificationSystem?.ShowFloatingNumber(
            $"+{mission.reputationReward} REP",
            new Vector2(-300, -160),
            new Color(0.83f, 0.39f, 0.10f)
        );

        // Apply rewards to PlayerStats
        playerStats?.AddMoney(mission.moneyReward);
        playerStats?.AddReputation(mission.reputationReward);
    }

    public void OnMissionFailed(MissionData mission)
    {
        missionPanel?.HideMission();
        notificationSystem?.ShowToast($"JOB FAILED: {mission.missionName.ToUpper()}");
    }

    // --- Interact Prompt ---

    public void ShowInteractPrompt(string text)
    {
        UIManager.Instance?.ShowInteractPrompt(text);
    }

    public void HideInteractPrompt()
    {
        UIManager.Instance?.HideInteractPrompt();
    }
}