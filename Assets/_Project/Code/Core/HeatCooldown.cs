using UnityEngine;

// HeatCooldown passively reduces heat over time when the player is not
// triggering heat events. Timer resets if heat increases while cooling down.

public class HeatCooldown : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float cooldownTime = 15f;

    [Header("Data")]
    [SerializeField] private PlayerStats playerStats;

    private float _timer;

    /// <summary>
    /// Public accessor for PlayerStats. Used by CrimeReporter.
    /// </summary>
    public PlayerStats GetPlayerStats()
    {
        return playerStats;
    }

    private void OnEnable()
    {
        playerStats.OnHeatChanged += HandleHeatChanged;
    }

    private void OnDisable()
    {
        playerStats.OnHeatChanged -= HandleHeatChanged;
    }

    private void Update()
    {
        if (playerStats.HeatLevel <= 0)
        {
            _timer = 0f;
            return;
        }

        _timer += Time.deltaTime;

        if (_timer >= cooldownTime)
        {
            _timer = 0f;
            playerStats.RemoveHeat(1);
        }
    }

    private void HandleHeatChanged(int newLevel)
    {
        // Reset timer any time heat changes — whether up or down
        // This means adding heat while cooling resets the countdown
        _timer = 0f;
    }
}