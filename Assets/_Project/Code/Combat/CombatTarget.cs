using UnityEngine;

/// <summary>
/// Marks a GameObject as a valid target for combat systems.
/// Provides a target point (chest height) for aiming and distance checks.
/// Any entity that can be fought should have this component.
/// </summary>
[RequireComponent(typeof(Health))]
public class CombatTarget : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("Height offset from transform origin for the target point (chest height).")]
    [SerializeField] private float targetHeightOffset = 1.0f;

    private Health _health;

    public Health Health => _health;
    public bool IsAlive => _health != null && !_health.IsDead;

    private void Awake()
    {
        _health = GetComponent<Health>();
    }

    /// <summary>
    /// Returns the world position that attackers should aim at (chest height).
    /// </summary>
    public Vector3 GetTargetPoint()
    {
        return transform.position + Vector3.up * targetHeightOffset;
    }
}