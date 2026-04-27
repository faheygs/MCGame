using UnityEngine;

namespace MCGame.Core
{
    /// <summary>
    /// Pre-computed animator parameter hashes for performance and refactor safety.
    ///
    /// Why this exists:
    ///   - Animator.SetTrigger("Knockout") does a string hash every call (slow at scale)
    ///   - "Knockout" as a string isn't checked at compile time (rename = silent break)
    ///   - Centralized list = single point of update when an animator parameter renames
    ///
    /// Usage:
    ///   animator.SetTrigger(AnimatorParams.Knockout);
    ///   animator.SetFloat(AnimatorParams.Speed, value);
    ///   animator.ResetTrigger(AnimatorParams.Hit);
    ///
    /// When an animator parameter renames, update the corresponding string here.
    /// All consumers automatically use the new name.
    /// </summary>
    public static class AnimatorParams
    {
        // ----- Locomotion -----
        public static readonly int Speed = Animator.StringToHash("Speed");

        // ----- Combat: outgoing attacks -----
        public static readonly int LightPunch = Animator.StringToHash("LightPunch");
        public static readonly int LightKick = Animator.StringToHash("LightKick");
        public static readonly int HeavyPunch = Animator.StringToHash("HeavyPunch");
        public static readonly int HeavyKick = Animator.StringToHash("HeavyKick");

        // ----- Combat: incoming hits / death -----
        public static readonly int Hit = Animator.StringToHash("Hit");
        public static readonly int Knockout = Animator.StringToHash("Knockout");
        public static readonly int Getup = Animator.StringToHash("Getup");

        // ----- Police-specific -----
        // Note: Police uses a generic "Attack" trigger rather than the player's
        // Light/Heavy split. Renaming this requires updating Police.prefab's
        // Animator Controller too.
        public static readonly int Attack = Animator.StringToHash("Attack");

        // ----- States (used with Animator.Play, not triggers) -----
        public static readonly int IdleState = Animator.StringToHash("Idle");
        public static readonly int EmptyState = Animator.StringToHash("Empty");
    }
}