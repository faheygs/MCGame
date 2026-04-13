using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

// UIManager is the single controller for all HUD elements.
// Nothing else directly controls UI visibility.
// Other scripts report state to UIManager and it decides what to show.

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Mission UI")]
    [SerializeField] private TextMeshProUGUI missionNameText;
    [SerializeField] private TextMeshProUGUI missionStatusText;

    [Header("Interaction UI")]
    [SerializeField] private TextMeshProUGUI interactPromptText;

    private int _nearbyGiverCount = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        HideInteractPrompt();
        HideMissionUI();
    }

    // Called by MissionGiver when player enters range
    public void RegisterNearbyGiver()
    {
        _nearbyGiverCount++;
        UpdateInteractPrompt();
    }

    // Called by MissionGiver when player exits range
    public void UnregisterNearbyGiver()
    {
        _nearbyGiverCount = Mathf.Max(0, _nearbyGiverCount - 1);
        UpdateInteractPrompt();
    }

    // Called by MissionManager when a mission starts or ends
    public void OnMissionStateChanged(bool missionActive)
    {
        UpdateInteractPrompt();

        if (!missionActive)
            HideMissionUI();
    }

    public void ShowMissionUI(string name, string status)
    {
        if (missionNameText != null)
            missionNameText.text = name;

        if (missionStatusText != null)
            missionStatusText.text = status;
    }

    public void HideMissionUI()
    {
        if (missionNameText != null)
            missionNameText.text = "";

        if (missionStatusText != null)
            missionStatusText.text = "";
    }

    private void UpdateInteractPrompt()
    {
        bool show = _nearbyGiverCount > 0 &&
                    !MissionManager.Instance.IsMissionActive;

        if (interactPromptText == null) return;

        interactPromptText.gameObject.SetActive(show);

        if (show)
        {
            bool usingGamepad = Gamepad.current != null &&
                                Gamepad.current == InputSystem.GetDevice<Gamepad>();

            interactPromptText.text = usingGamepad
                ? "Press Y to interact"
                : "Press E to interact";
        }
    }

    private void HideInteractPrompt()
    {
        if (interactPromptText != null)
            interactPromptText.gameObject.SetActive(false);
    }
}