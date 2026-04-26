using UnityEngine;
using System;

namespace MCGame.Core
{
    /// <summary>
    /// Single source of truth for the active waypoint.
    /// Set by FullMapController (player-placed) or MissionManager (auto-set on mission start).
    /// Cleared by MissionManager on complete/fail, or by player removing the pin on the map.
    /// </summary>
    public class WaypointManager : MonoBehaviour
    {
        [SerializeField] private Transform playerTransform;
        [SerializeField] private float arrivalRadius = 5f;

        public static WaypointManager Instance { get; private set; }
        public Vector3 WaypointPosition { get; private set; }
        public bool HasWaypoint { get; private set; }

        public event Action<Vector3> OnWaypointSet;
        public event Action OnWaypointCleared;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (!HasWaypoint) return;
            if (playerTransform == null) return;

            float distance = Vector3.Distance(
                new Vector3(playerTransform.position.x, 0f, playerTransform.position.z),
                WaypointPosition
            );

            if (distance <= arrivalRadius)
                ClearWaypoint();
        }

        public void SetWaypoint(Vector3 worldPosition)
        {
            WaypointPosition = new Vector3(worldPosition.x, 0f, worldPosition.z);
            HasWaypoint = true;
            OnWaypointSet?.Invoke(WaypointPosition);
        }

        public void ClearWaypoint()
        {
            if (!HasWaypoint) return;
            HasWaypoint = false;
            OnWaypointCleared?.Invoke();
        }
    }
}