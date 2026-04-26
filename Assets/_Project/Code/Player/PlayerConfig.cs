using UnityEngine;

namespace MCGame.Gameplay.Player
{
    /// <summary>
    /// Designer-tunable static configuration for the player.
    /// NEVER mutated at runtime. Read-only data referenced by PlayerDataController.
    ///
    /// Contains:
    ///   - Stat maximums (health, heat)
    ///   - Rank progression chain
    ///   - Bust system tunables (lay-low durations, money/rep penalties)
    ///   - Heat decay tunables
    ///
    /// Save data lives in PlayerData (plain C# class), NOT here.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "MCGame/Player Config")]
    public class PlayerConfig : ScriptableObject
    {
        [Header("Stats")]
        [Tooltip("Maximum player health. Used as full-heal target.")]
        public float maxHealth = 100f;

        [Header("Heat")]
        [Tooltip("Maximum heat level. Heat caps here regardless of crime accumulation.")]
        public int maxHeatLevel = 5;

        [Tooltip("Seconds of clean play before heat decays by 1 level.")]
        public float heatCooldownTime = 15f;

        [Header("Rank Progression")]
        [Tooltip("Rank chain in order from lowest (Prospect) to highest (President). Display names + rep thresholds.")]
        public RankDefinition[] rankChain;

        [Tooltip("Rank the player starts at on a fresh save.")]
        public ClubRank startingRank = ClubRank.Prospect;

        [Header("Bust System")]
        [Tooltip("Lay-low duration in seconds for each bust streak level (index 0 = streak 1, etc.). Cap is the last entry.")]
        public float[] layLowDurations = { 120f, 300f, 600f };

        [Tooltip("Percent of money lost on bust, per streak level (0..1). Cap is the last entry.")]
        public float[] moneyPenalties = { 0.15f, 0.30f, 0.50f };

        [Tooltip("Reputation lost on bust, per streak level. Cap is the last entry.")]
        public int[] repPenalties = { 50, 150, 300 };

        [Tooltip("Seconds of clean play before bust streak decays by 1.")]
        public float bustStreakDecayTime = 1200f;

        // -----------------------------------------------------------------
        // Lookup helpers — read-only utilities for code that needs the chain
        // -----------------------------------------------------------------

        /// <summary>
        /// Returns the RankDefinition for the given rank, or a default if not found.
        /// </summary>
        public RankDefinition GetRankDefinition(ClubRank rank)
        {
            if (rankChain == null) return default;

            for (int i = 0; i < rankChain.Length; i++)
            {
                if (rankChain[i].rank == rank)
                    return rankChain[i];
            }
            return default;
        }

        /// <summary>
        /// Returns the next rank in the progression chain, or the same rank if at top.
        /// </summary>
        public ClubRank GetNextRank(ClubRank current)
        {
            if (current >= ClubRank.President) return ClubRank.President;
            return (ClubRank)((int)current + 1);
        }

        /// <summary>
        /// Returns the bust consequence index for a given streak.
        /// Streak 1 → index 0, streak 2 → index 1, streak 3+ → cap at last entry.
        /// </summary>
        public int GetBustConsequenceIndex(int streak)
        {
            if (streak < 1) return 0;
            int maxIndex = Mathf.Min(layLowDurations.Length, moneyPenalties.Length, repPenalties.Length) - 1;
            return Mathf.Min(streak - 1, maxIndex);
        }
    }
}