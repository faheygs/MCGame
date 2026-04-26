using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MCGame.Core
{
    /// <summary>
    /// Single authority on top-level game state.
    /// Owns: current GameState, the OnStateChanged event, Time.timeScale during pause,
    /// and the pause input listener (which works even when gameplay input is disabled).
    ///
    /// Does NOT own: gameplay logic, mission state, player data, UI rendering,
    /// scene loading, save/load, or any system-specific behavior.
    /// Other systems subscribe to OnStateChanged and react accordingly.
    /// </summary>
    public class GameManager : PersistentSingleton<GameManager>
    {
        // -----------------------------------------------------------------
        // State
        // -----------------------------------------------------------------

        [Header("State (Read-Only at runtime)")]
        [SerializeField] private GameState currentState = GameState.Boot;

        public GameState CurrentState => currentState;

        public bool IsGameplay => currentState == GameState.Gameplay;
        public bool IsPaused => currentState == GameState.Paused;
        public bool IsGameOver => currentState == GameState.GameOver;

        /// <summary>
        /// Fires when the state changes. Passes (previous, current).
        /// Subscribers should react to the new state immediately.
        /// </summary>
        public event Action<GameState, GameState> OnStateChanged;

        // -----------------------------------------------------------------
        // Configuration
        // -----------------------------------------------------------------

        [Header("Pause Input")]
        [Tooltip("If true, pressing Escape (keyboard) or Start (gamepad) toggles pause during Gameplay.")]
        [SerializeField] private bool pauseInputEnabled = true;

        // -----------------------------------------------------------------
        // Lifecycle
        // -----------------------------------------------------------------

        protected override void OnAwake()
        {
            // Default startup state. Bootstrapper (A4) will move us to Gameplay.
            // For now, we transition to Gameplay immediately so existing systems
            // continue to work without depending on Bootstrapper yet.
            currentState = GameState.Boot;
            Debug.Log("[GameManager] Initialized in Boot state.");

            // Auto-transition to Gameplay. This is a temporary bridge until A4 (Bootstrapper)
            // owns the transition out of Boot.
            TransitionTo(GameState.Gameplay);
        }

        private void Update()
        {
            if (!pauseInputEnabled) return;
            HandlePauseInput();
        }

        // -----------------------------------------------------------------
        // Pause input — works regardless of InputReader state
        // -----------------------------------------------------------------

        /// <summary>
        /// Polls keyboard Escape and gamepad Start directly, bypassing InputReader.
        /// This is intentional — pause must work even when gameplay input is disabled
        /// (e.g., during a future cutscene or other input-suppressed state).
        /// </summary>
        private void HandlePauseInput()
        {
            bool pausePressed =
                (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);

            if (!pausePressed) return;

            if (currentState == GameState.Gameplay)
                PauseGame();
            else if (currentState == GameState.Paused)
                ResumeGame();
        }

        // -----------------------------------------------------------------
        // Public state-change API
        // -----------------------------------------------------------------

        /// <summary>
        /// Pause the game. Sets Time.timeScale to 0 and broadcasts the state change.
        /// Subscribers should disable gameplay input and show pause UI.
        /// </summary>
        public void PauseGame()
        {
            if (currentState != GameState.Gameplay)
            {
                Debug.LogWarning($"[GameManager] PauseGame() called from state {currentState}. Ignoring.");
                return;
            }

            TransitionTo(GameState.Paused);
        }

        /// <summary>
        /// Resume from pause. Restores Time.timeScale and broadcasts the state change.
        /// </summary>
        public void ResumeGame()
        {
            if (currentState != GameState.Paused)
            {
                Debug.LogWarning($"[GameManager] ResumeGame() called from state {currentState}. Ignoring.");
                return;
            }

            TransitionTo(GameState.Gameplay);
        }

        /// <summary>
        /// Trigger GameOver. Time keeps flowing, but gameplay input should suppress
        /// and a death screen UI (when built) takes over.
        /// </summary>
        public void TriggerGameOver()
        {
            if (currentState == GameState.GameOver)
            {
                Debug.LogWarning("[GameManager] TriggerGameOver() called while already in GameOver. Ignoring.");
                return;
            }

            TransitionTo(GameState.GameOver);
        }

        /// <summary>
        /// Used by RespawnService (A7) and similar systems to restore Gameplay
        /// after a GameOver-resolving event (respawn, scene reload, etc).
        /// </summary>
        public void ReturnToGameplay()
        {
            if (currentState == GameState.Gameplay)
            {
                Debug.LogWarning("[GameManager] ReturnToGameplay() called while already in Gameplay. Ignoring.");
                return;
            }

            TransitionTo(GameState.Gameplay);
        }

        // -----------------------------------------------------------------
        // Transition core
        // -----------------------------------------------------------------

        /// <summary>
        /// Single point of state mutation. All transitions go through here.
        /// Applies side effects (Time.timeScale), then broadcasts.
        /// </summary>
        private void TransitionTo(GameState newState)
        {
            if (newState == currentState) return;

            GameState previous = currentState;
            currentState = newState;

            ApplyStateSideEffects(newState);

            Debug.Log($"[GameManager] State: {previous} → {newState}");
            OnStateChanged?.Invoke(previous, newState);
        }

        /// <summary>
        /// Direct side effects of being in a particular state.
        /// Kept narrow — most reactions to state changes belong in subscribers,
        /// not here. This handles only what GameManager itself owns (Time.timeScale).
        /// </summary>
        private void ApplyStateSideEffects(GameState state)
        {
            switch (state)
            {
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;

                case GameState.Boot:
                case GameState.Gameplay:
                case GameState.GameOver:
                default:
                    Time.timeScale = 1f;
                    break;
            }
        }
    }
}