using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton managing all RoadNodes in the loaded scenes.
/// Supports nearest-node lookup and A* pathfinding across the road graph.
///
/// Used by: GPS routing, AI pathing, police pursuit, minimap road rendering.
/// </summary>
public class RoadNetwork : MonoBehaviour
{
    public static RoadNetwork Instance { get; private set; }

    [Header("Auto-Discovery")]
    [Tooltip("If true, scans the scene for all RoadNodes on Start. Disable if registering manually.")]
    [SerializeField] private bool autoDiscoverOnStart = true;

    private readonly List<RoadNode> _nodes = new List<RoadNode>();

    public IReadOnlyList<RoadNode> Nodes => _nodes;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[RoadNetwork] Duplicate instance on {name}. Destroying.");
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (autoDiscoverOnStart)
            DiscoverAllNodes();
    }

    /// <summary>
    /// Scan the scene for all RoadNode components and register them.
    /// </summary>
    public void DiscoverAllNodes()
    {
        _nodes.Clear();
        RoadNode[] found = FindObjectsByType<RoadNode>(FindObjectsInactive.Exclude);
        _nodes.AddRange(found);
    }

    public void RegisterNode(RoadNode node)
    {
        if (node == null) return;
        if (_nodes.Contains(node)) return;
        _nodes.Add(node);
    }

    public void UnregisterNode(RoadNode node)
    {
        if (node == null) return;
        _nodes.Remove(node);
    }

    // --- Nearest node queries ---

    /// <summary>
    /// Finds the closest RoadNode to the given world position.
    /// </summary>
    public RoadNode FindClosestNode(Vector3 worldPosition)
    {
        RoadNode closest = null;
        float closestDistSqr = float.MaxValue;

        for (int i = 0; i < _nodes.Count; i++)
        {
            RoadNode n = _nodes[i];
            if (n == null) continue;
            float distSqr = (n.Position - worldPosition).sqrMagnitude;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closest = n;
            }
        }

        return closest;
    }

    // --- A* pathfinding ---

    /// <summary>
    /// Finds the shortest path from start node to end node using A*.
    /// Returns a list of RoadNodes in order, or null if no path exists.
    /// </summary>
    public List<RoadNode> FindPath(RoadNode start, RoadNode end)
    {
        if (start == null || end == null) return null;
        if (start == end) return new List<RoadNode> { start };

        var openSet = new List<RoadNode> { start };
        var cameFrom = new Dictionary<RoadNode, RoadNode>();
        var gScore = new Dictionary<RoadNode, float> { { start, 0f } };
        var fScore = new Dictionary<RoadNode, float> { { start, Heuristic(start, end) } };

        while (openSet.Count > 0)
        {
            // Find node in openSet with lowest fScore
            RoadNode current = GetLowestFScore(openSet, fScore);

            if (current == end)
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);

            foreach (RoadNode neighbor in current.Connections)
            {
                if (neighbor == null) continue;

                float tentativeG = gScore[current] + current.DistanceTo(neighbor);

                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, end);

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        // No path found
        return null;
    }

    /// <summary>
    /// Convenience overload: find path from world position to world position
    /// using nearest road nodes as start and end.
    /// </summary>
    public List<RoadNode> FindPath(Vector3 fromWorld, Vector3 toWorld)
    {
        RoadNode start = FindClosestNode(fromWorld);
        RoadNode end = FindClosestNode(toWorld);
        return FindPath(start, end);
    }

    private float Heuristic(RoadNode a, RoadNode b)
    {
        // Straight-line distance heuristic (admissible for A*)
        return Vector3.Distance(a.Position, b.Position);
    }

    private RoadNode GetLowestFScore(List<RoadNode> openSet, Dictionary<RoadNode, float> fScore)
    {
        RoadNode best = openSet[0];
        float bestScore = fScore.ContainsKey(best) ? fScore[best] : float.MaxValue;

        for (int i = 1; i < openSet.Count; i++)
        {
            RoadNode candidate = openSet[i];
            float score = fScore.ContainsKey(candidate) ? fScore[candidate] : float.MaxValue;
            if (score < bestScore)
            {
                best = candidate;
                bestScore = score;
            }
        }

        return best;
    }

    private List<RoadNode> ReconstructPath(Dictionary<RoadNode, RoadNode> cameFrom, RoadNode current)
    {
        var path = new List<RoadNode> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }

        return path;
    }

    // --- Editor debug ---

    private void OnDrawGizmosSelected()
    {
        // Draw all edges in the network for visual debugging
        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        foreach (RoadNode node in _nodes)
        {
            if (node == null) continue;
            foreach (RoadNode connection in node.Connections)
            {
                if (connection == null) continue;
                Gizmos.DrawLine(node.Position, connection.Position);
            }
        }
    }
}