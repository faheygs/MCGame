using System;
using System.Collections.Generic;
using UnityEngine;
using MCGame.Core;
using MCGame.Gameplay.Player;

namespace MCGame.Gameplay.World
{
    /// <summary>
    /// Singleton tracking all registered districts in the loaded scenes.
    /// Notifies subscribers when the player enters or leaves a district.
    ///
    /// Lives in MCGame.Gameplay.World because it depends on PlayerService.
    /// </summary>
    public class DistrictManager : Singleton<DistrictManager>
    {
        private readonly List<District> _registeredDistricts = new List<District>();

        [Header("Auto-Discovery")]
        [Tooltip("If true, scans the scene for all District components on Start.")]
        [SerializeField] private bool autoDiscoverOnStart = true;

        [Header("Player Tracking")]
        [SerializeField] private float playerCheckInterval = 0.25f;

        private Transform _player;
        private District _currentPlayerDistrict;
        private float _lastCheckTime;

        public District CurrentDistrict => _currentPlayerDistrict;

        public event Action<District, District> OnDistrictChanged;

        private void Start()
        {
            if (autoDiscoverOnStart)
                DiscoverAllDistricts();

            _player = PlayerService.PlayerTransform;
            if (_player == null)
                Debug.LogError("[DistrictManager] PlayerService has no registered player. District tracking disabled.");
        }

        public void DiscoverAllDistricts()
        {
            District[] found = FindObjectsByType<District>(FindObjectsInactive.Exclude);
            for (int i = 0; i < found.Length; i++)
                RegisterDistrict(found[i]);
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
}