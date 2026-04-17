using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

// UIManager handles interaction prompts only.
// Mission UI is handled by HUDManager.
// Tracks two categories of nearby interactables: missions and vehicles.
// Vehicle prompt takes priority when both are registered simultaneously.

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Interaction UI")]
    [SerializeField] private TextMeshProUGUI interactPromptText;

    // Separate counters per interactable category so prompts can show context-appropriate text.
    private int _nearbyMissionCount = 0;
    private int _nearbyVehicleCount = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        HideInteractPrompt();
    }

    // --- Mission interactables ---

    public void RegisterMissionInteractable()
    {
        _nearbyMissionCount++;
        UpdateInteractPrompt();
    }

    public void UnregisterMissionInteractable()
    {
        _nearbyMissionCount = Mathf.Max(0, _nearbyMissionCount - 1);
        UpdateInteractPrompt();
    }

    // --- Vehicle interactables ---

    public void RegisterVehicleInteractable()
    {
        _nearbyVehicleCount++;
        UpdateInteractPrompt();
    }

    public void UnregisterVehicleInteractable()
    {
        _nearbyVehicleCount = Mathf.Max(0, _nearbyVehicleCount - 1);
        UpdateInteractPrompt();
    }

    // --- Prompt resolution ---

    private void UpdateInteractPrompt()
    {
        if (interactPromptText == null) return;

        bool vehicleAvailable = _nearbyVehicleCount > 0;
        bool missionAvailable = _nearbyMissionCount > 0 &&
                                MissionManager.Instance != null &&
                                !MissionManager.Instance.IsMissionActive;

        // Vehicle prompt takes priority if both are registered.
        if (vehicleAvailable)
        {
            interactPromptText.text = GetPromptText("Mount");
            interactPromptText.gameObject.SetActive(true);
        }
        else if (missionAvailable)
        {
            interactPromptText.text = GetPromptText("Interact");
            interactPromptText.gameObject.SetActive(true);
        }
        else
        {
            interactPromptText.gameObject.SetActive(false);
        }
    }

    private string GetPromptText(string action)
    {
        bool usingGamepad = Gamepad.current != null &&
                            Gamepad.current == InputSystem.GetDevice<Gamepad>();

        string button = usingGamepad ? "Y" : "E";
        return $"Press {button} to {action}";
    }

    // --- Manual prompt control (for special cases outside mission/vehicle system) ---

    public void ShowInteractPrompt(string text)
    {
        if (interactPromptText == null) return;
        interactPromptText.text = text;
        interactPromptText.gameObject.SetActive(true);
    }

    public void HideInteractPrompt()
    {
        if (interactPromptText == null) return;
        interactPromptText.gameObject.SetActive(false);
    }
}