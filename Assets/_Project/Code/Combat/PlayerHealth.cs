using UnityEngine;

/// <summary>
/// Bridges the Health component on the player to PlayerStats.
/// When the player takes damage via Health.TakeDamage(), this updates
/// PlayerStats so the HUD health bar responds automatically.
///
/// Also handles player death: triggers mission failure and disables movement.
/// </summary>
[RequireComponent(typeof(Health))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private PlayerStats playerStats;

    private Health _health;

    private void Awake()
    {
        _health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        _health.OnDamaged += HandleDamaged;
        _health.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        _health.OnDamaged -= HandleDamaged;
        _health.OnDied -= HandleDied;
    }

    private void HandleDamaged(DamageInfo info)
    {
        if (playerStats == null) return;

        // Sync Health component's HP to PlayerStats so HUD updates
        float healthPercent = (float)_health.CurrentHP / _health.MaxHP;
        float targetHealth = healthPercent * playerStats.MaxHealth;
        float damage = playerStats.Health - targetHealth;

        if (damage > 0)
            playerStats.TakeDamage(damage);
    }

    private void HandleDied()
    {
        // Fail the active mission if there is one
        if (MissionManager.Instance != null && MissionManager.Instance.IsMissionActive)
            MissionManager.Instance.FailMission();

        // Disable player movement
        if (PlayerStateManager.Instance != null)
        {
            var controller = GetComponent<PlayerController>();
            if (controller != null) controller.enabled = false;
        }

        HUDManager.Instance?.ShowToast("WASTED");
    }

    /// <summary>
    /// Call this to respawn the player (future: after death screen).
    /// Resets health and re-enables movement.
    /// </summary>
    public void Respawn()
    {
        _health.Reset();

        if (playerStats != null)
            playerStats.Heal(playerStats.MaxHealth);

        var controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = true;
    }
}