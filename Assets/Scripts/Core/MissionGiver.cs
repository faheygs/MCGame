using UnityEngine;

// MissionGiver detects player proximity and reports to UIManager.
// It does not control any UI directly.

public class MissionGiver : MonoBehaviour
{
    [Header("Mission")]
    [SerializeField] private MissionData missionToGive;

    [Header("Interaction Settings")]
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private InputReader inputReader;

    private Transform _player;
    private bool _playerInRange;
    private bool _isRegistered;

    private void Start()
    {
        _player = GameObject.FindWithTag("Player").transform;
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (MissionManager.Instance == null) return;

        MissionState state = MissionManager.Instance.GetMissionState(missionToGive);

        // Only show when Available — hide when Active, Completed, Locked, Failed
        bool visible = state == MissionState.Available;

        foreach (Transform child in transform)
            child.gameObject.SetActive(visible);

        MinimapMarker marker = GetComponent<MinimapMarker>();
        if (marker != null)
            marker.SetVisible(visible);
    }

    private void Update()
    {
        if (_player == null) return;
        if (MissionManager.Instance == null) return;
        if (UIManager.Instance == null) return;

        UpdateVisibility();

        bool missionAvailable = MissionManager.Instance.GetMissionState(missionToGive)
                                == MissionState.Available;

        float distance = Vector3.Distance(transform.position, _player.position);
        bool inRange = distance <= interactRange;

        // Only register/unregister when range or availability changes
        bool shouldBeRegistered = inRange && missionAvailable;

        if (shouldBeRegistered && !_isRegistered)
        {
            UIManager.Instance.RegisterNearbyGiver();
            _isRegistered = true;
        }
        else if (!shouldBeRegistered && _isRegistered)
        {
            UIManager.Instance.UnregisterNearbyGiver();
            _isRegistered = false;
        }

        _playerInRange = inRange;

        if (_playerInRange && missionAvailable && inputReader.InteractInput)
        {
            TryGiveMission();
        }
    }

    private void TryGiveMission()
    {
        if (MissionManager.Instance.IsMissionActive) return;

        MissionManager.Instance.StartMission(missionToGive);
        UIManager.Instance.UnregisterNearbyGiver();
        _isRegistered = false;
    }

    private void OnDisable()
    {
        // Clean up registration if object is disabled
        if (_isRegistered && UIManager.Instance != null)
        {
            UIManager.Instance.UnregisterNearbyGiver();
            _isRegistered = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}