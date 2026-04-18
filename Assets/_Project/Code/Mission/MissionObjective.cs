using UnityEngine;

// MissionObjective is spawned dynamically by MissionManager when a mission starts.
// It registers itself as a minimap marker on spawn and cleans up on destroy.

public class MissionObjective : MonoBehaviour
{
    private float _triggerRadius = 3f;
    private Transform _player;
    private int _minimapMarkerId = -1;

    private void Start()
    {
        _player = GameObject.FindWithTag("Player").transform;

        if (MinimapMarkerManager.Instance != null)
            _minimapMarkerId = MinimapMarkerManager.Instance.RegisterObjectiveMarker(
                () => transform.position
            );
    }

    public void SetRadius(float radius)
    {
        _triggerRadius = radius;
    }

    private void Update()
    {
        if (MissionManager.Instance == null) return;
        if (!MissionManager.Instance.IsMissionActive) return;
        if (_player == null) return;

        float distance = Vector3.Distance(transform.position, _player.position);

        if (distance <= _triggerRadius)
        {
            MissionManager.Instance.CompleteMission();
        }
    }

    private void OnDestroy()
    {
        if (_minimapMarkerId >= 0 && MinimapMarkerManager.Instance != null)
        {
            MinimapMarkerManager.Instance.UnregisterMissionMarker(_minimapMarkerId);
            _minimapMarkerId = -1;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _triggerRadius);
    }
}