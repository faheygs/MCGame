using UnityEngine;
using System;
using System.Collections.Generic;
using MCGame.Core;
using MCGame.Combat;
using MCGame.World;
using MCGame.Gameplay.Player;
using MCGame.Gameplay.Crime;
using MCGame.Gameplay.UI;

namespace MCGame.Gameplay.Police
{
    /// <summary>
    /// Manages law enforcement response to criminal activity.
    /// </summary>
    public class PoliceManager : Singleton<PoliceManager>
    {
        [Header("Corruption")]
        [Tooltip("Current corruption level.\n0 = No protection\n1 = Ignore severity 1\n2 = Ignore severity 1-2\n3 = Full local protection")]
        [Range(0, 3)]
        [SerializeField] private int corruptionLevel = 0;

        [Header("Police Spawning")]
        [Tooltip("Police NPC prefab to spawn")]
        [SerializeField] private GameObject policePrefab;

        [Tooltip("Maximum number of active police NPCs at any time")]
        [SerializeField] private int maxActivePolice = 6;

        [Tooltip("Minimum spawn distance from player (prevents pop-in)")]
        [SerializeField] private float minSpawnDistance = 30f;

        [Tooltip("Police beyond this distance from player are despawned and replaced")]
        [SerializeField] private float maxPoliceDistance = 80f;

        [Tooltip("Seconds between reinforcement checks when heat stays high")]
        [SerializeField] private float reinforcementInterval = 15f;

        public event Action<int> OnPoliceCountChanged;
        public event Action<CrimeType> OnCrimeSuppressedByCorruption;

        private List<GameObject> _activePolice = new List<GameObject>();
        private Dictionary<GameObject, int> _policeMarkerIds = new Dictionary<GameObject, int>();
        private int _currentHeatLevel;
        private float _reinforcementTimer;
        private float _bustStreakDecayTimer;
        private Health _playerHealth;
        private bool _processingBust;

        public int CorruptionLevel => corruptionLevel;
        public int ActivePoliceCount => _activePolice.Count;

        private void OnEnable()
        {
            if (CrimeManager.Instance != null)
                CrimeManager.Instance.OnCrimeReported += HandleCrimeReported;

            if (PlayerDataController.Instance != null)
                PlayerDataController.Instance.OnHeatChanged += HandleHeatChanged;
        }

        private void OnDisable()
        {
            if (CrimeManager.Instance != null)
                CrimeManager.Instance.OnCrimeReported -= HandleCrimeReported;

            if (PlayerDataController.Instance != null)
                PlayerDataController.Instance.OnHeatChanged -= HandleHeatChanged;

            if (_playerHealth != null)
                _playerHealth.OnDied -= HandlePlayerDied;
        }

        private void Start()
        {
            // Defensive re-subscription in case OnEnable fired before these singletons existed
            if (CrimeManager.Instance != null)
            {
                CrimeManager.Instance.OnCrimeReported -= HandleCrimeReported;
                CrimeManager.Instance.OnCrimeReported += HandleCrimeReported;
            }
            else
            {
                Debug.LogError("[PoliceManager] CrimeManager not found. Police will not respond to crimes.", this);
            }

            if (PlayerDataController.Instance != null)
            {
                PlayerDataController.Instance.OnHeatChanged -= HandleHeatChanged;
                PlayerDataController.Instance.OnHeatChanged += HandleHeatChanged;
            }
            else
            {
                Debug.LogError("[PoliceManager] PlayerDataController not found.", this);
            }

            _playerHealth = PlayerService.PlayerHealth;
            if (_playerHealth != null)
            {
                _playerHealth.OnDied += HandlePlayerDied;
            }
            else
            {
                Debug.LogError("[PoliceManager] PlayerService has no registered player health. Bust system disabled.", this);
            }
        }

