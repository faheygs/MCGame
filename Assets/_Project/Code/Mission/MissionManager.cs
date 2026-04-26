using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MCGame.Core;
using MCGame.Gameplay.UI;

namespace MCGame.Gameplay.Mission
{
    /// <summary>
    /// Single source of truth for all mission state.
    /// Tracks active mission, spawns objectives, applies rewards/costs,
    /// handles unlock chains with optional delays.
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        [Header("Missions")]
        [SerializeField] private MissionData[] allMissions;

        [Header("Objective")]
        [SerializeField] private GameObject missionObjectivePrefab;

        [Header("Data")]
        [SerializeField] private PlayerStats playerStats;

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
            foreach (MissionData mission in allMissions)
                _missionStates[mission.missionName] = mission.initialState;
        }

        public void StartMission(MissionData mission)
        {
            if (IsMissionActive) return;
            if (GetMissionState(mission) != MissionState.Available) return;

            _currentMission = mission;
            _missionStates[mission.missionName] = MissionState.Active;

            SpawnObjective(mission);
            HUDManager.Instance?.OnMissionStarted(mission);
            WaypointManager.Instance.SetWaypoint(mission.objectivePosition);
        }

        public void CompleteMission()
        {
            if (!IsMissionActive) return;

            _missionStates[_currentMission.missionName] = MissionState.Completed;

            if (playerStats != null)
            {
                if (_currentMission.moneyReward > 0)
                    playerStats.AddMoney(_currentMission.moneyReward);

                if (_currentMission.reputationReward > 0)
                    playerStats.AddReputation(_currentMission.reputationReward);
            }

            if (playerStats != null && _currentMission.moneyCost > 0)
                playerStats.AddMoney(-_currentMission.moneyCost);

            if (_currentMission.missionsToUnlockOnComplete != null)
            {
                foreach (MissionData mission in _currentMission.missionsToUnlockOnComplete)
                {
                    if (_currentMission.unlockDelay > 0)
                        StartCoroutine(DelayedUnlock(mission, _currentMission.unlockDelay));
                    else
                        UnlockMission(mission);
                }
            }

            DespawnObjective();
            WaypointManager.Instance.ClearWaypoint();
            HUDManager.Instance?.OnMissionCompleted(_currentMission);

            _currentMission = null;
        }

        public void FailMission()
        {
            if (!IsMissionActive) return;

            _missionStates[_currentMission.missionName] = MissionState.Failed;

            DespawnObjective();
            WaypointManager.Instance.ClearWaypoint();
            HUDManager.Instance?.OnMissionFailed(_currentMission);

            _currentMission = null;
        }

        public void UnlockMission(MissionData mission)
        {
            if (mission == null) return;

            if (GetMissionState(mission) == MissionState.Locked)
            {
                _missionStates[mission.missionName] = MissionState.Available;
                HUDManager.Instance?.ShowToast("New job available");
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

        private IEnumerator DelayedUnlock(MissionData mission, float delay)
        {
            yield return new WaitForSeconds(delay);
            UnlockMission(mission);
        }

        private void SpawnObjective(MissionData mission)
        {
            if (missionObjectivePrefab == null) return;

            _activeObjective = Instantiate(
                missionObjectivePrefab,
                mission.objectivePosition,
                Quaternion.identity
            );

            MissionObjective obj = _activeObjective.GetComponent<MissionObjective>();
            if (obj != null)
                obj.Initialize(mission);
        }

        private void DespawnObjective()
        {
            if (_activeObjective != null)
            {
                Destroy(_activeObjective);
                _activeObjective = null;
            }
        }
    }
}