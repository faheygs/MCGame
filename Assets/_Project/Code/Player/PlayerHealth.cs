using UnityEngine;
using MCGame.Combat;
using MCGame.Gameplay.Mission;
using MCGame.Gameplay.UI;

namespace MCGame.Gameplay.Player
{
    /// <summary>
    /// Bridges the Health component on the player to PlayerDataController
    /// and routes death events to the appropriate respawn pathway.
    ///
    /// Death routing:
    ///   - If killed by police: PoliceManager handles bust + respawn (we do nothing here)
    ///   - Otherwise: RespawnService handles a clean respawn
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
            // Universal on-death effects (apply regardless of cause):
            if (MissionManager.Instance != null && MissionManager.Instance.IsMissionActive)
                MissionManager.Instance.FailMission();

            HUDManager.Instance?.ShowToast("WASTED");

            // Disable the controller so the player can't move during the death state.
            // RespawnService re-enables it as part of respawn.
            var controller = GetComponent<PlayerController>();
            if (controller != null) controller.enabled = false;

            // Respawn routing:
            // If the killer was a PoliceNPC, PoliceManager owns the bust sequence
            // and will call respawn itself. We don't double-respawn.
            if (WasKilledByPolice())
                return;

            // Any other death cause (enemy, civilian, fall damage, etc.) — respawn now.
            RespawnService.RespawnPlayer();
        }

        /// <summary>
        /// True if the most recent damage source was a PoliceNPC.
        /// PoliceManager handles the bust + respawn sequence in that case.
        /// </summary>
        private bool WasKilledByPolice()
        {
            GameObject source = _health.LastDamageSource;
            if (source == null) return false;

            // Check for PoliceNPC component on the damage source
            return source.GetComponent<MCGame.Gameplay.Police.PoliceNPC>() != null;
        }

        /// <summary>
        /// Public API for explicit respawn (e.g., debug, save-load).
        /// Resets the underlying Health component too.
        /// </summary>
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