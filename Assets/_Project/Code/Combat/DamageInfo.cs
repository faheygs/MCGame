using UnityEngine;

namespace MCGame.Combat
{
    /// <summary>
    /// Data struct passed when anything deals damage to anything else.
    /// Used by both player attacks and enemy attacks.
    /// Future: guns, explosions, vehicles will use this same struct.
    /// </summary>
    public struct DamageInfo
    {
        /// <summary>How much damage this hit deals.</summary>
        public int amount;

        /// <summary>The GameObject that dealt the damage (player, enemy, explosion, etc).</summary>
        public GameObject source;

        /// <summary>World-space direction the hit came from. Used for hit reactions and knockback.</summary>
        public Vector3 hitDirection;

        /// <summary>True if this was a heavy attack. Enemies may react differently to heavy vs light.</summary>
        public bool isHeavy;

        public DamageInfo(int amount, GameObject source, Vector3 hitDirection, bool isHeavy = false)
        {
            this.amount = amount;
            this.source = source;
            this.hitDirection = hitDirection;
            this.isHeavy = isHeavy;
        }
    }
}