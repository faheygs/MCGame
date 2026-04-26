using System;
using UnityEngine;
using MCGame.Combat;

namespace MCGame.Gameplay.Player
{
    /// <summary>
    /// Static registry for the player and its core components.
    /// Replaces FindWithTag("Player") and FindAnyObjectByType&lt;PlayerController&gt;()
    /// with a fast, cached, event-broadcasting accessor.
    ///
    /// Lifecycle:
    ///   - PlayerRegistration (component on Player GameObject) calls Register() in Awake
    ///   - Other systems read PlayerService.Player / .Transform / .Health
    ///   - PlayerRegistration calls Unregister() in OnDestroy
    ///
    /// Events:
    ///   - OnPlayerSpawned — fired when the player registers (player became available)
    ///   - OnPlayerDespawned — fired when the player unregisters (player is gone)
    ///   - OnPlayerRespawned — fired by RespawnService (Phase A7) after a respawn
    ///
    /// Why static, not Singleton:
    ///   - No Update, no GameObject, no scene presence needed
    ///   - State is owned by the Player GameObject (pushed in by PlayerRegistration)
    ///   - Can't have duplicates, no Awake ordering concerns
    ///
    /// Why this lives in Gameplay.Player and not Core:
    ///   - Holds direct references to PlayerController (Gameplay) and Health (Combat)
    ///   - Putting it in Core would invert the dependency direction
    /// </summary>
    public static class PlayerService
    {
        // -----------------------------------------------------------------
        // Cached references — set by Register, cleared by Unregister
        // -----------------------------------------------------------------

        /// <summary>
        /// The active PlayerController. Null if no player is registered.
        /// </summary>
        public static PlayerController Player { get; private set; }

        /// <summary>
        /// The player's Transform. Cached for distance/position lookups.
        /// Null if no player is registered.
        /// </summary>
        public static Transform PlayerTransform { get; private set; }

        /// <summary>
        /// The player's Health component. Cached for damage routing and OnDied subscription.
        /// Null if no player is registered.
        /// </summary>
        public static Health PlayerHealth { get; private set; }

        /// <summary>
        /// True if a player is currently registered and accessible.
        /// </summary>
        public static bool IsRegistered => Player != null;

        // -----------------------------------------------------------------
        // Events
        // -----------------------------------------------------------------

        /// <summary>
        /// Fires when the player registers (became available).
        /// Subscribers should use this instead of FindWithTag at runtime.
        /// </summary>
        public static event Action<PlayerController> OnPlayerSpawned;

        /// <summary>
        /// Fires when the player unregisters (scene unloading, manual despawn).
        /// Subscribers should drop their cached references.
        /// </summary>
        public static event Action<PlayerController> OnPlayerDespawned;

        /// <summary>
        /// Fires when the player has been respawned (e.g., after a bust or death).
        /// Currently fired by Phase A7 (RespawnService). Subscribers reset state here.
        /// The PlayerController reference does NOT change — same instance, just reset.
        /// </summary>
        public static event Action OnPlayerRespawned;

        // -----------------------------------------------------------------
        // Registration API — called by PlayerRegistration component
        // -----------------------------------------------------------------

        /// <summary>
        /// Register the active player. Called by PlayerRegistration in Awake.
        /// If a player is already registered, this logs a warning and replaces
        /// the registration (the new player wins).
        /// </summary>
        internal static void Register(PlayerController controller, Health health)
        {
            if (controller == null)
            {
                Debug.LogError("[PlayerService] Register called with null controller. Ignoring.");
                return;
            }

            if (Player != null && Player != controller)
            {
                Debug.LogWarning(
                    $"[PlayerService] Replacing existing registered player '{Player.name}' with '{controller.name}'.");
            }

            Player = controller;
            PlayerTransform = controller.transform;
            PlayerHealth = health;

            Debug.Log($"[PlayerService] Player registered: '{controller.name}'");
            OnPlayerSpawned?.Invoke(controller);
        }

        /// <summary>
        /// Unregister the active player. Called by PlayerRegistration in OnDestroy.
        /// Only unregisters if the controller matches — prevents stale unregister calls
        /// from clobbering a new registration.
        /// </summary>
        internal static void Unregister(PlayerController controller)
        {
            if (controller == null) return;
            if (Player != controller) return;

            PlayerController previous = Player;

            Player = null;
            PlayerTransform = null;
            PlayerHealth = null;

            Debug.Log($"[PlayerService] Player unregistered: '{previous.name}'");
            OnPlayerDespawned?.Invoke(previous);
        }

        /// <summary>
        /// Broadcast a respawn event. Called by RespawnService (Phase A7) after
        /// resetting the player. Does NOT change the registered references —
        /// the same PlayerController instance is still active.
        /// </summary>
        public static void NotifyRespawned()
        {
            if (Player == null)
            {
                Debug.LogWarning("[PlayerService] NotifyRespawned called but no player is registered.");
                return;
            }

            Debug.Log("[PlayerService] Player respawned.");
            OnPlayerRespawned?.Invoke();
        }

        // -----------------------------------------------------------------
        // Editor reset hook
        // -----------------------------------------------------------------

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only: clear static state when entering Play mode.
        /// Without this, static references can persist across Play sessions in editor,
        /// pointing at destroyed GameObjects.
        ///
        /// In a build, statics are reset on app startup, so this hook does nothing.
        /// </summary>
        [UnityEditor.InitializeOnEnterPlayMode]
        private static void ResetOnEnterPlayMode()
        {
            Player = null;
            PlayerTransform = null;
            PlayerHealth = null;
            OnPlayerSpawned = null;
            OnPlayerDespawned = null;
            OnPlayerRespawned = null;
        }
#endif
    }
}