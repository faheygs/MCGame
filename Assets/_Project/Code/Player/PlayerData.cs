using System.Collections.Generic;
using UnityEngine;

namespace MCGame.Gameplay.Player
{
    /// <summary>
    /// Pure runtime player state. No Unity dependencies (other than Mathf for clamping).
    /// Created fresh on every play session by PlayerDataController.
    /// Restored from save data when SaveSystem is built.
    ///
    /// This class:
    ///   - Has NO events (those live on PlayerDataController)
    ///   - Is NOT a MonoBehaviour or ScriptableObject
    ///   - Implements ISaveable for future SaveSystem integration
    ///   - Uses internal mutators (PlayerDataController owns the public API)
    /// </summary>
    public class PlayerData : ISaveable
    {
        // -----------------------------------------------------------------
        // Save format keys — STABLE FOREVER. Changing these breaks old saves.
        // -----------------------------------------------------------------
        private const string KEY_MONEY = "money";
        private const string KEY_REP = "reputation";
        private const string KEY_TOTAL_REP = "totalReputation";
        private const string KEY_HEALTH = "health";
        private const string KEY_HEAT = "heatLevel";
        private const string KEY_RANK = "currentRank";
        private const string KEY_BUST_STREAK = "bustStreak";
        private const string KEY_IS_LAYING_LOW = "isLayingLow";
        private const string KEY_LAY_LOW_REMAINING = "layLowTimeRemaining";

        // -----------------------------------------------------------------
        // Runtime state — read-only from outside, mutated via internal setters
        // -----------------------------------------------------------------

        public int Money { get; private set; }
        public int Reputation { get; private set; }
        public int TotalReputation { get; private set; }
        public float Health { get; private set; }
        public int HeatLevel { get; private set; }
        public ClubRank CurrentRank { get; private set; }
        public int BustStreak { get; private set; }
        public bool IsLayingLow { get; private set; }
        public float LayLowTimeRemaining { get; private set; }

        // -----------------------------------------------------------------
        // Reference to config for clamping/lookups (not serialized)
        // -----------------------------------------------------------------

        private readonly PlayerConfig _config;

        // -----------------------------------------------------------------
        // Construction — always seeded from a Config (for max values, defaults)
        // -----------------------------------------------------------------

        public PlayerData(PlayerConfig config)
        {
            _config = config;
            ResetToDefaults();
        }

        /// <summary>
        /// Resets all runtime state to defaults from config.
        /// Called by constructor and (eventually) by NewGame button.
        /// </summary>
        public void ResetToDefaults()
        {
            Money = 0;
            Reputation = 0;
            TotalReputation = 0;
            Health = _config != null ? _config.maxHealth : 100f;
            HeatLevel = 0;
            CurrentRank = _config != null ? _config.startingRank : ClubRank.Prospect;
            BustStreak = 0;
            IsLayingLow = false;
            LayLowTimeRemaining = 0f;
        }

        // -----------------------------------------------------------------
        // Mutators — internal so only PlayerDataController can call them.
        // The Controller is the public API; this class is the data store.
        // -----------------------------------------------------------------

        internal void SetMoney(int value) { Money = Mathf.Max(0, value); }
        internal int AddMoney(int amount) { Money = Mathf.Max(0, Money + amount); return Money; }

        internal int AddReputation(int amount)
        {
            Reputation = Mathf.Max(0, Reputation + amount);
            if (amount > 0) TotalReputation += amount;
            return Reputation;
        }
        internal void SetReputation(int value) { Reputation = Mathf.Max(0, value); }
        internal void SetTotalReputation(int value) { TotalReputation = Mathf.Max(0, value); }
        internal void LoseReputation(int amount)
        {
            Reputation = Mathf.Max(0, Reputation - amount);
            TotalReputation = Mathf.Max(0, TotalReputation - amount);
        }

        internal float SetHealth(float value)
        {
            float max = _config != null ? _config.maxHealth : 100f;
            Health = Mathf.Clamp(value, 0f, max);
            return Health;
        }
        internal float TakeDamage(float amount)
        {
            Health = Mathf.Max(0f, Health - amount);
            return Health;
        }
        internal float Heal(float amount)
        {
            float max = _config != null ? _config.maxHealth : 100f;
            Health = Mathf.Min(max, Health + amount);
            return Health;
        }

