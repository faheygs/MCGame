using UnityEngine;

// MissionObjective is spawned dynamically by MissionManager when a mission starts.
// It registers itself as a minimap marker on spawn and cleans up on destroy.
//
// Supports two objective types:
// - GoToLocation: auto-completes when the player enters the radius (existing behavior)
// - Interact: player must press E on the objective to complete it (implements IInteractable)

public class MissionObjective : MonoBehaviour, IInteractable
{
    private float _triggerRadius = 3f;
    private ObjectiveType _type = ObjectiveType.GoToLocation;
    private string _promptText = "Interact";
    private Transform _player;
    private int _minimapMarkerId = -1;
    private bool _isRegistered;

    /// <summary>
    /// Called by MissionManager immediately after Instantiate.
    /// Configures the objective based on mission data.
    /// </summary>
    public void Initialize(MissionData mission)
    {
        _triggerRadius = mission.objectiveRadius;
        _type = mission.objectiveType;
        _promptText = mission.objectivePromptText;
    }

    // Legacy support — if SetRadius is called directly, treat as GoToLocation
    public void SetRadius(float radius)
    {
        _triggerRadius = radius;
    }

    private void Start()
    {
        _player = GameObject.FindWithTag("Player").transform;

        if (MinimapMarkerManager.Instance != null)
            _minimapMarkerId = MinimapMarkerManager.Instance.RegisterObjectiveMarker(
                () => transform.position
            );
    }

    private void Update()
    {
        if (MissionManager.Instance == null) return;
        if (!MissionManager.Instance.IsMissionActive) return;
        if (_player == null) return;

        float distance = Vector3.Distance(transform.position, _player.position);

        if (_type == ObjectiveType.GoToLocation)
        {
            // Auto-complete on proximity
            if (distance <= _triggerRadius)
            {
                MissionManager.Instance.CompleteMission();
            }
        }
        else if (_type == ObjectiveType.Interact)
        {
            // Register/unregister with InteractionManager based on proximity
            bool shouldRegister = distance <= _triggerRadius &&
                                  PlayerStateManager.Instance != null &&
                                  !PlayerStateManager.Instance.IsInVehicle;

            if (shouldRegister && !_isRegistered)
            {
                InteractionManager.Instance?.Register(this);
                _isRegistered = true;
            }
            else if (!shouldRegister && _isRegistered)
            {
                InteractionManager.Instance?.Unregister(this);
                _isRegistered = false;
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up minimap marker
        if (_minimapMarkerId >= 0 && MinimapMarkerManager.Instance != null)
        {
            MinimapMarkerManager.Instance.UnregisterMissionMarker(_minimapMarkerId);
            _minimapMarkerId = -1;
        }

        // Clean up interaction registration
        if (_isRegistered && InteractionManager.Instance != null)
        {
            InteractionManager.Instance.Unregister(this);
            _isRegistered = false;
        }
    }

    // --- IInteractable implementation (only used for Interact type) ---

    public int Priority => 15; // Higher than mission giver (5), higher than mount (10)

    public Vector3 GetPosition() => transform.position;

    public string GetPromptText() => _promptText;

    public bool ShouldShowPrompt() => true;

    public bool CanInteract()
    {
        if (_type != ObjectiveType.Interact) return false;
        if (MissionManager.Instance == null) return false;
        if (!MissionManager.Instance.IsMissionActive) return false;
        return true;
    }

    public void OnInteract()
    {
        if (!CanInteract()) return;
        MissionManager.Instance.CompleteMission();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _type == ObjectiveType.GoToLocation ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _triggerRadius);
    }
}