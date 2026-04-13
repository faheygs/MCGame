using UnityEngine;

[CreateAssetMenu(fileName = "MissionData", menuName = "MCGame/Mission Data")]
public class MissionData : ScriptableObject
{
    [Header("Mission Info")]
    public string missionName;
    [TextArea] public string briefingText;

    [Header("State")]
    public MissionState initialState = MissionState.Locked;

    [Header("Objective")]
    public Vector3 objectivePosition;
    public float objectiveRadius = 3f;

    [Header("Giver Location")]
    public Vector3 giverPosition;

    [Header("Reward")]
    public int moneyReward;
    public int reputationReward;

    [Header("Unlocks")]
    [Tooltip("Missions to unlock when this mission is completed")]
    public MissionData[] missionsToUnlockOnComplete;
}