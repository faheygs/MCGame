using UnityEngine;

namespace MCGame.Gameplay.Mission
{
    [CreateAssetMenu(fileName = "MissionData", menuName = "MCGame/Missions/Mission Data")]
    public class MissionData : ScriptableObject
    {
        [Header("Mission Info")]
        public string missionName;
        [TextArea] public string briefingText;

        [Header("Giver")]
        [Tooltip("Name of the NPC giving this mission. Used for interact prompt: 'Talk to [name]'")]
        public string giverName = "Contact";

        [Header("State")]
        public MissionState initialState = MissionState.Locked;

        [Header("Objective")]
        public ObjectiveType objectiveType = ObjectiveType.GoToLocation;
        [Tooltip("Text shown on the interact prompt when objective is Interact type")]
        public string objectivePromptText = "Interact";
        public Vector3 objectivePosition;
        public float objectiveRadius = 3f;

        [Header("Combat (DefeatTarget only)")]
        [Tooltip("Enemy prefab to spawn for DefeatTarget objectives")]
        public GameObject enemyPrefab;
        [Tooltip("Number of enemies to spawn")]
        public int enemyCount = 1;

        [Header("Rewards")]
        public int moneyReward;
        public int reputationReward;

        [Header("Costs")]
        [Tooltip("Money deducted from player on completion (e.g. deposit missions)")]
        public int moneyCost;

        [Header("Unlocks")]
        [Tooltip("Missions to unlock when this mission is completed")]
        public MissionData[] missionsToUnlockOnComplete;
        [Tooltip("Seconds to wait before unlocked missions become Available (0 = immediate)")]
        public float unlockDelay;
    }
}