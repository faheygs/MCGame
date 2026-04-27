using System.Collections;
using UnityEngine;
using MCGame.Input;

namespace MCGame.Core
{
    /// <summary>
    /// Single owner of the game's initialization sequence.
    /// Runs after GameManager (which is auto-discovered as a PersistentSingleton),
    /// before any other gameplay script.
    ///
    /// Phases (sequential):
    ///   1. Verify foundation (GameManager exists)
    ///   2. Initialize World managers
    ///   3. Initialize Gameplay managers
    ///   4. Initialize UI managers
    ///   5. Transition GameManager: Boot → Gameplay
    ///
    /// This is the "main()" of the game's runtime initialization.
    /// When you find yourself adding "wait until X is ready" coroutines or
    /// defensive null checks for cross-manager init, that's the signal to
    /// add explicit ordering here instead.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public class Bootstrapper : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("If true, prints the init phase sequence to the console. Useful for diagnostics.")]
        [SerializeField] private bool verbose = true;

        [Tooltip("If true, the bootstrap runs automatically on Start. Disable for tests/demos.")]
        [SerializeField] private bool autoBootstrap = true;

        [Header("References (optional, can be auto-found)")]
        [Tooltip("Reference to the InputReader ScriptableObject. Auto-found if left empty.")]
        [SerializeField] private InputReader inputReader;

        private bool _hasBootstrapped;

        private IEnumerator Start()
        {
            if (!autoBootstrap) yield break;

            // Wait one frame so all Awake/OnEnable have completed across the scene.
            // This includes the layout pass for any Canvas — important for UI managers
            // that depend on RectTransform sizes being valid.
            yield return null;

            yield return Bootstrap();
        }

        /// <summary>
        /// Public entry point for explicit boot (used by tests, scene transitions).
        /// Idempotent — safe to call multiple times; subsequent calls are no-ops.
        /// </summary>
        public IEnumerator Bootstrap()
        {
            if (_hasBootstrapped)
            {
                Log("Bootstrap requested but already complete. Skipping.");
                yield break;
            }

            Log("=== Bootstrap START ===");

            // ----- Phase 1: Foundation -----
            yield return Phase1_VerifyFoundation();

            // ----- Phase 2: World -----
            yield return Phase2_InitializeWorld();

            // ----- Phase 3: Gameplay -----
            yield return Phase3_InitializeGameplay();

            // ----- Phase 4: UI -----
            yield return Phase4_InitializeUI();

            // ----- Phase 5: Finalize -----
            yield return Phase5_Finalize();

            _hasBootstrapped = true;
            Log("=== Bootstrap COMPLETE ===");
        }

        // -----------------------------------------------------------------
        // Phase 1: Foundation
        // -----------------------------------------------------------------

        private IEnumerator Phase1_VerifyFoundation()
        {
            Log("Phase 1: Verifying foundation...");

            if (GameManager.Instance == null)
            {
                LogError("GameManager.Instance is null. The BOOT group must contain a GameManager. Bootstrap aborting.");
                yield break;
            }

            if (GameManager.Instance.CurrentState != GameState.Boot)
            {
                Log($"GameManager already past Boot (currently {GameManager.Instance.CurrentState}). Continuing anyway.");
            }

            yield return null;
        }

        // -----------------------------------------------------------------
        // Phase 2: World
        // -----------------------------------------------------------------

        private IEnumerator Phase2_InitializeWorld()
        {
            Log("Phase 2: Initializing World...");

            // RoadNetwork and DistrictManager auto-discover their content in their own
            // Start() methods. No explicit init call needed. We just verify they exist.
            // When we eventually convert them to bootstrap-driven init, the call sites
            // go here.

            yield return null;
        }

        // -----------------------------------------------------------------
        // Phase 3: Gameplay
        // -----------------------------------------------------------------

        private IEnumerator Phase3_InitializeGameplay()
        {
            Log("Phase 3: Initializing Gameplay...");

            // Most gameplay managers self-init via their base Singleton<T>.OnAwake().
            // Explicit init calls go here when we convert them.

            yield return null;
        }

        // -----------------------------------------------------------------
        // Phase 4: UI
        // -----------------------------------------------------------------

        private IEnumerator Phase4_InitializeUI()
        {
            Log("Phase 4: Initializing UI...");

            // SMOKE TEST CONVERSION: MinimapMarkerManager
            // Previously had fragile coroutine init that polled for canvas readiness.
            // Now we wait for canvas layout, then call its explicit Initialize().
            yield return InitializeMinimapMarkerManager();

            yield return null;
        }

        private IEnumerator InitializeMinimapMarkerManager()
        {
            // Wait one frame for the Canvas to complete its layout pass.
            // Canvas RectTransforms are not valid until the first layout completes.
            yield return null;

            Log("Sending UI phase ready signal to subscribers...");

            // The actual Initialize call happens via MinimapMarkerManager subscribing to
            // the OnUIPhaseReady event. This keeps Core ignorant of Gameplay.
            OnUIPhaseReady?.Invoke();
        }

        // -----------------------------------------------------------------
        // Phase 5: Finalize
        // -----------------------------------------------------------------

        private IEnumerator Phase5_Finalize()
        {
            Log("Phase 5: Finalizing...");

            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Boot)
            {
                GameManager.Instance.ReturnToGameplay();
                Log("GameManager transitioned Boot → Gameplay.");
            }

            yield return null;
        }

        // -----------------------------------------------------------------
        // Inter-assembly init signals
        // -----------------------------------------------------------------

        /// <summary>
        /// Fires when Phase 4 (UI) is ready to initialize. UI managers in MCGame.Gameplay
        /// subscribe to this to perform their explicit init. This pattern lets Bootstrapper
        /// drive UI init without Core depending on Gameplay.
        /// </summary>
        public static event System.Action OnUIPhaseReady;

        // -----------------------------------------------------------------
        // Logging
        // -----------------------------------------------------------------

        private void Log(string message)
        {
            if (!verbose) return;
            Debug.Log($"[Bootstrapper] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[Bootstrapper] {message}");
        }
    }
}