using UnityEngine;

namespace MCGame.Gameplay.Crime
{
    /// <summary>
    /// Static utility for witness detection.
    /// </summary>
    public static class CrimeReporter
    {
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

        private static bool CheckForWitnesses(Vector3 crimePosition, GameObject victim)
        {
            float radius = CrimeManager.Instance.WitnessDetectionRadius;
            LayerMask witnessLayer = CrimeManager.Instance.WitnessLayerMask;
            LayerMask obstructionLayer = CrimeManager.Instance.ObstructionLayerMask;

            Collider[] nearbyNPCs = Physics.OverlapSphere(crimePosition, radius, witnessLayer);

            if (nearbyNPCs.Length == 0)
            {
                return false;
            }

            int witnessCount = 0;
            foreach (Collider npcCollider in nearbyNPCs)
            {
                if (victim != null && npcCollider.gameObject == victim)
                {
                    continue;
                }

                Vector3 npcPosition = npcCollider.bounds.center;
                Vector3 directionToCrime = crimePosition - npcPosition;
                float distanceToCrime = directionToCrime.magnitude;

                if (Physics.Raycast(npcPosition, directionToCrime.normalized, distanceToCrime, obstructionLayer))
                {
                    continue;
                }

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
}