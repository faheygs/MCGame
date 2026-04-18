using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A single node in the road graph. Represents an intersection, turn, or waypoint along a road.
///
/// Nodes connect to other nodes bidirectionally via the connections list.
/// Used by: GPS routing, AI traffic, police pursuit, minimap road rendering.
/// </summary>
public class RoadNode : MonoBehaviour
{
    public enum RoadType
    {
        MajorRoad,
        SideStreet,
        Alley,
        Highway,
        Intersection
    }

    [Header("Connections")]
    [Tooltip("Nodes this node is connected to. Bidirectional — RoadNetwork syncs both ends automatically.")]
    [SerializeField] private List<RoadNode> connections = new List<RoadNode>();

    [Header("Road Metadata")]
    [SerializeField] private RoadType roadType = RoadType.SideStreet;
    [Tooltip("Speed limit in m/s (for future AI drivers). 13.4 = 30 mph, 22.3 = 50 mph.")]
    [SerializeField] private float speedLimit = 13.4f;

    public IReadOnlyList<RoadNode> Connections => connections;
    public RoadType Type => roadType;
    public float SpeedLimit => speedLimit;
    public Vector3 Position => transform.position;

    /// <summary>
    /// Adds a bidirectional connection between this node and another.
    /// Safe to call multiple times — duplicates are ignored.
    /// </summary>
    public void AddConnection(RoadNode other)
    {
        if (other == null || other == this) return;
        if (!connections.Contains(other)) connections.Add(other);
        if (!other.connections.Contains(this)) other.connections.Add(this);
    }

    /// <summary>
    /// Removes a bidirectional connection.
    /// </summary>
    public void RemoveConnection(RoadNode other)
    {
        if (other == null) return;
        connections.Remove(other);
        other.connections.Remove(this);
    }

    /// <summary>
    /// Distance between this node and another.
    /// </summary>
    public float DistanceTo(RoadNode other)
    {
        if (other == null) return float.MaxValue;
        return Vector3.Distance(transform.position, other.transform.position);
    }

    // --- Editor visualization ---

    private void OnDrawGizmos()
    {
        // Draw node sphere colored by road type
        Gizmos.color = GetGizmoColor();
        Gizmos.DrawSphere(transform.position, 0.3f);

        // Draw lines to connected nodes
        Gizmos.color = new Color(1f, 1f, 1f, 0.6f);
        foreach (RoadNode connection in connections)
        {
            if (connection == null) continue;
            Gizmos.DrawLine(transform.position, connection.transform.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Selected node: draw bigger sphere and thicker highlights
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // Highlight connections in yellow when selected
        Gizmos.color = Color.yellow;
        foreach (RoadNode connection in connections)
        {
            if (connection == null) continue;
            Gizmos.DrawLine(transform.position, connection.transform.position);
        }
    }

    private Color GetGizmoColor()
    {
        switch (roadType)
        {
            case RoadType.MajorRoad:    return Color.white;
            case RoadType.SideStreet:   return new Color(0.7f, 0.7f, 0.7f);
            case RoadType.Alley:        return new Color(0.5f, 0.5f, 0.5f);
            case RoadType.Highway:      return Color.yellow;
            case RoadType.Intersection: return Color.cyan;
            default:                    return Color.white;
        }
    }
}