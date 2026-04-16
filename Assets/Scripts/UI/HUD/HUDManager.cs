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

    [Header("Notifications")]
    [SerializeField] private HUDNotificationSystem notificationSystem;

    [Header("Heat")]
    [SerializeField] private HUDHeatPanel heatPanel;

    [Header("Data")]
    [SerializeField] private PlayerStats playerStats;

    private int _previousHeatLevel;
    private bool _maxHeatNotified;

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
        notificationSystem.ShowNotification(message);
    }

    public void ShowFloatingNumber(string text, Vector2 position, Color color)
    {
        // Floating numbers not yet implemented — placeholder
    }

    //Heat
    public void SetHeatLevel(int level)
    {
        heatPanel.SetHeatLevel(level);
    }

    // --- Heat ---
    private void OnEnable()
    {
        playerStats.OnHeatChanged += HandleHeatChanged;
    }

    private void OnDisable()
    {
        playerStats.OnHeatChanged -= HandleHeatChanged;
    }

    private void HandleHeatChanged(int newLevel)
    {
        if (newLevel > _previousHeatLevel)
        {
            if (newLevel == playerStats.MaxHeatLevel && !_maxHeatNotified)
            {
                notificationSystem.ShowWarningNotification("MAXIMUM HEAT — LAY LOW");
                _maxHeatNotified = true;
            }
            else if (newLevel < playerStats.MaxHeatLevel)
            {
                notificationSystem.ShowNotification("HEAT INCREASED");
            }
        }
        else if (newLevel < _previousHeatLevel)
        {
            _maxHeatNotified = false;
            notificationSystem.ShowNotification("HEAT DECREASED");
        }

        _previousHeatLevel = newLevel;
    }
}