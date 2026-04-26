using UnityEngine;
using System;
using System.Collections.Generic;
using MCGame.Core;
using MCGame.World;
using MCGame.Combat;

/// <summary>
/// Manages law enforcement response to criminal activity.
/// Subscribes to CrimeManager events and PlayerStats heat changes.
/// Owns corruption level, police spawning/despawning, and active police tracking.
/// 
/// Follows the same singleton pattern as CrimeManager, InteractionManager,
/// WaypointManager, DistrictManager.
/// </summary>
public class PoliceManager : MonoBehaviour
{
    // --- Singleton ---
    public static PoliceManager Instance { get; private set; }

    [Header("Data")]
    [SerializeField] private PlayerStats playerStats;

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

    [Header("Bust Consequences")]
    [Tooltip("Lay-low duration in seconds for each bust level")]
    [SerializeField] private float[] layLowDurations = { 120f, 300f, 600f }; // 2min, 5min, 10min

    [Tooltip("Money loss percentage for each bust level")]
    [SerializeField] private float[] moneyPenalties = { 0.15f, 0.30f, 0.50f };

    [Tooltip("Rep loss for each bust level")]
    [SerializeField] private int[] repPenalties = { 50, 150, 300 };

    [Tooltip("Seconds of clean play before bust streak decays by 1")]
    [SerializeField] private float bustStreakDecayTime = 1200f; // 20 minutes

    // =========================================================================
    // EVENTS
    // =========================================================================

    /// <summary>Fires when police spawn or despawn. Passes current active count.</summary>
    public event Action<int> OnPoliceCountChanged;

    /// <summary>Fires when corruption suppresses a police response.</summary>
    public event Action<CrimeType> OnCrimeSuppressedByCorruption;

    // =========================================================================
    // RUNTIME STATE
    // =========================================================================

    private List<GameObject> _activePolice = new List<GameObject>();
    private Dictionary<GameObject, int> _policeMarkerIds = new Dictionary<GameObject, int>();
    private int _currentHeatLevel;
    private float _reinforcementTimer;
    private float _bustStreakDecayTimer;
    private Health _playerHealth;
    private bool _processingBust;

    // =========================================================================
    // PUBLIC ACCESSORS
    // =========================================================================

