using UnityEngine;

// MissionObjective is spawned dynamically by MissionManager when a mission starts.
// It despawns automatically when the mission completes.

public class MissionObjective : MonoBehaviour
{
    private float _triggerRadius = 3f;
    private Transform _player;

    private void Start()
    {
        _player = GameObject.FindWithTag("Player").transform;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _triggerRadius);
    }
}