        internal int AddHeat(int amount)
        {
            int max = _config != null ? _config.maxHeatLevel : 5;
            HeatLevel = Mathf.Min(max, HeatLevel + amount);
            return HeatLevel;
        }
        internal int RemoveHeat(int amount)
        {
            HeatLevel = Mathf.Max(0, HeatLevel - amount);
            return HeatLevel;
        }

        internal void SetRank(ClubRank rank) { CurrentRank = rank; }

        internal int IncrementBustStreak() { BustStreak++; return BustStreak; }
        internal int DecrementBustStreak() { BustStreak = Mathf.Max(0, BustStreak - 1); return BustStreak; }

        internal void StartLayLow(float duration)
        {
            IsLayingLow = true;
            LayLowTimeRemaining = duration;
        }
        internal void ExtendLayLow(float additionalTime)
        {
            if (!IsLayingLow) return;
            LayLowTimeRemaining += additionalTime;
        }
        internal float TickLayLow(float deltaTime)
        {
            if (!IsLayingLow) return 0f;
            LayLowTimeRemaining -= deltaTime;
            if (LayLowTimeRemaining <= 0f)
            {
                LayLowTimeRemaining = 0f;
                IsLayingLow = false;
            }
            return LayLowTimeRemaining;
        }

        internal void LoseMoneyPercent(float percent)
        {
            int loss = Mathf.RoundToInt(Money * percent);
            Money = Mathf.Max(0, Money - loss);
        }

        // -----------------------------------------------------------------
        // ISaveable — explicit dictionary serialization, no reflection
        // -----------------------------------------------------------------

        public Dictionary<string, object> SaveToDictionary()
        {
            return new Dictionary<string, object>
            {
                { KEY_MONEY,             Money },
                { KEY_REP,               Reputation },
                { KEY_TOTAL_REP,         TotalReputation },
                { KEY_HEALTH,            Health },
                { KEY_HEAT,              HeatLevel },
                { KEY_RANK,              (int)CurrentRank },
                { KEY_BUST_STREAK,       BustStreak },
                { KEY_IS_LAYING_LOW,     IsLayingLow },
                { KEY_LAY_LOW_REMAINING, LayLowTimeRemaining },
            };
        }

        public void LoadFromDictionary(Dictionary<string, object> data)
        {
            if (data == null) return;

            if (data.TryGetValue(KEY_MONEY, out object money))             Money               = ToInt(money);
            if (data.TryGetValue(KEY_REP, out object rep))                 Reputation          = ToInt(rep);
            if (data.TryGetValue(KEY_TOTAL_REP, out object totalRep))      TotalReputation     = ToInt(totalRep);
            if (data.TryGetValue(KEY_HEALTH, out object hp))               Health              = ToFloat(hp);
            if (data.TryGetValue(KEY_HEAT, out object heat))               HeatLevel           = ToInt(heat);
            if (data.TryGetValue(KEY_RANK, out object rank))               CurrentRank         = (ClubRank)ToInt(rank);
            if (data.TryGetValue(KEY_BUST_STREAK, out object streak))      BustStreak          = ToInt(streak);
            if (data.TryGetValue(KEY_IS_LAYING_LOW, out object isLow))     IsLayingLow         = ToBool(isLow);
            if (data.TryGetValue(KEY_LAY_LOW_REMAINING, out object layLowRem)) LayLowTimeRemaining = ToFloat(layLowRem);
        }

        // -----------------------------------------------------------------
        // Defensive type coercion — JSON deserialization may give us any
        // numeric type (int, long, double). We handle them all gracefully.
        // -----------------------------------------------------------------

        private static int ToInt(object o)
        {
            if (o is int i) return i;
            if (o is long l) return (int)l;
            if (o is float f) return Mathf.RoundToInt(f);
            if (o is double d) return Mathf.RoundToInt((float)d);
            return 0;
        }

        private static float ToFloat(object o)
        {
            if (o is float f) return f;
            if (o is double d) return (float)d;
            if (o is int i) return i;
            if (o is long l) return l;
            return 0f;
        }

        private static bool ToBool(object o)
        {
            if (o is bool b) return b;
            return false;
        }
    }
}