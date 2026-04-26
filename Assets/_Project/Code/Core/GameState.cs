namespace MCGame.Core
{
    /// <summary>
    /// All possible top-level game states. The GameManager owns transitions between these.
    ///
    /// Intentionally minimal — states are added when features need them, not preemptively.
    /// Future additions when their respective systems exist:
    ///   - Loading (when scene streaming is added)
    ///   - Cutscene (when cutscene system is added)
    ///   - MainMenu (when main menu is added)
    /// </summary>
    public enum GameState
    {
        /// <summary>
        /// Initial state before any gameplay manager is ready.
        /// Used by Bootstrapper (Phase A4) to gate startup.
        /// </summary>
        Boot,

        /// <summary>
        /// Normal play. Input is live, time flows, all systems active.
        /// </summary>
        Gameplay,

        /// <summary>
        /// Player paused the game. Time.timeScale = 0, gameplay input disabled.
        /// Pause menu UI (when built) takes over.
        /// </summary>
        Paused,

        /// <summary>
        /// Player has died permanently (not busted-and-respawn — that stays in Gameplay).
        /// Time still flows, gameplay input disabled, death screen takes over.
        /// </summary>
        GameOver
    }
}