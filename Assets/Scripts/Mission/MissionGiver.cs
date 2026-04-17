using UnityEngine;
using System.Collections;

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
    private int _minimapMarkerId = -1;
    public int MinimapMarkerId => _minimapMarkerId;
    public MissionData MissionData => missionToGive;

    private IEnumerator Start()
    {
        _player = GameObject.FindWithTag("Player").transform;

        // Wait until MinimapMarkerManager is ready before registering
        yield return new WaitUntil(() => 
            MinimapMarkerManager.Instance != null && 
            MinimapMarkerManager.Instance.IsReady);

        _minimapMarkerId = MinimapMarkerManager.Instance.RegisterMissionMarker(
            () => transform.position
        );

        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (MissionManager.Instance == null) return;

        MissionState state = MissionManager.Instance.GetMissionState(missionToGive);
        bool visible = state == MissionState.Available;

        foreach (Transform child in transform)
            child.gameObject.SetActive(visible);

        if (_minimapMarkerId >= 0 && MinimapMarkerManager.Instance != null)
            MinimapMarkerManager.Instance.SetMissionMarkerVisible(_minimapMarkerId, visible);
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
        if (_isRegistered && UIManager.Instance != null)
        {
            UIManager.Instance.UnregisterNearbyGiver();
            _isRegistered = false;
        }

        if (_minimapMarkerId >= 0 && MinimapMarkerManager.Instance != null)
        {
            MinimapMarkerManager.Instance.UnregisterMissionMarker(_minimapMarkerId);
            _minimapMarkerId = -1;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}