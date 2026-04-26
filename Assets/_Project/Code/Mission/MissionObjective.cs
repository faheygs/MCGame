using UnityEngine;
using System.Collections.Generic;
using MCGame.Core;
using MCGame.Gameplay.AI;
using MCGame.Gameplay.Player;
using MCGame.Gameplay.UI;

namespace MCGame.Gameplay.Mission
{
    /// <summary>
    /// Spawned dynamically by MissionManager when a mission starts.
    /// Registers a minimap marker and handles objective completion.
    /// </summary>
    public class MissionObjective : MonoBehaviour, IInteractable
    {
        private float _triggerRadius = 3f;
        private ObjectiveType _type = ObjectiveType.GoToLocation;
        private string _promptText = "Interact";
        private Transform _player;
        private int _minimapMarkerId = -1;
        private bool _isRegistered;

        // DefeatTarget tracking
        private List<EnemyAI> _spawnedEnemies = new();
        private int _enemiesDefeated;
        private int _totalEnemies;

        public void Initialize(MissionData mission)
        {
            _triggerRadius = mission.objectiveRadius;
            _type = mission.objectiveType;
            _promptText = mission.objectivePromptText;

            if (_type == ObjectiveType.DefeatTarget)
            {
                HideVisualMarker();
                SpawnEnemies(mission);
            }
        }

        public void SetRadius(float radius)
        {
            _triggerRadius = radius;
        }

        private void Start()
        {
            _player = PlayerService.PlayerTransform;
            if (_player == null)
            {
                Debug.LogError("[MissionObjective] PlayerService has no registered player. Objective cannot track distance.", this);
            }

            if (MinimapMarkerManager.Instance != null)
                _minimapMarkerId = MinimapMarkerManager.Instance.RegisterObjectiveMarker(
                    () => transform.position
                );
        }

        private void Update()
        {
            if (MissionManager.Instance == null) return;
            if (!MissionManager.Instance.IsMissionActive) return;
            if (_player == null) return;

            float distance = Vector3.Distance(transform.position, _player.position);

            switch (_type)
            {
                case ObjectiveType.GoToLocation:
                    if (distance <= _triggerRadius)
                        MissionManager.Instance.CompleteMission();
                    break;

                case ObjectiveType.Interact:
                    HandleInteractProximity(distance);
                    break;

                case ObjectiveType.DefeatTarget:
                    // Completion handled by enemy death callbacks
                    break;
            }
        }

        private void HideVisualMarker()
        {
            foreach (Transform child in transform)
            {
                MeshRenderer mr = child.GetComponent<MeshRenderer>();
                if (mr != null)
                    child.gameObject.SetActive(false);
            }
        }

        private void HandleInteractProximity(float distance)
        {
            bool shouldRegister = distance <= _triggerRadius &&
                                  PlayerStateManager.Instance != null &&
                                  !PlayerStateManager.Instance.IsInVehicle;

            if (shouldRegister && !_isRegistered)
            {
                InteractionManager.Instance?.Register(this);
                _isRegistered = true;
            }
            else if (!shouldRegister && _isRegistered)
            {
                InteractionManager.Instance?.Unregister(this);
                _isRegistered = false;
            }
        }

        private void SpawnEnemies(MissionData mission)
        {
            if (mission.enemyPrefab == null)
            {
                Debug.LogError("[MissionObjective] DefeatTarget mission has no enemy prefab assigned.");
                return;
            }

            _totalEnemies = mission.enemyCount;
            _enemiesDefeated = 0;

            for (int i = 0; i < mission.enemyCount; i++)
            {
                float angle = (360f / mission.enemyCount) * i;
                Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * 3f;
                Vector3 spawnPos = transform.position + offset;

                GameObject enemyGO = Instantiate(mission.enemyPrefab, spawnPos, Quaternion.identity);
                EnemyAI enemy = enemyGO.GetComponent<EnemyAI>();

                if (enemy != null)
                {
                    _spawnedEnemies.Add(enemy);
                    enemy.OnDefeated += HandleEnemyDefeated;
                }
            }
        }

        private void HandleEnemyDefeated()
        {
            _enemiesDefeated++;

            if (_enemiesDefeated >= _totalEnemies)
                MissionManager.Instance?.CompleteMission();
        }

        private void OnDestroy()
        {
            if (_minimapMarkerId >= 0 && MinimapMarkerManager.Instance != null)
            {
                MinimapMarkerManager.Instance.UnregisterMissionMarker(_minimapMarkerId);
                _minimapMarkerId = -1;
            }

            if (_isRegistered && InteractionManager.Instance != null)
            {
                InteractionManager.Instance.Unregister(this);
                _isRegistered = false;
            }

            foreach (EnemyAI enemy in _spawnedEnemies)
            {
                if (enemy != null)
                    enemy.OnDefeated -= HandleEnemyDefeated;
            }
        }

        // --- IInteractable (Interact type only) ---

        public int Priority => 15;
        public Vector3 GetPosition() => transform.position;
        public string GetPromptText() => _promptText;
        public bool ShouldShowPrompt() => true;

        public bool CanInteract()
        {
            if (_type != ObjectiveType.Interact) return false;
            if (MissionManager.Instance == null) return false;
            if (!MissionManager.Instance.IsMissionActive) return false;
            return true;
        }

        public void OnInteract()
        {
            if (!CanInteract()) return;
            MissionManager.Instance.CompleteMission();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _type == ObjectiveType.GoToLocation ? Color.green :
                           _type == ObjectiveType.Interact ? Color.cyan : Color.red;
            Gizmos.DrawWireSphere(transform.position, _triggerRadius);
        }
    }
}