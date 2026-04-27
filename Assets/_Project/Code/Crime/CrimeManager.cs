using UnityEngine;
using System;
using MCGame.Core;
using MCGame.Gameplay.Player;

namespace MCGame.Gameplay.Crime
{
    /// <summary>
    /// Central manager for the crime detection pipeline.
    /// </summary>
    public class CrimeManager : Singleton<CrimeManager>
    {
        [Header("Witness Detection")]
        [Tooltip("Maximum distance (meters) an NPC can witness a crime from")]
        [Range(5f, 50f)]
        [SerializeField] private float witnessDetectionRadius = 20f;

        public event Action<CrimeType, Vector3> OnCrimeReported;
        public event Action<CrimeType> OnCrimeUnwitnessed;

        public float WitnessDetectionRadius => witnessDetectionRadius;
        public LayerMask WitnessLayerMask => Layers.NPCMask;
        public LayerMask ObstructionLayerMask => Layers.LineOfSightObstructionMask;

        private void Start()
        {
            if (PlayerDataController.Instance == null)
            {
                Debug.LogError("[CrimeManager] PlayerDataController not found in scene. Crime system will not function.", this);
            }
        }

        public void HandleCrimeReported(CrimeType crimeType, Vector3 crimePosition)
        {
            if (crimeType == null) return;
            if (PlayerDataController.Instance == null) return;

            if (PlayerDataController.Instance.IsLayingLow)
            {
                PlayerDataController.Instance.ExtendLayLow(PlayerDataController.Instance.LayLowTimeRemaining);
                Debug.Log($"[CrimeManager] Crime during lay-low! Timer doubled.");
            }

            PlayerDataController.Instance.AddHeat(crimeType.baseHeatAmount);

            Debug.Log($"[CrimeManager] Crime '{crimeType.crimeName}' witnessed. " +
                    $"+{crimeType.baseHeatAmount} heat. Current: {PlayerDataController.Instance.HeatLevel}");

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