using UnityEngine;
using MCGame.Core;
using MCGame.Combat;
using MCGame.Gameplay.Mission;
using MCGame.Gameplay.UI;

namespace MCGame.Gameplay.Player
{
    /// <summary>
    /// Bridges the Health component on the player to PlayerStats.
    /// When the player takes damage via Health.TakeDamage(), this updates
    /// PlayerStats so the HUD health bar responds automatically.
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

            float healthPercent = (float)_health.CurrentHP / _health.MaxHP;
            float targetHealth = healthPercent * playerStats.MaxHealth;
            float damage = playerStats.Health - targetHealth;

            if (damage > 0)
                playerStats.TakeDamage(damage);
        }

        private void HandleDied()
        {
            if (MissionManager.Instance != null && MissionManager.Instance.IsMissionActive)
                MissionManager.Instance.FailMission();

            if (PlayerStateManager.Instance != null)
            {
                var controller = GetComponent<PlayerController>();
                if (controller != null) controller.enabled = false;
            }

            HUDManager.Instance?.ShowToast("WASTED");
        }

        public void Respawn()
        {
            _health.Reset();

            if (playerStats != null)
                playerStats.Heal(playerStats.MaxHealth);

            var controller = GetComponent<PlayerController>();
            if (controller != null) controller.enabled = true;
        }
    }
}