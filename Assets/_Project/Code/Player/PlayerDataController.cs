using System;
using UnityEngine;
using MCGame.Core;

namespace MCGame.Gameplay.Player
{
    /// <summary>
    /// Public API and Unity adapter for player runtime data.
    /// Owns the PlayerData instance, fires events on state changes,
    /// and provides the public methods consumed by other systems.
    ///
    /// Lives on the Player GameObject. Singleton for convenience —
    /// other systems access via PlayerDataController.Instance.
    ///
    /// Replaces the old PlayerStats ScriptableObject. Same surface area,
    /// fundamentally different ownership.
    /// </summary>
    public class PlayerDataController : Singleton<PlayerDataController>
    {
        [Header("Configuration")]
        [Tooltip("PlayerConfig asset — designer-tunable static data. Required.")]
        [SerializeField] private PlayerConfig config;

        // -----------------------------------------------------------------
        // Runtime data — owned by this Controller, never persisted to disk
        // -----------------------------------------------------------------

        public PlayerData Data { get; private set; }
        public PlayerConfig Config => config;

        // -----------------------------------------------------------------
        // Convenience accessors — proxy through to Data
        // -----------------------------------------------------------------

        public int Money => Data.Money;
        public int Reputation => Data.Reputation;
        public int TotalReputation => Data.TotalReputation;
        public int ReputationToNextRank => GetReputationToNextRank();
        public float Health => Data.Health;
        public float MaxHealth => config != null ? config.maxHealth : 100f;
        public int HeatLevel => Data.HeatLevel;
        public int MaxHeatLevel => config != null ? config.maxHeatLevel : 5;
        public ClubRank CurrentRank => Data.CurrentRank;
        public string CurrentRankDisplayName => GetRankDisplayName(Data.CurrentRank);
        public int BustStreak => Data.BustStreak;
        public bool IsLayingLow => Data.IsLayingLow;
        public float LayLowTimeRemaining => Data.LayLowTimeRemaining;

        // -----------------------------------------------------------------
        // Events — same names as old PlayerStats, for migration compatibility
        // -----------------------------------------------------------------

        public event Action<int> OnMoneyChanged;
        public event Action<int> OnReputationChanged;
        public event Action<float> OnHealthChanged;
        public event Action<int> OnHeatChanged;
        public event Action<ClubRank> OnRankChanged;
        public event Action<bool> OnLayLowChanged;
        public event Action<float> OnLayLowTimerUpdated;

        // -----------------------------------------------------------------
        // Lifecycle
        // -----------------------------------------------------------------

        protected override void OnAwake()
        {
            if (config == null)
            {
                Debug.LogError("[PlayerDataController] PlayerConfig not assigned. Player data system disabled.", this);
                enabled = false;
                return;
            }

            Data = new PlayerData(config);
            Debug.Log("[PlayerDataController] Fresh PlayerData created from config. Money=" + Data.Money + ", Rank=" + Data.CurrentRank);
        }

        // -----------------------------------------------------------------
        // Economy
        // -----------------------------------------------------------------

        public void AddMoney(int amount)
        {
            int newValue = Data.AddMoney(amount);
            OnMoneyChanged?.Invoke(newValue);
        }

        public void RemoveMoney(int amount)
        {
            // Same behavior as old PlayerStats.RemoveMoney — clamped to zero
            int newValue = Data.AddMoney(-amount);
            OnMoneyChanged?.Invoke(newValue);
        }

        public void LoseMoneyPercent(float percent)
        {
            Data.LoseMoneyPercent(percent);
            OnMoneyChanged?.Invoke(Data.Money);
            Debug.Log($"[PlayerDataController] Lost {percent * 100}% of money. Remaining: {Data.Money}");
        }

        // -----------------------------------------------------------------
        // Reputation & Rank
        // -----------------------------------------------------------------

        public void AddReputation(int amount)
        {
            int newRep = Data.AddReputation(amount);
            CheckRankUp();
            OnReputationChanged?.Invoke(newRep);
        }

