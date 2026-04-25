using UnityEngine;
using System;
using System.Collections.Generic;

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
}