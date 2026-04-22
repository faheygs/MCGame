using UnityEngine;
using System;

/// <summary>
/// Generic health component. Attach to anything that can take damage:
/// player, enemies, future destructibles.
///
/// Fires events on damage and death so other components can react
/// without coupling to Health's internals.
/// </summary>
public class Health : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int currentHP;

    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public bool IsDead => currentHP <= 0;

    /// <summary>Fires when this entity takes damage. Passes the DamageInfo.</summary>
    public event Action<DamageInfo> OnDamaged;

    /// <summary>Fires once when health reaches zero.</summary>
    public event Action OnDied;

    private bool _hasDied;

    private void Awake()
    {
        currentHP = maxHP;
    }

    /// <summary>
    /// Deal damage to this entity. Clamps HP to zero.
    /// Fires OnDamaged, and OnDied if this is the killing blow.
    /// </summary>
    public void TakeDamage(DamageInfo info)
    {
        if (_hasDied) return;

        currentHP = Mathf.Max(0, currentHP - info.amount);
        OnDamaged?.Invoke(info);

        if (currentHP <= 0 && !_hasDied)
        {
            _hasDied = true;
            OnDied?.Invoke();
        }
    }

    /// <summary>
    /// Heal this entity. Clamps HP to maxHP.
    /// </summary>
    public void Heal(int amount)
    {
        if (_hasDied) return;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
    }

    /// <summary>
    /// Reset to full health. Used for respawn/retry.
    /// </summary>
    public void Reset()
    {
        currentHP = maxHP;
        _hasDied = false;
    }
}