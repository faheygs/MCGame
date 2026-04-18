using UnityEngine;
using UnityEngine.InputSystem;

public class DebugTools : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private PlayerStats playerStats;

    private void Update()
    {
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
    }
}