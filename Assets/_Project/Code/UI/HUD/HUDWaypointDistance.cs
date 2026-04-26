using UnityEngine;
using TMPro;
using MCGame.Core;

/// <summary>
/// Displays distance to active waypoint near the minimap.
/// Shows "---" when no waypoint is active.
/// </summary>
public class HUDWaypointDistance : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private Transform playerTransform;

    private bool _hasWaypoint = false;
    private Vector3 _waypointPos;

    private void OnEnable()
    {
        if (WaypointManager.Instance == null) return;
        WaypointManager.Instance.OnWaypointSet += OnWaypointSet;
        WaypointManager.Instance.OnWaypointCleared += OnWaypointCleared;
    }

    private void OnDisable()
    {
        if (WaypointManager.Instance == null) return;
        WaypointManager.Instance.OnWaypointSet -= OnWaypointSet;
        WaypointManager.Instance.OnWaypointCleared -= OnWaypointCleared;
    }

    private void Start()
    {
        if (WaypointManager.Instance != null)
        {
            WaypointManager.Instance.OnWaypointSet += OnWaypointSet;
            WaypointManager.Instance.OnWaypointCleared += OnWaypointCleared;
        }

        SetNoWaypoint();
    }

    private void Update()
    {
        if (!_hasWaypoint) return;
        if (playerTransform == null) return;

        float distance = Vector3.Distance(
            new Vector3(playerTransform.position.x, 0f, playerTransform.position.z),
            new Vector3(_waypointPos.x, 0f, _waypointPos.z)
        );

        // Show meters under 1km, kilometers over
        if (distance < 1000f)
            distanceText.text = $"{Mathf.RoundToInt(distance)}m";
        else
            distanceText.text = $"{(distance / 1000f):F1}km";
    }

    private void OnWaypointSet(Vector3 worldPos)
    {
        _waypointPos = worldPos;
        _hasWaypoint = true;
    }

    private void OnWaypointCleared()
    {
        SetNoWaypoint();
    }

    private void SetNoWaypoint()
    {
        _hasWaypoint = false;
        distanceText.text = "---";
    }
}