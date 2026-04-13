using UnityEngine;

// MissionObjective is placed in the world at the target location.
// When the player enters the trigger zone, the mission completes.

public class MissionObjective : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float triggerRadius = 3f;

    private Transform _player;
    private bool _isActive;

    private void Start()
    {
        _player = GameObject.FindWithTag("Player").transform;
    }

    private void Update()
    {
        // Only check when a mission is active
        if (MissionManager.Instance == null) return;
        if (!MissionManager.Instance.IsMissionActive) return;

        float distance = Vector3.Distance(transform.position, _player.position);

        if (distance <= triggerRadius)
        {
            MissionManager.Instance.CompleteMission();
        }
    }

    // Draw trigger zone in Scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}