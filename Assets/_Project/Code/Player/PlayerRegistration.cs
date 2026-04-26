using UnityEngine;
using MCGame.Combat;

namespace MCGame.Gameplay.Player
{
    /// <summary>
    /// Component that registers this Player GameObject with PlayerService.
    /// Lives on the Player GameObject alongside PlayerController.
    ///
    /// Why this is a separate component (not folded into PlayerController):
    ///   - PlayerController is gameplay logic (movement, input, etc.)
    ///   - PlayerRegistration is service-layer plumbing
    ///   - Keeping them separate makes either replaceable independently
    ///   - Tests can use PlayerController without auto-registering
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerRegistration : MonoBehaviour
    {
        private PlayerController _controller;
        private Health _health;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _health = GetComponent<Health>();

            if (_controller == null)
            {
                Debug.LogError("[PlayerRegistration] No PlayerController found on this GameObject. Cannot register.", this);
                return;
            }

            // Note: _health may legitimately be null on a non-combat player setup
            // (e.g., a future cinematic-only camera rig). Don't error — just register what we have.

            PlayerService.Register(_controller, _health);
        }

        private void OnDestroy()
        {
            if (_controller != null)
                PlayerService.Unregister(_controller);
        }
    }
}