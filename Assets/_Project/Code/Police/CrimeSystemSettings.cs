using UnityEngine;
using MCGame.Police;

/// <summary>
/// Configurable settings for the crime and witness detection system.
/// Place this component in the scene (on a manager or empty GameObject).
/// Values are applied on Start and can be tuned in the Inspector.
/// </summary>
public class CrimeSystemSettings : MonoBehaviour
{
    [Header("Witness Detection")]
    [Tooltip("Maximum distance (in meters) an NPC can be from a crime to witness it")]
    [Range(5f, 50f)]
    [SerializeField] private float witnessDetectionRadius = 20f;

    [Header("Layer Masks")]
    [Tooltip("Which layers count as potential witnesses (default: NPC layer 13)")]
    [SerializeField] private LayerMask witnessLayerMask = 1 << 13;

    [Tooltip("Which layers block line of sight (default: Default layer 0)")]
    [SerializeField] private LayerMask obstructionLayerMask = 1 << 0;

    private void Start()
    {
        // Apply settings to CrimeReporter on startup
        CrimeReporter.ConfigureWitnessDetection(witnessDetectionRadius, witnessLayerMask, obstructionLayerMask);
        
        Debug.Log($"[CrimeSystemSettings] Witness detection configured: Radius={witnessDetectionRadius}m");
    }

    // Allow runtime updates for testing/debugging
    private void OnValidate()
    {
        // Update in real-time when values change in Inspector (Editor only)
        if (Application.isPlaying)
        {
            CrimeReporter.ConfigureWitnessDetection(witnessDetectionRadius, witnessLayerMask, obstructionLayerMask);
        }
    }
}