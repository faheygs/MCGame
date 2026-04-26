using UnityEngine;
using UnityEngine.InputSystem;

namespace MCGame.Core
{
    public class DebugTools : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private PlayerStats playerStats;

        private void Update()
        {
            // --- PlayerStats debug keys ---

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
                playerStats.AddHeat(1);

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
                playerStats.RemoveHeat(1);

            if (Keyboard.current.digit3Key.wasPressedThisFrame)
                playerStats.AddMoney(500);

            if (Keyboard.current.digit4Key.wasPressedThisFrame)
                playerStats.AddReputation(50);

            if (Keyboard.current.digit5Key.wasPressedThisFrame)
                playerStats.TakeDamage(10f);

            if (Keyboard.current.digit6Key.wasPressedThisFrame)
                playerStats.Heal(10f);

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