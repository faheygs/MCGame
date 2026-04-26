namespace MCGame.Gameplay.Player
{
    /// <summary>
    /// Player's rank in the motorcycle club. Ordered from lowest to highest.
    /// Display names (with spaces, like "Patched Member") come from RankDefinition.displayName,
    /// not from this enum. Code uses this enum exclusively — never compare rank strings.
    /// </summary>
    public enum ClubRank
    {
        Prospect = 0,
        Hangaround = 1,
        PatchedMember = 2,
        Enforcer = 3,
        RoadCaptain = 4,
        VicePresident = 5,
        President = 6
    }
}