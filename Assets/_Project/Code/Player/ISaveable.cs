using System.Collections.Generic;

namespace MCGame.Gameplay.Player
{
    /// <summary>
    /// Contract for any data class that can be serialized to/from a save file.
    /// SaveSystem (when built) will read/write via this interface, never via reflection.
    ///
    /// Implementations should:
    ///   - Use stable string keys (these are the save file format — keep them stable forever)
    ///   - Handle missing keys gracefully (forward compatibility — old saves loading new code)
    ///   - Not depend on Unity types in their save data (use plain primitives + collections)
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// Serialize this object's runtime state into a dictionary.
        /// Keys are stable identifiers used by the save file format.
        /// </summary>
        Dictionary<string, object> SaveToDictionary();

        /// <summary>
        /// Restore this object's runtime state from a dictionary.
        /// Implementations should tolerate missing keys (use defaults) and handle
        /// type mismatches without crashing (skip the field, log a warning).
        /// </summary>
        void LoadFromDictionary(Dictionary<string, object> data);
    }
}