using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton tracking all registered districts in the loaded scenes.
/// Notifies subscribers when the player enters or leaves a district.
///
/// Subscribe to OnDistrictChanged to react to district transitions
/// (e.g., minimap label change, music change, jurisdiction update).
/// </summary>
public class DistrictManager : MonoBehaviour
{
    public static DistrictManager Instance { get; private set; }

    private readonly List<District> _registeredDistricts = new List<District>();

    [Header("Player Tracking")]
    [SerializeField] private float playerCheckInterval = 0.25f;

    private Transform _player;
    private District _currentPlayerDistrict;
    private float _lastCheckTime;

    /// <summary>
    /// Current district the player is inside. Null if not in any district.
    /// </summary>
    public District CurrentDistrict => _currentPlayerDistrict;

    /// <summary>
    /// Fires when the player's current district changes.
    /// Parameters: (previous district, new district). Either can be null.
    /// </summary>
    public event Action<District, District> OnDistrictChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[DistrictManager] Duplicate instance on {name}. Destroying.");
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
            _player = playerGO.transform;
    }

    private void Update()
    {
        if (_player == null) return;
        if (Time.time - _lastCheckTime < playerCheckInterval) return;

        _lastCheckTime = Time.time;
        CheckPlayerDistrict();
    }

    private void CheckPlayerDistrict()
    {
        District found = null;
        Vector3 playerPos = _player.position;

        // Iterate districts, find the one containing the player
        for (int i = 0; i < _registeredDistricts.Count; i++)
        {
            District d = _registeredDistricts[i];
            if (d == null) continue;
            if (d.Contains(playerPos))
            {
                found = d;
                break;
            }
        }

        if (found != _currentPlayerDistrict)
        {
            District previous = _currentPlayerDistrict;
            _currentPlayerDistrict = found;
            OnDistrictChanged?.Invoke(previous, found);
        }
    }

    // --- Public registration API ---

    public void RegisterDistrict(District district)
    {
        if (district == null) return;
        if (_registeredDistricts.Contains(district)) return;
        _registeredDistricts.Add(district);
    }

    public void UnregisterDistrict(District district)
    {
        if (district == null) return;
        _registeredDistricts.Remove(district);

        if (_currentPlayerDistrict == district)
        {
            District previous = _currentPlayerDistrict;
            _currentPlayerDistrict = null;
            OnDistrictChanged?.Invoke(previous, null);
        }
    }

    public District GetDistrictAt(Vector3 worldPosition)
    {
        for (int i = 0; i < _registeredDistricts.Count; i++)
        {
            District d = _registeredDistricts[i];
            if (d != null && d.Contains(worldPosition)) return d;
        }
        return null;
    }

    public IReadOnlyList<District> GetAllDistricts() => _registeredDistricts;
}