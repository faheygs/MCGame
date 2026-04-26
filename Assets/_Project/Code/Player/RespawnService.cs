using UnityEngine;
using MCGame.Combat;
using MCGame.World;

namespace MCGame.Gameplay.Player
{
    /// <summary>
    /// Static service that owns "how to respawn the player."
    /// Handles position reset, health reset, animator reset, controller re-enable,
    /// and broadcasts the respawn via PlayerService.NotifyRespawned().
    ///
    /// Called by:
    ///   - PlayerHealth (on non-police-caused deaths)
    ///   - PoliceManager (after a bust sequence completes — wired in A7.3)
    ///   - Future: debug keys, fall-out-of-world detection, save-load, etc.
    ///
    /// Does NOT own:
    ///   - Bust consequences (lay-low, money penalties) — those are police-specific, stay in PoliceManager
    ///   - Mission state on death — MissionManager handles that via PlayerHealth
    ///   - Game state transitions — GameManager owns those
    ///   - UI fade/transition effects — future feature
    /// </summary>
    public static class RespawnService
    {
        // -----------------------------------------------------------------
        // Public API
        // -----------------------------------------------------------------

        /// <summary>
        /// Respawn the player at the default PlayerStart spawn point.
        /// Returns true on success, false if the respawn couldn't be completed
        /// (e.g., no PlayerStart in scene, no player registered).
        /// </summary>
        public static bool RespawnPlayer()
        {
            return RespawnPlayer(spawnId: null);
        }

        /// <summary>
        /// Respawn the player at a specific named spawn point.
        /// If spawnId is null or no matching spawn is found, falls back to the first
        /// SpawnPoint of type PlayerStart.
        /// </summary>
        public static bool RespawnPlayer(string spawnId)
        {
            if (!PlayerService.IsRegistered)
            {
                Debug.LogError("[RespawnService] No player registered. Cannot respawn.");
                return false;
            }

            Vector3? spawnPos = FindSpawnPoint(spawnId);
            if (!spawnPos.HasValue)
            {
                Debug.LogError($"[RespawnService] No valid spawn point found (requested id: '{spawnId ?? "default"}'). Respawning at origin.");
                spawnPos = Vector3.zero;
            }

            ExecuteRespawn(spawnPos.Value);
            return true;
        }

        // -----------------------------------------------------------------
        // Spawn point lookup
        // -----------------------------------------------------------------

        private static Vector3? FindSpawnPoint(string spawnId)
        {
            SpawnPoint[] spawnPoints = Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Exclude);

            // First pass: look for a matching named spawn ID
            if (!string.IsNullOrEmpty(spawnId))
            {
                foreach (SpawnPoint sp in spawnPoints)
                {
                    if (sp.SpawnId == spawnId)
                        return sp.Position;
                }
                Debug.LogWarning($"[RespawnService] No spawn point with id '{spawnId}' found. Falling back to PlayerStart.");
            }

            // Default: first PlayerStart-typed spawn
            foreach (SpawnPoint sp in spawnPoints)
            {
                if (sp.Type == SpawnPoint.SpawnType.PlayerStart)
                    return sp.Position;
            }

            return null;
        }

        // -----------------------------------------------------------------
        // Respawn execution sequence
        // -----------------------------------------------------------------

        private static void ExecuteRespawn(Vector3 position)
        {
            PlayerController player = PlayerService.Player;
            if (player == null)
            {
                Debug.LogError("[RespawnService] PlayerService.Player is null at execution time. Aborting.");
                return;
            }

            ResetPosition(player, position);
            ResetHealth(player);
            ResetAnimator(player);
            ResetCombat(player);
            ResetController(player);

            Debug.Log($"[RespawnService] Player respawned at {position}");
            PlayerService.NotifyRespawned();
        }

        private static void ResetPosition(PlayerController player, Vector3 position)
        {
            // CharacterController must be disabled before teleporting, then re-enabled.
            // Otherwise the controller's internal state fights the position change.
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = position;

            if (cc != null) cc.enabled = true;
        }

        private static void ResetHealth(PlayerController player)
        {
            // Reset the Health component (combat damage routing layer)
            Health health = PlayerService.PlayerHealth;
            if (health != null) health.Reset();

            // Reset PlayerData (HUD events layer)
            if (PlayerDataController.Instance != null)
                PlayerDataController.Instance.HealToFull();
        }

        private static void ResetAnimator(PlayerController player)
        {
            // Clean animator reset across all layers.
            // For each layer, we attempt to play a default state if it exists.
            // This avoids the "Animator.GotoState: State could not be found" warning that
            // happened when the previous implementation tried to play "Empty" on layers
            // that didn't have it.
            Animator playerAnimator = player.GetComponentInChildren<Animator>();
            if (playerAnimator == null) return;

            // Clear lingering attack/hit/knockout triggers so they don't immediately re-fire.
            playerAnimator.ResetTrigger("Knockout");
            playerAnimator.ResetTrigger("Hit");
            playerAnimator.ResetTrigger("LightPunch");
            playerAnimator.ResetTrigger("LightKick");
            playerAnimator.ResetTrigger("HeavyPunch");
            playerAnimator.ResetTrigger("HeavyKick");

            // Reset every layer to a known-good state.
            // Try common default state names in priority order. First match wins.
            // If no match exists on a layer, the layer keeps its current state (no warning).
            string[] candidateStates = { "Idle", "Empty" };

            for (int layer = 0; layer < playerAnimator.layerCount; layer++)
            {
                foreach (string stateName in candidateStates)
                {
                    int stateHash = Animator.StringToHash(stateName);
                    if (playerAnimator.HasState(layer, stateHash))
                    {
                        playerAnimator.Play(stateHash, layer, 0f);
                        break; // Move to next layer
                    }
                }
            }
        }

        private static void ResetCombat(PlayerController player)
        {
            // PlayerCombat may be in the middle of an attack coroutine when respawn
            // happens. Stop coroutines + cycle the component to ensure clean state.
            PlayerCombat combat = player.GetComponent<PlayerCombat>();
            if (combat == null) return;

            combat.StopAllCoroutines();
            combat.enabled = false;
            combat.enabled = true;
        }

        private static void ResetController(PlayerController player)
        {
            // Re-enable the player controller (PlayerHealth.HandleDied disables it on death).
            player.enabled = true;
        }
    }
}