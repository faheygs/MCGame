using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

// UIManager handles interaction prompts only.
// Mission UI is handled by HUDManager.
// Proximity registration is handled by MissionGiver.

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

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
    }

    public void RegisterNearbyGiver()
    {
        _nearbyGiverCount++;
        UpdateInteractPrompt();
    }

    public void UnregisterNearbyGiver()
    {
        _nearbyGiverCount = Mathf.Max(0, _nearbyGiverCount - 1);
        UpdateInteractPrompt();
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