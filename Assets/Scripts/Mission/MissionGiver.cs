using UnityEngine;
using System.Collections;

// MissionGiver represents a point in the world where the player can start a mission.
// Implements IInteractable — registers with InteractionManager when relevant
// (player in range, mission available, player on foot), unregisters otherwise.
// InteractionManager owns prompt display and input handling.

public class MissionGiver : MonoBehaviour, IInteractable
{
    [Header("Mission")]
    [SerializeField] private MissionData missionToGive;

    [Header("Interaction Settings")]
    [SerializeField] private float interactRange = 2f;
    [Tooltip("Higher priority wins over lower interactables when both are in range.")]
    [SerializeField] private int priority = 5;

    private Transform _player;
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

    private void OnDisable()
    {
        // Always ensure we unregister from the interaction manager on disable.
        if (_isRegistered && InteractionManager.Instance != null)
        {
            InteractionManager.Instance.Unregister(this);
            _isRegistered = false;
        }

        if (_minimapMarkerId >= 0 && MinimapMarkerManager.Instance != null)
        {
            MinimapMarkerManager.Instance.UnregisterMissionMarker(_minimapMarkerId);
            _minimapMarkerId = -1;
        }
    }

    private void Update()
    {
        if (_player == null) return;
        if (MissionManager.Instance == null) return;

        UpdateVisibility();
        UpdateRegistration();
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

    private void UpdateRegistration()
    {
        if (InteractionManager.Instance == null) return;

        bool shouldBeRegistered = ShouldBeRegistered();

        if (shouldBeRegistered && !_isRegistered)
        {
            InteractionManager.Instance.Register(this);
            _isRegistered = true;
        }
        else if (!shouldBeRegistered && _isRegistered)
        {
            InteractionManager.Instance.Unregister(this);
            _isRegistered = false;
        }
    }

    private bool ShouldBeRegistered()
    {
        // Must be in range
        float distance = Vector3.Distance(transform.position, _player.position);
        if (distance > interactRange) return false;

        // Mission must be Available (not Locked, not Active, not Completed)
        if (MissionManager.Instance.GetMissionState(missionToGive) != MissionState.Available)
            return false;

        // Player must be on foot (not mounted)
        if (PlayerStateManager.Instance != null && PlayerStateManager.Instance.IsInVehicle)
            return false;

        return true;
    }

    // --- IInteractable implementation ---

    public int Priority => priority;

    public Vector3 GetPosition() => transform.position;

    public string GetPromptText() => "Start Mission";

    public bool ShouldShowPrompt() => true;

    public bool CanInteract()
    {
        // Defensive final check — conditions may change between registration and input.
        if (MissionManager.Instance == null) return false;
        if (MissionManager.Instance.IsMissionActive) return false;
        if (MissionManager.Instance.GetMissionState(missionToGive) != MissionState.Available) return false;
        return true;
    }

    public void OnInteract()
    {
        if (!CanInteract()) return;
        MissionManager.Instance.StartMission(missionToGive);
        // InteractionManager's LateUpdate will detect the state change next frame
        // and unregister us naturally (mission is no longer Available).
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}