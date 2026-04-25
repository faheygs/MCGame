using UnityEngine;

/// <summary>
/// Marker for where things can spawn in the world. Attached to empty GameObjects
/// positioned where NPCs, vehicles, or props should appear.
///
/// Systems that spawn content (NPCSpawner, MissionController, etc.) query for
/// SpawnPoints of a specific type and use their position + rotation.
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    public enum SpawnType
    {
        NPCPedestrian,      // Random walking NPCs (future traffic/ambience)
        NPCTrafficDriver,   // NPCs who drive vehicles (future)
        Vehicle,            // Parked vehicles in the world
        MissionActor,       // NPCs spawned as part of a mission
        MissionTarget,      // Props/objects spawned as mission targets
        PlayerStart,        // Where the player starts in this area
        Police,             // Where the police spawn
        Generic             // Anything else
    }

    [Header("Spawn Type")]
    [SerializeField] private SpawnType type = SpawnType.Generic;

    [Header("Identifier (Optional)")]
    [Tooltip("Unique ID for this spawn point. Used by missions to target specific spawns.")]
    [SerializeField] private string spawnId = "";

    public SpawnType Type => type;
    public string SpawnId => spawnId;

    public Vector3 Position => transform.position;
    public Quaternion Rotation => transform.rotation;

    // --- Editor visualization ---

    private void OnDrawGizmos()
    {
        Gizmos.color = GetGizmoColor();
        Gizmos.DrawWireSphere(transform.position, 0.4f);

        // Draw forward direction arrow
        Vector3 forward = transform.forward * 0.8f;
        Gizmos.DrawLine(transform.position, transform.position + forward);
    }

    private Color GetGizmoColor()
    {
        switch (type)
        {
            case SpawnType.NPCPedestrian:    return Color.cyan;
            case SpawnType.NPCTrafficDriver: return new Color(0f, 0.7f, 1f);
            case SpawnType.Vehicle:          return Color.yellow;
            case SpawnType.MissionActor:     return Color.red;
            case SpawnType.MissionTarget:    return new Color(1f, 0.5f, 0f);
            case SpawnType.PlayerStart:      return Color.green;
            case SpawnType.Police:           return Color.blue;
            default:                         return Color.white;
        }
    }
}