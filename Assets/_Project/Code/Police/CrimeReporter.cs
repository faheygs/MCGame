using UnityEngine;

namespace MCGame.Police
{
    /// <summary>
    /// Static utility for reporting crimes and checking for witnesses.
    /// When a crime occurs, checks for NPCs with line of sight to the crime location.
    /// If witnesses found, reports the crime to PlayerStats (generates heat).
    /// </summary>
    public static class CrimeReporter
    {
        // Configurable witness detection parameters
        private static float _witnessDetectionRadius = 20f; // meters
        private static LayerMask _witnessLayerMask = 1 << 13; // NPC layer (layer 13)
        private static LayerMask _obstructionLayerMask = 1 << 0; // Default layer (world geometry)

        /// <summary>
        /// Report a crime at the specified world position.
        /// Checks for witnesses and adds heat to PlayerStats if crime was witnessed.
        /// </summary>
        /// <param name="crimeType">The type of crime committed</param>
        /// <param name="crimePosition">World position where crime occurred</param>
        /// <param name="victim">The GameObject that was the victim of the crime (excluded from witness check)</param>
        public static void ReportCrime(CrimeType crimeType, Vector3 crimePosition, GameObject victim = null)
        {
            if (crimeType == null)
            {
                Debug.LogWarning("[CrimeReporter] ReportCrime called with null CrimeType. Ignoring.");
                return;
            }

            // Check for witnesses (excluding the victim)
            bool wasWitnessed = CheckForWitnesses(crimePosition, victim);

            if (wasWitnessed)
            {
                // Crime was witnessed - add heat
                // Find PlayerStats through HeatCooldown component on Player
                HeatCooldown heatCooldown = Object.FindAnyObjectByType<HeatCooldown>();
                if (heatCooldown != null)
                {
                    PlayerStats playerStats = heatCooldown.GetPlayerStats();
                    if (playerStats != null)
                    {
                        playerStats.AddHeat(crimeType.baseHeatAmount);
                        Debug.Log($"[CrimeReporter] Crime '{crimeType.crimeName}' witnessed at {crimePosition}. Adding {crimeType.baseHeatAmount} heat.");
                    }
                    else
                    {
                        Debug.LogError("[CrimeReporter] HeatCooldown found but PlayerStats reference is null.");
                    }
                }
                else
                {
                    Debug.LogError("[CrimeReporter] HeatCooldown component not found in scene. Cannot add heat.");
                }
            }
            else
            {
                // No witnesses - clean crime
                Debug.Log($"[CrimeReporter] Crime '{crimeType.crimeName}' at {crimePosition} - NO WITNESSES. No heat added.");
            }
        }

        /// <summary>
        /// Check if any NPCs witnessed the crime at the given position.
        /// An NPC is a witness if they are within detection radius AND have unobstructed line of sight.
        /// </summary>
        /// <param name="crimePosition">World position of the crime</param>
        /// <param name="victim">The victim GameObject to exclude from witness check</param>
        /// <returns>True if at least one witness found, false otherwise</returns>
        private static bool CheckForWitnesses(Vector3 crimePosition, GameObject victim)
        {
            // Find all NPCs within witness detection radius
            Collider[] nearbyNPCs = Physics.OverlapSphere(crimePosition, _witnessDetectionRadius, _witnessLayerMask);

            if (nearbyNPCs.Length == 0)
            {
                // No NPCs in range - no witnesses possible
                return false;
            }

            Debug.Log($"[CrimeReporter] Found {nearbyNPCs.Length} potential witnesses within {_witnessDetectionRadius}m of crime.");

            // Check line of sight for each nearby NPC
            int witnessCount = 0;
            foreach (Collider npcCollider in nearbyNPCs)
            {
                // Skip the victim - they can't witness their own assault
                if (victim != null && npcCollider.gameObject == victim)
                {
                    Debug.Log($"[CrimeReporter] Skipping victim '{npcCollider.gameObject.name}' from witness check.");
                    continue;
                }

                // Get NPC position (use collider center as eye position for V1)
                Vector3 npcPosition = npcCollider.bounds.center;

                // Raycast from NPC to crime position
                Vector3 directionToCrime = crimePosition - npcPosition;
                float distanceToCrime = directionToCrime.magnitude;

                // If raycast hits world geometry before reaching crime position, LOS is blocked
                if (Physics.Raycast(npcPosition, directionToCrime.normalized, distanceToCrime, _obstructionLayerMask))
                {
                    // Line of sight blocked - not a witness
                    Debug.Log($"[CrimeReporter] NPC '{npcCollider.gameObject.name}' - LOS blocked by geometry. Not a witness.");
                    continue;
                }

                // Clear line of sight - this NPC is a witness
                witnessCount++;
                Debug.Log($"[CrimeReporter] NPC '{npcCollider.gameObject.name}' - CLEAR LOS. Witness confirmed.");
            }

            return witnessCount > 0;
        }

        /// <summary>
        /// Configure witness detection parameters at runtime (for testing/tuning).
        /// </summary>
        public static void ConfigureWitnessDetection(float radius, LayerMask witnessLayer, LayerMask obstructionLayer)
        {
            _witnessDetectionRadius = radius;
            _witnessLayerMask = witnessLayer;
            _obstructionLayerMask = obstructionLayer;
            Debug.Log($"[CrimeReporter] Witness detection configured: Radius={radius}m, WitnessLayer={witnessLayer.value}, ObstructionLayer={obstructionLayer.value}");
        }
    }
}