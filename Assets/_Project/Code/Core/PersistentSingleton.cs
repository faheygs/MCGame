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
    ///
    /// Note on hierarchy: Unity requires DontDestroyOnLoad targets to be root-level
    /// GameObjects. To keep editor-time visual organization (e.g., living under a
    /// "--- BOOT ---" group), this base class automatically detaches the GameObject
    /// from its parent at runtime before calling DontDestroyOnLoad. This means:
    ///   - In edit mode: GameObject lives where you placed it (e.g., under BOOT group)
    ///   - In play mode: GameObject is automatically moved to scene root, then to
    ///                   the DontDestroyOnLoad scene.
    /// This is intentional and is the cleanest way to keep both organization and persistence.
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

            // Unity requires DontDestroyOnLoad targets to be root-level GameObjects.
            // If we have a parent (e.g., we live under "--- BOOT ---" for organization),
            // detach to root before persisting.
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }

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