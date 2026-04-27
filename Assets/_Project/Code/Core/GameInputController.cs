using UnityEngine;
using MCGame.Input;

namespace MCGame.Core
{
    /// <summary>
    /// Bridges GameManager game state to InputReader's enabled state.
    /// Sits in Core because it needs to know about both GameManager (Core) and InputReader (Input).
    ///
    /// Responsibility: gameplay input is enabled only during GameState.Gameplay.
    /// During Boot, Paused, GameOver — gameplay input is disabled.
    ///
    /// Why this exists as its own component instead of inside InputReader:
    /// InputReader is a pure input data source. Adding GameManager awareness to it would
    /// create a circular asmdef dependency (Input → Core → Input). This bridge keeps
    /// the layering clean.
    /// </summary>
    public class GameInputController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The InputReader ScriptableObject to control. Must match the one used by gameplay systems.")]
        [SerializeField] private InputReader inputReader;

        private bool _subscribed;

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void Start()
        {
            // Belt-and-suspenders in case OnEnable ran before GameManager Awake.
            TrySubscribe();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= HandleGameStateChanged;
            _subscribed = false;
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            if (GameManager.Instance == null) return;
            if (inputReader == null)
            {
                Debug.LogError("[GameInputController] InputReader not assigned. Gameplay input will not respond to game state.", this);
                return;
            }

            GameManager.Instance.OnStateChanged += HandleGameStateChanged;
            _subscribed = true;

            // Apply current state immediately so we don't miss the first transition.
            ApplyState(GameManager.Instance.CurrentState);
        }

        private void HandleGameStateChanged(GameState previous, GameState current)
        {
            ApplyState(current);
        }

        private void ApplyState(GameState state)
        {
            bool shouldBeEnabled = state == GameState.Gameplay;
            inputReader.SetInputEnabled(shouldBeEnabled);
        }
    }
}