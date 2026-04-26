using UnityEngine;
using UnityEngine.InputSystem;
using MCGame.Core;
using MCGame.Gameplay.Player;

namespace MCGame.Gameplay
{
    /// <summary>
    /// Editor/debug-time hotkeys for quickly manipulating player state during testing.
    /// Lives in Gameplay assembly because it depends on PlayerDataController (gameplay-side).
    /// </summary>
    public class DebugTools : MonoBehaviour
    {
        private void Update()
        {
            if (PlayerDataController.Instance == null) return;

            // --- PlayerData debug keys ---

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
                PlayerDataController.Instance.AddHeat(1);

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
                PlayerDataController.Instance.RemoveHeat(1);

            if (Keyboard.current.digit3Key.wasPressedThisFrame)
                PlayerDataController.Instance.AddMoney(500);

            if (Keyboard.current.digit4Key.wasPressedThisFrame)
                PlayerDataController.Instance.AddReputation(50);

            if (Keyboard.current.digit5Key.wasPressedThisFrame)
                PlayerDataController.Instance.TakeDamage(10f);

            if (Keyboard.current.digit6Key.wasPressedThisFrame)
                PlayerDataController.Instance.Heal(10f);

            // --- GameManager debug keys ---

            if (Keyboard.current.gKey.wasPressedThisFrame)
            {
                if (GameManager.Instance != null)
                {
                    Debug.Log("[DebugTools] G pressed — triggering GameOver.");
                    GameManager.Instance.TriggerGameOver();
                }
            }

            if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                if (GameManager.Instance != null)
                {
                    Debug.Log("[DebugTools] H pressed — returning to Gameplay.");
                    GameManager.Instance.ReturnToGameplay();
                }
            }
        }
    }
}