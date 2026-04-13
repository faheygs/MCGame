using System.Collections.Generic;
using UnityEngine;
using TMPro;

// MissionManager is the single source of truth for all mission state.
// Missions can be started from anywhere by calling StartMission().
// Story events unlock missions by calling UnlockMission().

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [Header("Missions")]
    [SerializeField] private MissionData[] allMissions;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI missionNameText;
    [SerializeField] private TextMeshProUGUI missionStatusText;

    [Header("Objective")]
    [SerializeField] private GameObject missionObjectivePrefab;

    // Runtime state tracking — keyed by mission name for fast lookup
    private Dictionary<string, MissionState> _missionStates = new();
    private MissionData _currentMission;
    private GameObject _activeObjective;

    public MissionData CurrentMission => _currentMission;
    public bool IsMissionActive => _currentMission != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeMissions();
    }

    private void InitializeMissions()
    {
        // Set each mission to its initial state defined in the asset
        foreach (MissionData mission in allMissions)
        {
            _missionStates[mission.missionName] = mission.initialState;
        }
    }

    // Called by anything — MissionGiver, cutscene, story event, trigger volume
    public void StartMission(MissionData mission)
{
    if (IsMissionActive)
    {
        Debug.Log("MissionManager: A mission is already active.");
        return;
    }

    if (GetMissionState(mission) != MissionState.Available)
    {
        Debug.Log($"MissionManager: {mission.missionName} is not available.");
        return;
    }

    _currentMission = mission;
    _missionStates[mission.missionName] = MissionState.Active;

    SpawnObjective(mission);

    if (UIManager.Instance != null)
    {
        UIManager.Instance.ShowMissionUI(mission.missionName, mission.briefingText);
        UIManager.Instance.OnMissionStateChanged(true);
    }

    Debug.Log($"Mission started: {mission.missionName}");
}

    public void CompleteMission()
{
    if (!IsMissionActive) return;

    Debug.Log($"Mission complete: {_currentMission.missionName}");
    Debug.Log($"Reward: ${_currentMission.moneyReward} | Rep: {_currentMission.reputationReward}");

    _missionStates[_currentMission.missionName] = MissionState.Completed;

    if (_currentMission.missionsToUnlockOnComplete != null)
    {
        foreach (MissionData mission in _currentMission.missionsToUnlockOnComplete)
        {
            UnlockMission(mission);
        }
    }

    DespawnObjective();
    _currentMission = null;

    if (UIManager.Instance != null)
        UIManager.Instance.OnMissionStateChanged(false);
}

   public void FailMission()
{
    if (!IsMissionActive) return;

    Debug.Log($"Mission failed: {_currentMission.missionName}");
    _missionStates[_currentMission.missionName] = MissionState.Failed;

    DespawnObjective();
    _currentMission = null;

    if (UIManager.Instance != null)
        UIManager.Instance.OnMissionStateChanged(false);
}

    // Called by anything to make a mission available
    // This is the hook that cutscenes and story events will call later
    public void UnlockMission(MissionData mission)
    {
        if (mission == null) return;

        MissionState current = GetMissionState(mission);

        if (current == MissionState.Locked)
        {
            _missionStates[mission.missionName] = MissionState.Available;
            Debug.Log($"Mission unlocked: {mission.missionName}");
        }
    }

    public MissionState GetMissionState(MissionData mission)
    {
        if (_missionStates.TryGetValue(mission.missionName, out MissionState state))
            return state;

        return MissionState.Locked;
    }

    public List<MissionData> GetAvailableMissions()
    {
        List<MissionData> available = new();

        foreach (MissionData mission in allMissions)
        {
            if (GetMissionState(mission) == MissionState.Available)
                available.Add(mission);
        }

        return available;
    }

    private void SpawnObjective(MissionData mission)
    {
        if (missionObjectivePrefab == null)
        {
            Debug.LogError("MissionManager: No objective prefab assigned.");
            return;
        }

        _activeObjective = Instantiate(
            missionObjectivePrefab,
            mission.objectivePosition,
            Quaternion.identity
        );

        MissionObjective obj = _activeObjective.GetComponent<MissionObjective>();
        if (obj != null)
            obj.SetRadius(mission.objectiveRadius);
    }

    private void DespawnObjective()
    {
        if (_activeObjective != null)
        {
            Destroy(_activeObjective);
            _activeObjective = null;
        }
    }

    private void UpdateUI()
{
    // UI is now handled by UIManager
}
}