        public void LoseReputation(int amount)
        {
            Data.LoseReputation(amount);
            OnReputationChanged?.Invoke(Data.Reputation);
            Debug.Log($"[PlayerDataController] Lost {amount} reputation. Current: {Data.Reputation}, Total: {Data.TotalReputation}");
        }

        private int GetReputationToNextRank()
        {
            if (config == null) return int.MaxValue;
            RankDefinition def = config.GetRankDefinition(Data.CurrentRank);
            return def.reputationToNextRank > 0 ? def.reputationToNextRank : int.MaxValue;
        }

        private string GetRankDisplayName(ClubRank rank)
        {
            if (config == null) return rank.ToString();
            RankDefinition def = config.GetRankDefinition(rank);
            return string.IsNullOrEmpty(def.displayName) ? rank.ToString() : def.displayName;
        }

        private void CheckRankUp()
        {
            int threshold = GetReputationToNextRank();
            if (threshold <= 0 || threshold == int.MaxValue) return;
            if (Data.Reputation < threshold) return;

            ClubRank next = config.GetNextRank(Data.CurrentRank);
            if (next == Data.CurrentRank) return; // Already at top

            Data.SetRank(next);
            Data.SetReputation(0);
            OnRankChanged?.Invoke(next);
            Debug.Log($"[PlayerDataController] Promoted to {GetRankDisplayName(next)}!");
        }

        // -----------------------------------------------------------------
        // Health
        // -----------------------------------------------------------------

        public void TakeDamage(float amount)
        {
            float newHealth = Data.TakeDamage(amount);
            OnHealthChanged?.Invoke(newHealth);
        }

        public void Heal(float amount)
        {
            float newHealth = Data.Heal(amount);
            OnHealthChanged?.Invoke(newHealth);
        }

        public void HealToFull()
        {
            float newHealth = Data.SetHealth(MaxHealth);
            OnHealthChanged?.Invoke(newHealth);
        }

        // -----------------------------------------------------------------
        // Heat
        // -----------------------------------------------------------------

        public void AddHeat(int amount)
        {
            int newHeat = Data.AddHeat(amount);
            OnHeatChanged?.Invoke(newHeat);
        }

        public void RemoveHeat(int amount)
        {
            int newHeat = Data.RemoveHeat(amount);
            OnHeatChanged?.Invoke(newHeat);
        }

        // -----------------------------------------------------------------
        // Bust System
        // -----------------------------------------------------------------

        public void IncrementBustStreak()
        {
            int newStreak = Data.IncrementBustStreak();
            Debug.Log($"[PlayerDataController] Bust streak: {newStreak}");
        }

        public void DecrementBustStreak()
        {
            int newStreak = Data.DecrementBustStreak();
            Debug.Log($"[PlayerDataController] Bust streak decayed: {newStreak}");
        }

        public void StartLayLow(float duration)
        {
            Data.StartLayLow(duration);
            OnLayLowChanged?.Invoke(true);
            Debug.Log($"[PlayerDataController] Laying low for {duration} seconds.");
        }

        public void ExtendLayLow(float additionalTime)
        {
            if (!Data.IsLayingLow) return;
            Data.ExtendLayLow(additionalTime);
            Debug.Log($"[PlayerDataController] Lay-low extended by {additionalTime}s. Remaining: {Data.LayLowTimeRemaining}s");
        }

        public void UpdateLayLowTimer(float deltaTime)
        {
            if (!Data.IsLayingLow) return;
            bool wasLayingLow = Data.IsLayingLow;
            float remaining = Data.TickLayLow(deltaTime);
            OnLayLowTimerUpdated?.Invoke(remaining);
            if (wasLayingLow && !Data.IsLayingLow)
            {
                OnLayLowChanged?.Invoke(false);
                Debug.Log("[PlayerDataController] Lay-low ended. BACK IN BUSINESS.");
            }
        }
    }
}