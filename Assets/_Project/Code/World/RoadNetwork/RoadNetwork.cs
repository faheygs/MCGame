using System.Collections.Generic;
using UnityEngine;
using MCGame.Core;

namespace MCGame.World
{
    /// <summary>
    /// Singleton managing all RoadNodes in the loaded scenes.
    /// Supports nearest-node lookup and A* pathfinding across the road graph.
    /// </summary>
    public class RoadNetwork : Singleton<RoadNetwork>
    {
        [Header("Auto-Discovery")]
        [Tooltip("If true, scans the scene for all RoadNodes on Start. Disable if registering manually.")]
        [SerializeField] private bool autoDiscoverOnStart = true;

        private readonly List<RoadNode> _nodes = new List<RoadNode>();

        public IReadOnlyList<RoadNode> Nodes => _nodes;

        private void Start()
        {
            if (autoDiscoverOnStart)
                DiscoverAllNodes();
        }

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

            return null;
        }

        public List<RoadNode> FindPath(Vector3 fromWorld, Vector3 toWorld)
        {
            RoadNode start = FindClosestNode(fromWorld);
            RoadNode end = FindClosestNode(toWorld);
            return FindPath(start, end);
        }

        private float Heuristic(RoadNode a, RoadNode b)
        {
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
}