        private void Update()
        {
            if (_currentHeatLevel > 0)
                HandleReinforcements();

            CleanupDestroyedPolice();
            HandleDistanceDespawn();

            if (PlayerDataController.Instance != null && PlayerDataController.Instance.IsLayingLow)
                PlayerDataController.Instance.UpdateLayLowTimer(Time.deltaTime);

            if (PlayerDataController.Instance != null &&
                PlayerDataController.Instance.BustStreak > 0 &&
                !PlayerDataController.Instance.IsLayingLow)
            {
                float decayTime = PlayerDataController.Instance.Config != null
                    ? PlayerDataController.Instance.Config.bustStreakDecayTime
                    : 1200f;

                _bustStreakDecayTimer += Time.deltaTime;
                if (_bustStreakDecayTimer >= decayTime)
                {
                    _bustStreakDecayTimer = 0f;
                    PlayerDataController.Instance.DecrementBustStreak();
                }
            }
        }

        // =========================================================================
        // CRIME RESPONSE
        // =========================================================================

        private void HandleCrimeReported(CrimeType crimeType, Vector3 crimePosition)
        {
            if (crimeType.severityTier <= corruptionLevel)
            {
                Debug.Log($"[PoliceManager] Crime '{crimeType.crimeName}' (severity {crimeType.severityTier}) " +
                          $"suppressed by corruption level {corruptionLevel}. No police response.");

                OnCrimeSuppressedByCorruption?.Invoke(crimeType);
                return;
            }

            Debug.Log($"[PoliceManager] Crime '{crimeType.crimeName}' acknowledged. " +
                      $"Severity {crimeType.severityTier} exceeds corruption {corruptionLevel}. Police will respond.");
        }

        // =========================================================================
        // HEAT RESPONSE
        // =========================================================================

        private void HandleHeatChanged(int newHeatLevel)
        {
            int previousHeat = _currentHeatLevel;
            _currentHeatLevel = newHeatLevel;

            if (_processingBust) return;

            Debug.Log($"[PoliceManager] Heat changed: {previousHeat} → {newHeatLevel}");

            if (newHeatLevel <= 0)
            {
                DespawnAllPolice();
            }
            else if (newHeatLevel > previousHeat)
            {
                _reinforcementTimer = 0f;
                EvaluatePoliceResponse();
            }
        }

        private void EvaluatePoliceResponse()
        {
            if (_currentHeatLevel <= corruptionLevel)
            {
                Debug.Log($"[PoliceManager] Heat {_currentHeatLevel} within corruption level {corruptionLevel}. No police spawn.");
                return;
            }

            int targetCount = GetTargetPoliceCount(_currentHeatLevel);
            int currentCount = _activePolice.Count;

            if (currentCount < targetCount)
            {
                int toSpawn = targetCount - currentCount;
                Debug.Log($"[PoliceManager] Spawning {toSpawn} police. Current: {currentCount}, Target: {targetCount}");
                SpawnPolice(toSpawn);
            }
        }

        private int GetTargetPoliceCount(int heatLevel)
        {
            switch (heatLevel)
            {
                case 1: return 1;
                case 2: return 2;
                case 3: return 3;
                case 4: return 5;
                case 5: return 6;
                default: return 0;
            }
        }

        // =========================================================================
        // REINFORCEMENTS
        // =========================================================================

        private void HandleReinforcements()
        {
            _reinforcementTimer += Time.deltaTime;

            if (_reinforcementTimer >= reinforcementInterval)
            {
                _reinforcementTimer = 0f;
                EvaluatePoliceResponse();
            }
        }

        // =========================================================================
        // POLICE SPAWNING
        // =========================================================================

