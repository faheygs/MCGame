using UnityEngine;

/// <summary>
/// Static utility for witness detection. Does ONE job:
/// check if a crime was witnessed, then report result to CrimeManager.
/// 
/// Does NOT touch PlayerStats, heat, corruption, or any other state.
/// All config is read from CrimeManager (single source of truth).
/// </summary>
public static class CrimeReporter
{
    /// <summary>
    /// Report a crime. Checks for witnesses, then notifies CrimeManager.
    /// </summary>
    /// <param name="crimeType">The type of crime committed</param>
    /// <param name="crimePosition">World position where crime occurred</param>
    /// <param name="victim">The victim (excluded from witness check)</param>
    public static void ReportCrime(CrimeType crimeType, Vector3 crimePosition, GameObject victim = null)
    {
        if (crimeType == null)
        {
            Debug.LogWarning("[CrimeReporter] ReportCrime called with null CrimeType. Ignoring.");
            return;
        }

        if (CrimeManager.Instance == null)
        {
            Debug.LogError("[CrimeReporter] CrimeManager not found in scene. Cannot report crime.");
            return;
        }

        // Check for witnesses
        bool wasWitnessed = CheckForWitnesses(crimePosition, victim);

        if (wasWitnessed)
        {
            CrimeManager.Instance.HandleCrimeReported(crimeType, crimePosition);
        }
        else
        {
            CrimeManager.Instance.HandleCrimeUnwitnessed(crimeType);
        }
    }

    /// <summary>
    /// Check if any NPCs witnessed the crime. Reads all config from CrimeManager.
    /// </summary>
    private static bool CheckForWitnesses(Vector3 crimePosition, GameObject victim)
    {
        float radius = CrimeManager.Instance.WitnessDetectionRadius;
        LayerMask witnessLayer = CrimeManager.Instance.WitnessLayerMask;
        LayerMask obstructionLayer = CrimeManager.Instance.ObstructionLayerMask;

        // Find all NPCs within detection radius
        Collider[] nearbyNPCs = Physics.OverlapSphere(crimePosition, radius, witnessLayer);

        if (nearbyNPCs.Length == 0)
        {
            return false;
        }

        // Check line of sight for each NPC
        int witnessCount = 0;
        foreach (Collider npcCollider in nearbyNPCs)
        {
            // Skip the victim
            if (victim != null && npcCollider.gameObject == victim)
            {
                continue;
            }

            Vector3 npcPosition = npcCollider.bounds.center;
            Vector3 directionToCrime = crimePosition - npcPosition;
            float distanceToCrime = directionToCrime.magnitude;

            // Raycast: if blocked by geometry, not a witness
            if (Physics.Raycast(npcPosition, directionToCrime.normalized, distanceToCrime, obstructionLayer))
            {
                continue;
            }

            // Clear LOS — confirmed witness
            witnessCount++;
            Debug.Log($"[CrimeReporter] NPC '{npcCollider.gameObject.name}' witnessed crime.");
        }

        if (witnessCount > 0)
        {
            Debug.Log($"[CrimeReporter] {witnessCount} witness(es) confirmed.");
        }

        return witnessCount > 0;
    }
}