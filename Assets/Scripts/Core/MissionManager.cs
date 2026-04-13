using UnityEngine;
using TMPro;

// MissionManager tracks the current mission state.
// It is the single source of truth for what mission is active,
// whether it's complete, and handles rewards.

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI missionNameText;
    [SerializeField] private TextMeshProUGUI missionStatusText;

    public MissionData CurrentMission { get; private set; }
    public bool IsMissionActive { get; private set; }

    private void Awake()
    {
        // Singleton pattern — only one MissionManager can exist
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void StartMission(MissionData mission)
    {
        if (IsMissionActive)
        {
            Debug.Log("MissionManager: A mission is already active.");
            return;
        }

        CurrentMission = mission;
        IsMissionActive = true;

        UpdateUI();
        Debug.Log($"Mission started: {mission.missionName}");
    }

    public void CompleteMission()
    {
        if (!IsMissionActive) return;

        Debug.Log($"Mission complete: {CurrentMission.missionName}");
        Debug.Log($"Reward: ${CurrentMission.moneyReward} | Rep: {CurrentMission.reputationReward}");

        IsMissionActive = false;
        CurrentMission = null;

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (missionNameText == null || missionStatusText == null) return;

        if (IsMissionActive)
        {
            missionNameText.text = CurrentMission.missionName;
            missionStatusText.text = CurrentMission.briefingText;
        }
        else
        {
            missionNameText.text = "";
            missionStatusText.text = "";
        }
    }
}