// Defines how a mission objective is completed.
// GoToLocation: player walks into the radius and it auto-completes.
// Interact: player must press E on the objective to complete it.
// DefeatTarget: player must defeat all enemies at the objective location.

public enum ObjectiveType
{
    GoToLocation,
    Interact,
    DefeatTarget
}