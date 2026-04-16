using UnityEngine;

// HUDManager is the single entry point for all HUD operations.
// All game systems call HUDManager — never touch panel scripts directly.

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private HUDMissionPanel missionPanel;
    [SerializeField] private HUDIdentityPanel identityPanel;
    [SerializeField] private HUDVitalsPanel vitalsPanel;
    [SerializeField] private HUDHeatPanel heatPanel;

    [Header("Notifications")]
    [SerializeField] private HUDNotificationSystem notificationSystem;

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
        ShowToast("JOB DONE");

        if (mission.moneyReward > 0)
            ShowFloatingNumber(
                "$" + mission.moneyReward.ToString("N0"),
                Vector2.zero,
                new Color(0.831f, 0.388f, 0.102f)
            );
    }

    public void OnMissionFailed(MissionData mission)
    {
        missionPanel.HidePanel();
        ShowToast("JOB FAILED");
    }

    // --- Notifications ---

    public void ShowToast(string message)
    {
        notificationSystem.ShowToast(message);
    }

    public void ShowFloatingNumber(string text, Vector2 position, Color color)
    {
        notificationSystem.ShowFloatingNumber(text, position, color);
    }

    // --- Heat ---

    public void SetHeatLevel(int level)
    {
        heatPanel.SetHeatLevel(level);
    }
}