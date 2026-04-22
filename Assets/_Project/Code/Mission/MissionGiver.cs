using UnityEngine;
using System.Collections;

// MissionGiver is an NPC in the world that can offer missions to the player.
//
// - The NPC (visual representation) is ALWAYS visible. NPCs don't vanish.
// - The minimap marker shows when this NPC has Available missions, hides otherwise.
// - When a mission becomes newly available, the minimap marker blinks 3 times.
// - Supports multiple missions — the NPC gives the first Available one in the list.
// - Prompt says "Talk to [giverName]".
//
// Implements IInteractable — registers with InteractionManager when the player
// is in range, on foot, and the NPC has an available mission.

public class MissionGiver : MonoBehaviour, IInteractable
{
    [Header("NPC Identity")]
    [SerializeField] private string giverName = "Contact";

    [Header("Missions")]
    [Tooltip("Missions this NPC can give, checked in order. First Available one is offered.")]
    [SerializeField] private MissionData[] missions;

    [Header("Interaction Settings")]
    [SerializeField] private float interactRange = 3f;
    [Tooltip("Higher priority wins over lower interactables when both are in range.")]
    [SerializeField] private int priority = 5;

    private Transform _player;
    private bool _isBlinking;
    private bool _isRegistered;
    private int _minimapMarkerId = -1;
    private MissionData _currentOffering; // cached each frame
    private bool _hadMissionLastFrame; // tracks transitions for blink

    public int MinimapMarkerId => _minimapMarkerId;
    public string GiverName => giverName;

    // For FullMapController compatibility — returns the first available mission's data
    public MissionData MissionData => _currentOffering;

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

        _currentOffering = GetFirstAvailableMission();
        _hadMissionLastFrame = _currentOffering != null;

        UpdateMarkerVisibility();
    }

    private void OnDisable()
    {
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

        // Cache the current available mission (or null if none)
        _currentOffering = GetFirstAvailableMission();
        bool hasMissionNow = _currentOffering != null;

        // Detect transition: no mission last frame -> mission available now = blink
        if (hasMissionNow && !_hadMissionLastFrame)
        {
            if (_minimapMarkerId >= 0 && MinimapMarkerManager.Instance != null)
            {
                _isBlinking = true;
                MinimapMarkerManager.Instance.BlinkMissionMarker(_minimapMarkerId, 3, 0.3f);
                StartCoroutine(ClearBlinkFlag(3 * 2 * 0.3f));
            }
        }

        _hadMissionLastFrame = hasMissionNow;

        // NPC visuals stay active always — we only toggle the minimap marker
        UpdateMarkerVisibility();
        UpdateRegistration();
    }

    // --- Mission selection ---

    /// <summary>
    /// Returns the first mission in the array that is Available, or null.
    /// </summary>
    private MissionData GetFirstAvailableMission()
    {
        if (missions == null) return null;

        foreach (MissionData mission in missions)
        {
            if (mission == null) continue;
            if (MissionManager.Instance.GetMissionState(mission) == MissionState.Available)
                return mission;
        }

        return null;
    }

    /// <summary>
    /// True if this NPC has at least one Available mission.
    /// </summary>
    public bool HasAvailableMission()
    {
        return _currentOffering != null;
    }

    // --- Minimap marker ---

    private void UpdateMarkerVisibility()
    {
        if (_minimapMarkerId < 0 || MinimapMarkerManager.Instance == null) return;
        if (_isBlinking) return;

        // Marker visible only when this NPC has work to give
        MinimapMarkerManager.Instance.SetMissionMarkerVisible(_minimapMarkerId, HasAvailableMission());
    }

    // --- Interaction registration ---

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

        // Must have an available mission
        if (!HasAvailableMission()) return false;

        // Player must be on foot
        if (PlayerStateManager.Instance != null && PlayerStateManager.Instance.IsInVehicle)
            return false;

        return true;
    }

    // --- IInteractable implementation ---

    public int Priority => priority;

    public Vector3 GetPosition() => transform.position;

    public string GetPromptText() => $"Talk to {giverName}";

    public bool ShouldShowPrompt() => true;

    public bool CanInteract()
    {
        if (MissionManager.Instance == null) return false;
        if (MissionManager.Instance.IsMissionActive) return false;
        if (!HasAvailableMission()) return false;
        return true;
    }

    public void OnInteract()
    {
        if (!CanInteract()) return;
        MissionManager.Instance.StartMission(_currentOffering);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }

    private IEnumerator ClearBlinkFlag(float duration)
    {
        yield return new WaitForSeconds(duration);
        _isBlinking = false;
    }
}