using UnityEngine;

/// <summary>
/// Contract for any object the player can interact with in the world.
/// Examples: mission givers, vehicles, doors, shops, pickups, NPCs.
///
/// Implementers register themselves with InteractionManager when the player is in range,
/// and unregister when out of range. The manager handles input routing and prompt display.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Higher priority wins over lower when multiple interactables are in range.
    /// Ties are broken by distance (nearest wins).
    ///
    /// Suggested values:
    ///   Pickup / low-stakes: 0
    ///   Mission giver: 5
    ///   Vehicle (on foot): 10
    ///   Vehicle (while mounted — dismount): 100
    ///   Cutscene / narrative: 1000
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// World position used for distance calculation.
    /// </summary>
    Vector3 GetPosition();

    /// <summary>
    /// Text shown on the interact prompt while this is the active interactable.
    /// Examples: "Press E to Mount", "Press E to Start Mission", "Press E to Pick Up".
    ///
    /// Implementers should NOT include the input button in the string —
    /// InteractionManager will prepend the correct "Press E" / "Press Y" prefix.
    /// Example implementation: return "Mount";
    /// </summary>
    string GetPromptText();

    /// <summary>
    /// True if this interactable should display a prompt to the player.
    /// Used for interactables that accept input but don't need visual prompts
    /// (e.g., a mounted vehicle still accepts Interact to dismount, but doesn't
    /// need to show a persistent "Dismount" prompt — players learn this by symmetry).
    /// </summary>
    bool ShouldShowPrompt();

    /// <summary>
    /// True if this interactable is currently usable.
    /// Use for dynamic conditions that can't be checked at registration time.
    /// Example: a mission giver may be in range but the player is in a cutscene.
    /// </summary>
    bool CanInteract();

    /// <summary>
    /// Called when the player presses Interact while this is the active interactable.
    /// This is where the interactable does its thing (start mission, mount bike, etc.).
    /// </summary>
    void OnInteract();
}