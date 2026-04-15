using UnityEngine;
using System;

// PlayerStats is the single source of truth for all player data.
// Money, rep, health, and heat all live here.
// Any system that changes player data writes to this asset.
// The HUD listens to events and updates automatically.

[CreateAssetMenu(fileName = "PlayerStats", menuName = "MCGame/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Header("Identity")]
    public string clubRank = "Prospect";
    public string playerName = "Player";

    [Header("Economy")]
    [SerializeField] private int _money = 0;
    [SerializeField] private int _reputation = 0;
    [SerializeField] private int _reputationToNextRank = 500;

    [Header("Health")]
    [SerializeField] private float _health = 100f;
    [SerializeField] private float _maxHealth = 100f;

    [Header("Heat")]
    [SerializeField] private int _heatLevel = 0;
    [SerializeField] private int _maxHeatLevel = 5;

    [Header("Ammo")]
    [SerializeField] private int _currentAmmo = 0;
    [SerializeField] private int _maxAmmo = 0;

    // Events — HUD and other systems subscribe to these
    public event Action<int> OnMoneyChanged;
    public event Action<int> OnReputationChanged;
    public event Action<float> OnHealthChanged;
    public event Action<int> OnHeatChanged;
    public event Action<int, int> OnAmmoChanged;
    public event Action<string> OnRankChanged;

    // Public accessors
    public int Money => _money;
    public int Reputation => _reputation;
    public int ReputationToNextRank => _reputationToNextRank;
    public float Health => _health;
    public float MaxHealth => _maxHealth;
    public int HeatLevel => _heatLevel;
    public int MaxHeatLevel => _maxHeatLevel;
    public int CurrentAmmo => _currentAmmo;
    public int MaxAmmo => _maxAmmo;

    // Called when the asset is enabled — reset runtime state
    private void OnEnable()
    {
        ResetToDefaults();
    }

    private void ResetToDefaults()
    {
        _money = 0;
        _reputation = 0;
        _health = _maxHealth;
        _heatLevel = 0;
        _currentAmmo = 0;
    }

    // --- Economy ---

    public void AddMoney(int amount)
    {
        _money += amount;
        OnMoneyChanged?.Invoke(_money);
    }

    public void RemoveMoney(int amount)
    {
        _money = Mathf.Max(0, _money - amount);
        OnMoneyChanged?.Invoke(_money);
    }

    public void AddReputation(int amount)
    {
        _reputation += amount;
        OnReputationChanged?.Invoke(_reputation);
        CheckRankUp();
    }

    // --- Health ---

    public void TakeDamage(float amount)
    {
        _health = Mathf.Max(0f, _health - amount);
        OnHealthChanged?.Invoke(_health);
    }

    public void Heal(float amount)
    {
        _health = Mathf.Min(_maxHealth, _health + amount);
        OnHealthChanged?.Invoke(_health);
    }

    // --- Heat ---

    public void AddHeat(int amount)
    {
        _heatLevel = Mathf.Min(_maxHeatLevel, _heatLevel + amount);
        OnHeatChanged?.Invoke(_heatLevel);
    }

    public void RemoveHeat(int amount)
    {
        _heatLevel = Mathf.Max(0, _heatLevel - amount);
        OnHeatChanged?.Invoke(_heatLevel);
    }

    // --- Ammo ---

    public void SetAmmo(int current, int max)
    {
        _currentAmmo = current;
        _maxAmmo = max;
        OnAmmoChanged?.Invoke(_currentAmmo, _maxAmmo);
    }

    // --- Rank ---

    private void CheckRankUp()
    {
        if (_reputation >= _reputationToNextRank)
        {
            PromoteRank();
        }
    }

    private void PromoteRank()
    {
        // Rank progression for an MC
        switch (clubRank)
        {
            case "Prospect":
                clubRank = "Hangaround";
                _reputationToNextRank = 1000;
                break;
            case "Hangaround":
                clubRank = "Patched Member";
                _reputationToNextRank = 2500;
                break;
            case "Patched Member":
                clubRank = "Enforcer";
                _reputationToNextRank = 5000;
                break;
            case "Enforcer":
                clubRank = "Road Captain";
                _reputationToNextRank = 10000;
                break;
            case "Road Captain":
                clubRank = "Vice President";
                _reputationToNextRank = 20000;
                break;
            case "Vice President":
                clubRank = "President";
                _reputationToNextRank = int.MaxValue;
                break;
        }

        OnRankChanged?.Invoke(clubRank);
    }
}