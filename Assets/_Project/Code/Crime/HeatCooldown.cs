using UnityEngine;
using MCGame.Gameplay.Player;

namespace MCGame.Gameplay.Crime
{
    /// <summary>
    /// Passively reduces heat over time when the player is not triggering heat events.
    /// Timer resets if heat increases while cooling down.
    ///
    /// Lives in Gameplay.Crime because heat is a crime-system concept.
    /// (Was previously in Core; moved during A5.3 since it depends on PlayerDataController.)
    /// </summary>
    public class HeatCooldown : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Seconds of clean play before heat decays by 1 level. Resets on any heat change.")]
        [SerializeField] private float cooldownTime = 15f;

        private float _timer;
        private bool _subscribed;

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void Start()
        {
            // Belt-and-suspenders in case OnEnable ran before PlayerDataController was ready.
            TrySubscribe();
        }

        private void OnDisable()
        {
            if (PlayerDataController.Instance != null)
                PlayerDataController.Instance.OnHeatChanged -= HandleHeatChanged;
            _subscribed = false;
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            if (PlayerDataController.Instance == null) return;

            PlayerDataController.Instance.OnHeatChanged += HandleHeatChanged;
            _subscribed = true;
        }

        private void Update()
        {
            if (PlayerDataController.Instance == null) return;
            if (PlayerDataController.Instance.HeatLevel <= 0)
            {
                _timer = 0f;
                return;
            }

            _timer += Time.deltaTime;

            if (_timer >= cooldownTime)
            {
                _timer = 0f;
                PlayerDataController.Instance.RemoveHeat(1);
            }
        }

        private void HandleHeatChanged(int newLevel)
        {
            // Reset timer any time heat changes — whether up or down.
            // Adding heat while cooling resets the countdown.
            _timer = 0f;
        }
    }
}