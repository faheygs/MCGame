using UnityEngine;
using MCGame.Combat;
using MCGame.Gameplay.Mission;
using MCGame.Gameplay.UI;

namespace MCGame.Gameplay.Player
{
    /// <summary>
    /// Bridges the Health component on the player to PlayerDataController.
    /// When the player takes damage via Health.TakeDamage(), this updates
    /// PlayerData so the HUD health bar responds automatically.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlayerHealth : MonoBehaviour
    {
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
            if (PlayerDataController.Instance == null) return;

            float maxHealth = PlayerDataController.Instance.MaxHealth;
            float healthPercent = (float)_health.CurrentHP / _health.MaxHP;
            float targetHealth = healthPercent * maxHealth;
            float damage = PlayerDataController.Instance.Health - targetHealth;

            if (damage > 0)
                PlayerDataController.Instance.TakeDamage(damage);
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

            if (PlayerDataController.Instance != null)
                PlayerDataController.Instance.HealToFull();

            var controller = GetComponent<PlayerController>();
            if (controller != null) controller.enabled = true;
        }
    }
}