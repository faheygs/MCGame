using UnityEngine;

namespace MCGame.Core
{
    /// <summary>
    /// Base class for cross-scene singleton MonoBehaviours.
    /// Identical to Singleton&lt;T&gt; except the GameObject survives scene loads
    /// via DontDestroyOnLoad.
    ///
    /// Use for managers that own state across scenes:
    ///   - GameManager (game-wide state machine)
    ///   - SaveSystem (when built)
    ///   - AudioManager (when built)
    ///
    /// Do NOT use for managers scoped to a single gameplay scene
    ///   - MissionManager, HUDManager, PoliceManager, etc. should remain Singleton&lt;T&gt;.
    /// </summary>
    public abstract class PersistentSingleton<T> : MonoBehaviour where T : PersistentSingleton<T>
    {
        public static T Instance { get; private set; }

        protected void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    $"[{typeof(T).Name}] Duplicate instance on '{name}'. Destroying duplicate GameObject.",
                    this
                );
                Destroy(gameObject);
                return;
            }

            Instance = (T)this;
            DontDestroyOnLoad(gameObject);
            OnAwake();
        }

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
        /// </summary>
        protected virtual void OnAwake() { }

        /// <summary>
        /// Override this for manager-specific destroy work.
        /// </summary>
        protected virtual void OnDestroyed() { }
    }
}