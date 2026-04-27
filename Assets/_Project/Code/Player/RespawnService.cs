using UnityEngine;
using MCGame.Combat;
using MCGame.Core;
using MCGame.World;

namespace MCGame.Gameplay.Player
{
    /// <summary>
    /// Static service that owns "how to respawn the player."
    /// Handles position reset, health reset, animator reset, controller re-enable,
    /// and broadcasts the respawn via PlayerService.NotifyRespawned().
    /// </summary>
    public static class RespawnService
    {
        // -----------------------------------------------------------------
        // Public API
        // -----------------------------------------------------------------

        public static bool RespawnPlayer()
        {
            return RespawnPlayer(spawnId: null);
        }

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

            if (!string.IsNullOrEmpty(spawnId))
            {
                foreach (SpawnPoint sp in spawnPoints)
                {
                    if (sp.SpawnId == spawnId)
                        return sp.Position;
                }
                Debug.LogWarning($"[RespawnService] No spawn point with id '{spawnId}' found. Falling back to PlayerStart.");
            }

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
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = position;

            if (cc != null) cc.enabled = true;
        }

        private static void ResetHealth(PlayerController player)
        {
            Health health = PlayerService.PlayerHealth;
            if (health != null) health.Reset();

            if (PlayerDataController.Instance != null)
                PlayerDataController.Instance.HealToFull();
        }

        private static void ResetAnimator(PlayerController player)
        {
            Animator playerAnimator = player.GetComponentInChildren<Animator>();
            if (playerAnimator == null) return;

            // Clear lingering attack/hit/knockout triggers so they don't immediately re-fire.
            playerAnimator.ResetTrigger(AnimatorParams.Knockout);
            playerAnimator.ResetTrigger(AnimatorParams.Hit);
            playerAnimator.ResetTrigger(AnimatorParams.LightPunch);
            playerAnimator.ResetTrigger(AnimatorParams.LightKick);
            playerAnimator.ResetTrigger(AnimatorParams.HeavyPunch);
            playerAnimator.ResetTrigger(AnimatorParams.HeavyKick);

            // Reset every layer to a known-good state.
            // Try Idle first, then Empty. First match wins per layer.
            int[] candidateStates = { AnimatorParams.IdleState, AnimatorParams.EmptyState };

            for (int layer = 0; layer < playerAnimator.layerCount; layer++)
            {
                foreach (int stateHash in candidateStates)
                {
                    if (playerAnimator.HasState(layer, stateHash))
                    {
                        playerAnimator.Play(stateHash, layer, 0f);
                        break;
                    }
                }
            }
        }

        private static void ResetCombat(PlayerController player)
        {
            PlayerCombat combat = player.GetComponent<PlayerCombat>();
            if (combat == null) return;

            combat.StopAllCoroutines();
            combat.enabled = false;
            combat.enabled = true;
        }

        private static void ResetController(PlayerController player)
        {
            player.enabled = true;
        }
    }
}