using UnityEngine;

namespace MCGame.Gameplay.Crime
{
    /// <summary>
    /// Defines a criminal action in the game world.
    /// </summary>
    [CreateAssetMenu(fileName = "CrimeType_", menuName = "MCGame/Crime Type")]
    public class CrimeType : ScriptableObject
    {
        [Header("Crime Identity")]
        [Tooltip("Human-readable crime name")]
        public string crimeName = "Unnamed Crime";

        [Header("Heat Generation")]
        [Tooltip("Base amount of personal heat this crime generates when witnessed")]
        [Range(0, 5)]
        public int baseHeatAmount = 1;

        [Header("Severity Classification")]
        [Tooltip("Severity tier used by corruption system:\n1 = Minor\n2 = Moderate\n3 = Serious\n4 = Extreme")]
        [Range(1, 4)]
        public int severityTier = 1;

        [Header("Description")]
        [TextArea(2, 4)]
        [Tooltip("Internal note describing when this crime is triggered")]
        public string description = "";
    }
}