using System;
using UnityEngine;

namespace MCGame.Gameplay.Player
{
    /// <summary>
    /// Designer-tunable rank entry. Lives in PlayerConfig as part of the rank chain.
    /// Defines the rank's display name and how much rep is needed to reach the NEXT rank.
    /// </summary>
    [Serializable]
    public struct RankDefinition
    {
        [Tooltip("Which rank this entry represents.")]
        public ClubRank rank;

        [Tooltip("Player-facing display name (e.g. 'Patched Member' for ClubRank.PatchedMember).")]
        public string displayName;

        [Tooltip("Reputation required to advance from THIS rank to the next. Use 0 or negative for the final rank.")]
        public int reputationToNextRank;
    }
}