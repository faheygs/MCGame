using UnityEngine;

namespace MCGame.Core
{
    /// <summary>
    /// Base class for scene-scoped singleton MonoBehaviours.
    /// Inherit from this to get a static Instance property and consistent
    /// duplicate-handling behavior with zero boilerplate.
    ///
    /// Usage:
    ///     public class MyManager : Singleton&lt;MyManager&gt;
    ///     {
    ///         protected override void OnAwake()
    ///         {
    ///             // Optional: manager-specific Awake work
    ///         }
    ///     }
    ///
    /// On scene load:
    ///   - The first instance wins and becomes Instance.
    ///   - Any duplicates added later are silently destroyed (component only,
    ///     not the GameObject — siblings on that GameObject stay alive).
    ///
    /// On scene unload / object destroy:
    ///   - Instance is cleared if and only if the destroyed object was the
    ///     active instance. Stops dangling references after scene transitions.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        /// <summary>
        /// The single live instance of this manager, or null if none exists yet.
        /// </summary>
        public static T Instance { get; private set; }

        /// <summary>
        /// Sealed Awake — handles singleton plumbing. Override OnAwake() instead.
        /// </summary>
        protected void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    $"[{typeof(T).Name}] Duplicate instance on '{name}'. Destroying duplicate component.",
                    this
                );
                Destroy(this);
                return;
            }

            Instance = (T)this;
            OnAwake();
        }

        /// <summary>
        /// Sealed OnDestroy — clears the static Instance when the active
        /// instance is destroyed. Override OnDestroyed() instead.
        /// </summary>
        protected void OnDestroy()
        {
            if (Instance == this)
            {
                OnDestroyed();
                Instance = null;
            }
        }

        /// <summary>
        /// Override this for manager-specific Awake work.
        /// Called only on the surviving (active) instance, after Instance is set.
        /// </summary>
        protected virtual void OnAwake() { }

        /// <summary>
        /// Override this for manager-specific destroy work.
        /// Called only on the active instance, before Instance is cleared.
        /// </summary>
        protected virtual void OnDestroyed() { }
    }
}