    public int CorruptionLevel => corruptionLevel;
    public int ActivePoliceCount => _activePolice.Count;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PoliceManager] Duplicate instance detected. Destroying this one.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        if (CrimeManager.Instance != null)
        {
            CrimeManager.Instance.OnCrimeReported += HandleCrimeReported;
        }

        if (playerStats != null)
        {
            playerStats.OnHeatChanged += HandleHeatChanged;
        }
    }

    private void OnDisable()
    {
        if (CrimeManager.Instance != null)
        {
            CrimeManager.Instance.OnCrimeReported -= HandleCrimeReported;
        }

        if (playerStats != null)
        {
            playerStats.OnHeatChanged -= HandleHeatChanged;
        }

        if (_playerHealth != null)
        {
            _playerHealth.OnDied -= HandlePlayerDied;
        }
    }

    private void Start()
    {
        // CrimeManager may not have existed during OnEnable (script execution order).
        if (CrimeManager.Instance != null)
        {
            CrimeManager.Instance.OnCrimeReported -= HandleCrimeReported;
            CrimeManager.Instance.OnCrimeReported += HandleCrimeReported;
        }
        else
        {
            Debug.LogError("[PoliceManager] CrimeManager not found. Police will not respond to crimes.", this);
        }

        if (playerStats == null)
        {
            Debug.LogError("[PoliceManager] PlayerStats not assigned.", this);
        }

        // Subscribe to player death for bust detection
        PlayerController playerController = FindAnyObjectByType<PlayerController>();
        if (playerController != null)
        {
            _playerHealth = playerController.GetComponent<Health>();
            if (_playerHealth != null)
            {
                _playerHealth.OnDied += HandlePlayerDied;
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (_currentHeatLevel > 0)
        {
            HandleReinforcements();
        }

        CleanupDestroyedPolice();
        HandleDistanceDespawn();

        // Tick lay-low timer
        if (playerStats != null && playerStats.IsLayingLow)
        {
            playerStats.UpdateLayLowTimer(Time.deltaTime);
        }

        // Tick bust streak decay
        if (playerStats != null && playerStats.BustStreak > 0 && !playerStats.IsLayingLow)
        {
            _bustStreakDecayTimer += Time.deltaTime;
            if (_bustStreakDecayTimer >= bustStreakDecayTime)
            {
                _bustStreakDecayTimer = 0f;
                playerStats.DecrementBustStreak();
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

        // Don't react to heat changes during bust processing
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

    /// <summary>
    /// Per design doc Section 3.1:
    /// Heat 1: 1-2 deputies, Heat 2: 2-3 deputies
    /// Heat 3-5: future tiers
    /// </summary>
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

                // Register minimap marker
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
        {
            return bestSpawn.Position;
        }

        // Fallback: spawn at offset from player if no valid spawn points
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
                // Remove minimap marker BEFORE anything else
                RemovePoliceMarker(_activePolice[i]);

                // Tell police to walk away instead of instant destroy
                PoliceNPC policeAI = _activePolice[i].GetComponent<PoliceNPC>();
                if (policeAI != null)
                {
                    policeAI.Disengage();
                }
                else
                {
                    Destroy(_activePolice[i]);
                }
            }
        }

        _activePolice.Clear();
        _policeMarkerIds.Clear();
        OnPoliceCountChanged?.Invoke(0);
    }

    /// <summary>
    /// Remove a specific police NPC from tracking.
    /// Called by PoliceNPC when it is knocked out (Phase 3).
    /// </summary>
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
            {
                MinimapMarkerManager.Instance.UnregisterMissionMarker(markerId);
            }
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
        PlayerController player = FindAnyObjectByType<PlayerController>();
        return player != null ? player.transform : null;
    }

    private void CleanupDestroyedPolice()
    {
        for (int i = _activePolice.Count - 1; i >= 0; i--)
        {
            if (_activePolice[i] == null)
            {
                _activePolice.RemoveAt(i);
            }
        }
    }

    // =========================================================================
    // BUST SYSTEM
    // =========================================================================

    private void HandlePlayerDied()
    {
        if (_playerHealth == null) return;

        GameObject source = _playerHealth.LastDamageSource;
        if (source != null && source.GetComponent<PoliceNPC>() != null)
        {
            _processingBust = true;

            // IMMEDIATELY destroy all police — not disengage, DESTROY
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

            // Clear heat IMMEDIATELY — nothing should spawn
            while (playerStats.HeatLevel > 0)
            {
                playerStats.RemoveHeat(1);
            }
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
            // Wait until Knockout state is actually playing on any layer
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

            // Now wait for the knockout animation to finish
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

        // Hold on ground briefly
        yield return new WaitForSeconds(1f);

        // Apply consequences
        playerStats.IncrementBustStreak();
        int streak = playerStats.BustStreak;
        int consequenceIndex = Mathf.Min(streak - 1, 2);

        playerStats.LoseMoney(moneyPenalties[consequenceIndex]);
        playerStats.LoseReputation(repPenalties[consequenceIndex]);

        float layLowDuration = layLowDurations[consequenceIndex];
        if (playerStats.IsLayingLow)
        {
            playerStats.ExtendLayLow(layLowDuration);
        }
        else
        {
            playerStats.StartLayLow(layLowDuration);
        }

        _bustStreakDecayTimer = 0f;

        RespawnPlayerAtCompound();

        _processingBust = false;

        Debug.Log($"[PoliceManager] Bust complete. Streak: {streak}, Lay-low: {layLowDuration}s");
    }

    private void RespawnPlayerAtCompound()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;

        // Find player spawn point
        SpawnPoint[] spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsInactive.Exclude);
        Vector3 respawnPos = Vector3.zero;
        bool foundSpawn = false;

        foreach (SpawnPoint sp in spawnPoints)
        {
            if (sp.Type == SpawnPoint.SpawnType.PlayerStart)
            {
                respawnPos = sp.Position;
                foundSpawn = true;
                break;
            }
        }

        if (!foundSpawn)
        {
            Debug.LogWarning("[PoliceManager] No PlayerStart spawn point found. Respawning at origin.");
        }

        // Disable CharacterController to allow position change
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Move player
        player.transform.position = respawnPos;

        // Re-enable CharacterController
        if (cc != null) cc.enabled = true;

        // Reset player health
        Health playerHealth = player.GetComponent<Health>();
        if (playerHealth != null)
        {
            playerHealth.Reset();
        }

        // Reset animator to idle — clear knockout state
        Animator playerAnimator = player.GetComponentInChildren<Animator>();
        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger("Knockout");
            playerAnimator.ResetTrigger("Hit");

            // Reset ALL layers to their default state
            for (int i = 0; i < playerAnimator.layerCount; i++)
            {
                playerAnimator.Play("Empty", i, 0f);
            }

            // Force base layer to Idle
            playerAnimator.Play("Idle", 0, 0f);
        }

        // Re-enable player controller
        player.enabled = true;

        // Reset combat — stop all coroutines and clear state
        PlayerCombat combat = player.GetComponent<PlayerCombat>();
        if (combat != null)
        {
            combat.StopAllCoroutines();
            combat.enabled = false;
            combat.enabled = true;
        }

        Debug.Log($"[PoliceManager] Player respawned at {respawnPos}");
    }
}