        private void SpawnPolice(int count)
        {
            if (policePrefab == null)
            {
                Debug.LogWarning("[PoliceManager] No police prefab assigned. Cannot spawn police.");
                return;
            }

            Transform player = FindPlayer();
            if (player == null)
            {
                Debug.LogError("[PoliceManager] Cannot find player. Cannot spawn police.");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                if (_activePolice.Count >= maxActivePolice)
                {
                    Debug.Log("[PoliceManager] Max active police reached. Stopping spawn.");
                    break;
                }

                Vector3 spawnPos = GetPoliceSpawnPosition(player.position);
                if (spawnPos != Vector3.zero)
                {
                    GameObject police = Instantiate(policePrefab, spawnPos, Quaternion.identity);
                    _activePolice.Add(police);

                    if (MinimapMarkerManager.Instance != null && MinimapMarkerManager.Instance.IsReady)
                    {
                        Transform policeTransform = police.transform;
                        int markerId = MinimapMarkerManager.Instance.RegisterPoliceMarker(() => policeTransform.position);
                        _policeMarkerIds[police] = markerId;
                    }

                    Debug.Log($"[PoliceManager] Spawned police at {spawnPos}. Active: {_activePolice.Count}");
                }
            }

            OnPoliceCountChanged?.Invoke(_activePolice.Count);
        }

        private Vector3 GetPoliceSpawnPosition(Vector3 playerPosition)
        {
            SpawnPoint[] spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsInactive.Exclude);
            SpawnPoint bestSpawn = null;
            float bestDistance = float.MaxValue;

            foreach (SpawnPoint sp in spawnPoints)
            {
                if (sp.Type != SpawnPoint.SpawnType.Police) continue;

                float dist = Vector3.Distance(sp.Position, playerPosition);

                if (dist < minSpawnDistance) continue;

                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestSpawn = sp;
                }
            }

            if (bestSpawn != null)
                return bestSpawn.Position;

