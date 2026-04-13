using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

// MissionGiver is placed in the world and gives the player a mission
// when they walk up and press the interact button.
// The interact prompt automatically shows the correct button
// based on the last input device used.

public class MissionGiver : MonoBehaviour
{
    [Header("Mission")]
    [SerializeField] private MissionData missionToGive;

    [Header("Interaction Settings")]
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private InputReader inputReader;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI interactPromptText;

    private Transform _player;
    private bool _playerInRange;

    private void Start()
    {
        _player = GameObject.FindWithTag("Player").transform;

        if (interactPromptText != null)
            interactPromptText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (_player == null) return;

        float distance = Vector3.Distance(transform.position, _player.position);
        bool wasInRange = _playerInRange;
        _playerInRange = distance <= interactRange;

        // Show or hide prompt based on range
        if (_playerInRange != wasInRange)
        {
            bool showPrompt = _playerInRange && !MissionManager.Instance.IsMissionActive;

            if (interactPromptText != null)
            {
                interactPromptText.gameObject.SetActive(showPrompt);

                if (showPrompt)
                    UpdatePromptText();
            }
        }

        // Update prompt text if device changes while in range
        if (_playerInRange)
            UpdatePromptText();

        if (_playerInRange && inputReader.InteractInput)
        {
            TryGiveMission();
        }
    }

    private void UpdatePromptText()
    {
        if (interactPromptText == null) return;

        // Check the last used input device
        var lastDevice = InputSystem.GetDevice<Gamepad>();
        bool usingGamepad = lastDevice != null && 
                            InputSystem.devices.Count > 0 &&
                            Gamepad.current != null &&
                            Gamepad.current == InputSystem.GetDevice<Gamepad>();

        interactPromptText.text = usingGamepad ? "Press Y to interact" : "Press E to interact";
    }

    private void TryGiveMission()
    {
        if (MissionManager.Instance == null)
        {
            Debug.LogError("MissionGiver: No MissionManager found in scene.");
            return;
        }

        if (MissionManager.Instance.IsMissionActive)
        {
            Debug.Log("MissionGiver: Player already has an active mission.");
            return;
        }

        MissionManager.Instance.StartMission(missionToGive);

        if (interactPromptText != null)
            interactPromptText.gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}