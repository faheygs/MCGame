namespace MCGame.Gameplay.Mission
{
    // Defines all possible states a mission can be in.
    public enum MissionState
    {
        Locked,      // Not yet available — story hasn't unlocked it
        Available,   // Player can see it and trigger it
        Active,      // Currently in progress
        Completed,   // Successfully finished
        Failed       // Failed — may be retried depending on mission settings
    }
}