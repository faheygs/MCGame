using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using MCGame.Input;

namespace MCGame.Core
{
    /// <summary>
    /// Central interaction coordinator. Owns the interact prompt, owns the interact input subscription.
    ///
    /// Interactables register with this manager via Register() when they become relevant
    /// (usually when the player enters their proximity range), and Unregister() when they no longer are.
    ///
    /// Every LateUpdate, the manager selects a "current interactable" from the registered list,
    /// sorted by priority (highest first) then distance (nearest first). The current interactable's
    /// prompt is shown; on input, its OnInteract() is called.
    ///
    /// Only the current interactable responds to input. Guarantees one action per button press.
    /// </summary>
    public class InteractionManager : MonoBehaviour
    {
        public static InteractionManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private InputReader inputReader;
        [Tooltip("TMP text used for the interact prompt. Usually the same one UIManager uses.")]
        [SerializeField] private TextMeshProUGUI interactPromptText;

        private readonly List<IInteractable> _registered = new List<IInteractable>();
        private IInteractable _currentInteractable;
        private Transform _player;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[InteractionManager] Duplicate instance on {name}. Destroying this one.");
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            GameObject playerGO = GameObject.FindWithTag("Player");
            if (playerGO == null)
            {
                Debug.LogError("[InteractionManager] No GameObject tagged 'Player' in scene.");
                enabled = false;
                return;
            }
            _player = playerGO.transform;

            HidePrompt();
        }

        private void OnEnable()
        {
            if (inputReader != null)
            {
                // Defensive unsubscribe + subscribe — idempotent, prevents duplicate handlers.
                inputReader.InteractPressed -= HandleInteractPressed;
                inputReader.InteractPressed += HandleInteractPressed;
            }
        }

        private void OnDisable()
        {
            if (inputReader != null)
                inputReader.InteractPressed -= HandleInteractPressed;
        }

        // --- Public registration API ---

        /// <summary>
        /// Register an interactable. Safe to call multiple times for the same instance
        /// (duplicates are prevented).
        /// </summary>
        public void Register(IInteractable interactable)
        {
            if (interactable == null) return;
            if (_registered.Contains(interactable)) return;
            _registered.Add(interactable);
        }

        /// <summary>
        /// Unregister an interactable. Safe to call even if not registered.
        /// </summary>
        public void Unregister(IInteractable interactable)
        {
            if (interactable == null) return;
            _registered.Remove(interactable);

            // If this was the current interactable, immediately clear so the prompt hides.
            if (_currentInteractable == interactable)
            {
                _currentInteractable = null;
                HidePrompt();
            }
        }

        // --- Per-frame selection ---

        private void LateUpdate()
        {
            if (_player == null) return;

            IInteractable best = SelectBestInteractable();
            _currentInteractable = best;

            // Refresh every frame — prompt text can change based on the interactable's
            // internal state (e.g., MotorcycleInteraction returns "Mount" or "Dismount"
            // based on PlayerStateManager state). Checking identity isn't enough.
            RefreshPrompt();
        }

        /// <summary>
        /// Highest priority wins. Ties broken by nearest to player.
        /// Skips interactables whose CanInteract() returns false.
        /// </summary>
        private IInteractable SelectBestInteractable()
        {
            IInteractable best = null;
            int bestPriority = int.MinValue;
            float bestDistanceSqr = float.MaxValue;

            Vector3 playerPos = _player.position;

            for (int i = 0; i < _registered.Count; i++)
            {
                IInteractable candidate = _registered[i];
                if (candidate == null) continue;
                if (!candidate.CanInteract()) continue;

                int candidatePriority = candidate.Priority;
                float candidateDistanceSqr = (candidate.GetPosition() - playerPos).sqrMagnitude;

                // Priority first. Tie-broken by distance.
                if (candidatePriority > bestPriority ||
                    (candidatePriority == bestPriority && candidateDistanceSqr < bestDistanceSqr))
                {
                    best = candidate;
                    bestPriority = candidatePriority;
                    bestDistanceSqr = candidateDistanceSqr;
                }
            }

            return best;
        }

        // --- Prompt display ---

        private void RefreshPrompt()
        {
            if (_currentInteractable == null)
            {
                HidePrompt();
                return;
            }

            if (!_currentInteractable.ShouldShowPrompt())
            {
                HidePrompt();
                return;
            }

            ShowPrompt(_currentInteractable.GetPromptText());
        }

        private void ShowPrompt(string action)
        {
            if (interactPromptText == null) return;

            bool usingGamepad = Gamepad.current != null &&
                                Gamepad.current == InputSystem.GetDevice<Gamepad>();
            string button = usingGamepad ? "Y" : "E";

            interactPromptText.text = $"Press {button} to {action}";
            interactPromptText.gameObject.SetActive(true);
        }

        private void HidePrompt()
        {
            if (interactPromptText == null) return;
            interactPromptText.gameObject.SetActive(false);
        }

        // --- Input ---

        private void HandleInteractPressed()
        {
            if (_currentInteractable == null) return;
            if (!_currentInteractable.CanInteract()) return;

            _currentInteractable.OnInteract();
        }
    }
}