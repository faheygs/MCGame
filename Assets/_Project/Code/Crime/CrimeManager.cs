using UnityEngine;
using System;
using MCGame.Core;

/// <summary>
/// Central manager for the crime detection pipeline.
/// Single source of truth for witness configuration and crime event processing.
/// 
/// Receives crime reports from CrimeReporter, applies heat to PlayerStats,
/// and fires events so other systems (police, reputation, HUD) can react.
/// 
/// Follows the same singleton pattern as InteractionManager, WaypointManager,
/// DistrictManager.
/// </summary>
public class CrimeManager : MonoBehaviour
{
    // --- Singleton ---
    public static CrimeManager Instance { get; private set; }

    [Header("Data")]
    [SerializeField] private PlayerStats playerStats;

    [Header("Witness Detection")]
    [Tooltip("Maximum distance (meters) an NPC can witness a crime from")]
    [Range(5f, 50f)]
    [SerializeField] private float witnessDetectionRadius = 20f;

    [Tooltip("Which layers count as potential witnesses")]
    [SerializeField] private LayerMask witnessLayerMask = 1 << 13; // NPC layer

    [Tooltip("Which layers block line of sight")]
    [SerializeField] private LayerMask obstructionLayerMask = 1 << 0; // Default layer

    // =========================================================================
    // EVENTS
    // =========================================================================

    /// <summary>
    /// Fires when a crime is witnessed and confirmed. Passes crime type and position.
    /// PoliceManager subscribes to decide whether to respond (based on corruption).
    /// Future: reputation system, civilian flee behavior, ATF surveillance.
    /// </summary>
    public event Action<CrimeType, Vector3> OnCrimeReported;

    /// <summary>
    /// Fires when a crime was committed but not witnessed.
    /// Future: stealth feedback, "clean crime" HUD notification.
    /// </summary>
    public event Action<CrimeType> OnCrimeUnwitnessed;

    // =========================================================================
    // PUBLIC ACCESSORS — CrimeReporter reads config from here
    // =========================================================================

    public float WitnessDetectionRadius => witnessDetectionRadius;
    public LayerMask WitnessLayerMask => witnessLayerMask;
    public LayerMask ObstructionLayerMask => obstructionLayerMask;
    public PlayerStats PlayerStats => playerStats;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[CrimeManager] Duplicate instance detected. Destroying this one.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        if (playerStats == null)
        {
            Debug.LogError("[CrimeManager] PlayerStats not assigned! Crime system will not function.", this);
        }
    }

    // =========================================================================
    // CRIME HANDLING — Called by CrimeReporter after witness check
    // =========================================================================

    /// <summary>
    /// Handle a witnessed crime. Applies heat and fires event.
    /// PoliceManager listens to OnCrimeReported and decides police response
    /// based on corruption level.
    /// </summary>
    public void HandleCrimeReported(CrimeType crimeType, Vector3 crimePosition)
    {
        if (crimeType == null || playerStats == null) return;

        // If laying low, extend the lay-low timer
        if (playerStats.IsLayingLow)
        {
            playerStats.ExtendLayLow(playerStats.LayLowTimeRemaining);
            Debug.Log($"[CrimeManager] Crime during lay-low! Timer doubled.");
        }

        // Apply heat
        playerStats.AddHeat(crimeType.baseHeatAmount);

        Debug.Log($"[CrimeManager] Crime '{crimeType.crimeName}' witnessed. " +
                $"+{crimeType.baseHeatAmount} heat. Current: {playerStats.HeatLevel}");

        OnCrimeReported?.Invoke(crimeType, crimePosition);
    }

    /// <summary>
    /// Handle an unwitnessed crime. No heat, fires event for feedback.
    /// </summary>
    public void HandleCrimeUnwitnessed(CrimeType crimeType)
    {
        if (crimeType == null) return;

        Debug.Log($"[CrimeManager] Crime '{crimeType.crimeName}' - NO WITNESSES. Clean crime.");

        OnCrimeUnwitnessed?.Invoke(crimeType);
    }

    // =========================================================================
    // DEBUG / EDITOR
    // =========================================================================

    private void OnValidate()
    {
        if (Application.isPlaying && Instance == this)
        {
            Debug.Log($"[CrimeManager] Config updated: Witness radius={witnessDetectionRadius}m");
        }
    }
}