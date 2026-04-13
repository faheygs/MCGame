using UnityEngine;

// MissionData defines a single mission.
// Create missions in the editor via Assets > Create > MCGame > Mission Data
// Each mission is a ScriptableObject asset — no code needed per mission.

[CreateAssetMenu(fileName = "MissionData", menuName = "MCGame/Mission Data")]
public class MissionData : ScriptableObject
{
    [Header("Mission Info")]
    public string missionName;
    [TextArea] public string briefingText;

    [Header("Reward")]
    public int moneyReward;
    public int reputationReward;
}