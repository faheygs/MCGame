using UnityEngine;

namespace MCGame.Gameplay.AI
{
    /// <summary>
    /// Defines a simple two-point walk path for civilian NPCs.
    /// </summary>
    public class CivilianWalkPath : MonoBehaviour
    {
        [Header("Path Endpoints")]
        [Tooltip("First endpoint (civilian starts here)")]
        public Transform pointA;

        [Tooltip("Second endpoint (civilian walks to here)")]
        public Transform pointB;

        [Header("Path Settings")]
        [Tooltip("How long the civilian pauses at each endpoint (seconds)")]
        [Range(0f, 10f)]
        public float pauseDuration = 2f;

        private void OnDrawGizmos()
        {
            if (pointA != null && pointB != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(pointA.position, pointB.position);
                Gizmos.DrawWireSphere(pointA.position, 0.3f);
                Gizmos.DrawWireSphere(pointB.position, 0.3f);
            }
        }

        private void OnValidate()
        {
            if (pointA == null || pointB == null)
            {
                Debug.LogWarning($"[CivilianWalkPath] '{gameObject.name}' is missing endpoint references. Assign pointA and pointB.", this);
            }
        }
    }
}