            Debug.LogWarning("[PoliceManager] No valid Police spawn points found. Using fallback position.");
            Vector3 randomDirection = UnityEngine.Random.insideUnitSphere;
            randomDirection.y = 0;
            randomDirection = randomDirection.normalized * minSpawnDistance;
            return playerPosition + randomDirection;
        }

        // =========================================================================
        // POLICE DESPAWNING
        // =========================================================================

        private void DespawnAllPolice()
        {
            Debug.Log($"[PoliceManager] Heat cleared. Disengaging {_activePolice.Count} police.");

            for (int i = _activePolice.Count - 1; i >= 0; i--)
            {
                if (_activePolice[i] != null)
                {
                    RemovePoliceMarker(_activePolice[i]);

                    PoliceNPC policeAI = _activePolice[i].GetComponent<PoliceNPC>();
                    if (policeAI != null)
                        policeAI.Disengage();
                    else
                        Destroy(_activePolice[i]);
                }
            }

            _activePolice.Clear();
            _policeMarkerIds.Clear();
            OnPoliceCountChanged?.Invoke(0);
        }

        public void UnregisterPolice(GameObject policeObj)
        {
            if (_activePolice.Remove(policeObj))
            {
                RemovePoliceMarker(policeObj);
                Debug.Log($"[PoliceManager] Police unregistered. Active: {_activePolice.Count}");
                OnPoliceCountChanged?.Invoke(_activePolice.Count);
            }
        }

        private void RemovePoliceMarker(GameObject policeObj)
        {
            if (_policeMarkerIds.TryGetValue(policeObj, out int markerId))
            {
                if (MinimapMarkerManager.Instance != null)
                    MinimapMarkerManager.Instance.UnregisterMissionMarker(markerId);
                _policeMarkerIds.Remove(policeObj);
            }
        }

        // =========================================================================
        // DISTANCE MANAGEMENT
        // =========================================================================

        private void HandleDistanceDespawn()
        {
            Transform player = FindPlayer();
            if (player == null) return;

            for (int i = _activePolice.Count - 1; i >= 0; i--)
            {
                if (_activePolice[i] == null) continue;

                float dist = Vector3.Distance(_activePolice[i].transform.position, player.position);
                if (dist > maxPoliceDistance)
                {
                    Debug.Log($"[PoliceManager] Police too far ({dist:F0}m). Despawning and replacing.");
                    RemovePoliceMarker(_activePolice[i]);
                    Destroy(_activePolice[i]);
                    _activePolice.RemoveAt(i);
                    SpawnPolice(1);
                }
            }
        }

        // =========================================================================
        // UTILITY
        // =========================================================================

        private Transform FindPlayer()
        {
            return PlayerService.PlayerTransform;
        }

        private void CleanupDestroyedPolice()
        {
            for (int i = _activePolice.Count - 1; i >= 0; i--)
            {
                if (_activePolice[i] == null)
                    _activePolice.RemoveAt(i);
            }
        }

        // =========================================================================
        // BUST SYSTEM
        // =========================================================================

        private void HandlePlayerDied()
        {
            if (_playerHealth == null) return;
            if (PlayerDataController.Instance == null) return;

            GameObject source = _playerHealth.LastDamageSource;
            if (source != null && source.GetComponent<PoliceNPC>() != null)
            {
                _processingBust = true;

                for (int i = _activePolice.Count - 1; i >= 0; i--)
                {
                    if (_activePolice[i] != null)
                    {
                        RemovePoliceMarker(_activePolice[i]);
                        Destroy(_activePolice[i]);
                    }
                }
                _activePolice.Clear();
                _policeMarkerIds.Clear();

                while (PlayerDataController.Instance.HeatLevel > 0)
                    PlayerDataController.Instance.RemoveHeat(1);
                _currentHeatLevel = 0;

                StartCoroutine(BustSequence());
            }
        }

        private System.Collections.IEnumerator BustSequence()
        {
            Debug.Log("[PoliceManager] PLAYER BUSTED BY POLICE!");

            Animator playerAnimator = _playerHealth.GetComponentInChildren<Animator>();
            if (playerAnimator != null)
            {
                float timeout = 3f;
                float elapsed = 0f;
                int knockoutLayer = -1;

                while (elapsed < timeout)
                {
                    for (int i = 0; i < playerAnimator.layerCount; i++)
                    {
                        if (playerAnimator.GetCurrentAnimatorStateInfo(i).IsName("Knockout"))
                        {
                            knockoutLayer = i;
                            break;
                        }
                    }

                    if (knockoutLayer >= 0) break;

                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (knockoutLayer >= 0)
                {
                    Debug.Log($"[PoliceManager] Knockout playing on layer {knockoutLayer}. Waiting for it to finish.");

                    while (playerAnimator.GetCurrentAnimatorStateInfo(knockoutLayer).IsName("Knockout") &&
                        playerAnimator.GetCurrentAnimatorStateInfo(knockoutLayer).normalizedTime < 0.95f)
                    {
                        yield return null;
                    }

                    Debug.Log("[PoliceManager] Knockout animation complete.");
                }
                else
                {
                    Debug.LogWarning("[PoliceManager] Knockout state never found. Using fallback.");
                    yield return new WaitForSeconds(2f);
                }
            }

            yield return new WaitForSeconds(1f);

            // Apply bust consequences from PlayerConfig (designer-tuned)
            PlayerConfig config = PlayerDataController.Instance.Config;
            if (config == null)
            {
                Debug.LogError("[PoliceManager] PlayerConfig not assigned to PlayerDataController. Cannot apply bust consequences.");
                _processingBust = false;
                yield break;
            }

            PlayerDataController.Instance.IncrementBustStreak();
            int streak = PlayerDataController.Instance.BustStreak;
            int consequenceIndex = config.GetBustConsequenceIndex(streak);

            PlayerDataController.Instance.LoseMoneyPercent(config.moneyPenalties[consequenceIndex]);
            PlayerDataController.Instance.LoseReputation(config.repPenalties[consequenceIndex]);

            float layLowDuration = config.layLowDurations[consequenceIndex];
            if (PlayerDataController.Instance.IsLayingLow)
                PlayerDataController.Instance.ExtendLayLow(layLowDuration);
            else
                PlayerDataController.Instance.StartLayLow(layLowDuration);

            _bustStreakDecayTimer = 0f;

            RespawnService.RespawnPlayer();

            _processingBust = false;

            Debug.Log($"[PoliceManager] Bust complete. Streak: {streak}, Lay-low: {layLowDuration}s");
        }
    }
}