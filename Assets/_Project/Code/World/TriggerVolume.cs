using System;
using UnityEngine;
using UnityEngine.Events;

namespace MCGame.World
{
    /// <summary>
    /// Generic trigger volume. Fires events when a specific tagged object enters or exits.
    /// Exposes both a C# event (for code subscription) and UnityEvents (for Inspector wiring).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TriggerVolume : MonoBehaviour
    {
        [Header("Filtering")]
        [Tooltip("Only objects with this tag will fire the trigger. Default: Player.")]
        [SerializeField] private string requiredTag = "Player";
        [Tooltip("If true, the trigger only fires once and then disables itself.")]
        [SerializeField] private bool fireOnce = false;

        [Header("Inspector Events")]
        public UnityEvent OnEnter;
        public UnityEvent OnExit;

        // C# events (for code subscription)
        public event Action<Collider> Entered;
        public event Action<Collider> Exited;

        private bool _hasFired = false;

        private void Awake()
        {
            // Ensure attached collider is set as trigger
            Collider col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                Debug.LogWarning($"[TriggerVolume] Collider on {name} was not marked as Trigger. Auto-fixing.");
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsValidEnter(other)) return;

            _hasFired = true;

            OnEnter?.Invoke();
            Entered?.Invoke(other);

            if (fireOnce)
                enabled = false;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsValidExit(other)) return;

            OnExit?.Invoke();
            Exited?.Invoke(other);
        }

        private bool IsValidEnter(Collider other)
        {
            if (fireOnce && _hasFired) return false;
            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return false;
            return true;
        }

        private bool IsValidExit(Collider other)
        {
            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return false;
            return true;
        }

        // --- Editor gizmo ---

        private void OnDrawGizmos()
        {
            Collider col = GetComponent<Collider>();
            if (col == null) return;

            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);

            if (col is BoxCollider box)
            {
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.matrix = oldMatrix;
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
            }
        }
    }
}