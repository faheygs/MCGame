using UnityEngine;
using System;
using MCGame.Core;

namespace MCGame.Gameplay.Crime
{
    /// <summary>
    /// Central manager for the crime detection pipeline.
    /// </summary>
    public class CrimeManager : Singleton<CrimeManager>
    {
        [Header("Data")]
        [SerializeField] private PlayerStats playerStats;

        [Header("Witness Detection")]
        [Tooltip("Maximum distance (meters) an NPC can witness a crime from")]
        [Range(5f, 50f)]
        [SerializeField] private float witnessDetectionRadius = 20f;

        [Tooltip("Which layers count as potential witnesses")]
        [SerializeField] private LayerMask witnessLayerMask = 1 << 13;

        [Tooltip("Which layers block line of sight")]
        [SerializeField] private LayerMask obstructionLayerMask = 1 << 0;

        public event Action<CrimeType, Vector3> OnCrimeReported;
        public event Action<CrimeType> OnCrimeUnwitnessed;

        public float WitnessDetectionRadius => witnessDetectionRadius;
        public LayerMask WitnessLayerMask => witnessLayerMask;
        public LayerMask ObstructionLayerMask => obstructionLayerMask;
        public PlayerStats PlayerStats => playerStats;

        private void Start()
        {
            if (playerStats == null)
            {
                Debug.LogError("[CrimeManager] PlayerStats not assigned! Crime system will not function.", this);
            }
        }

        public void HandleCrimeReported(CrimeType crimeType, Vector3 crimePosition)
        {
            if (crimeType == null || playerStats == null) return;

            if (playerStats.IsLayingLow)
            {
                playerStats.ExtendLayLow(playerStats.LayLowTimeRemaining);
                Debug.Log($"[CrimeManager] Crime during lay-low! Timer doubled.");
            }

            playerStats.AddHeat(crimeType.baseHeatAmount);

            Debug.Log($"[CrimeManager] Crime '{crimeType.crimeName}' witnessed. " +
                    $"+{crimeType.baseHeatAmount} heat. Current: {playerStats.HeatLevel}");

            OnCrimeReported?.Invoke(crimeType, crimePosition);
        }

        public void HandleCrimeUnwitnessed(CrimeType crimeType)
        {
            if (crimeType == null) return;

            Debug.Log($"[CrimeManager] Crime '{crimeType.crimeName}' - NO WITNESSES. Clean crime.");

            OnCrimeUnwitnessed?.Invoke(crimeType);
        }

        private void OnValidate()
        {
            if (Application.isPlaying && Instance == this)
            {
                Debug.Log($"[CrimeManager] Config updated: Witness radius={witnessDetectionRadius}m");
            }
        }
    }
}