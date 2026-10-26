using System;
using UnityEngine;

namespace BeanBlaster.Core
{
    /// <summary>Contains all information required to evaluate and react to one damage attempt.</summary>
    [Serializable]
    public struct DamageInfo
    {
        public float amount;
        public Vector2 sourcePosition;
        public Vector2 knockbackDirection;
        public float knockbackForce;
        public Team sourceTeam;
        public DamageType type;
        public bool ignoreInvulnerability;

        /// <summary>Creates a fully specified damage request.</summary>
        public DamageInfo(
            float amount,
            Vector2 sourcePosition,
            Vector2 knockbackDirection,
            float knockbackForce,
            Team sourceTeam,
            DamageType type,
            bool ignoreInvulnerability = false)
        {
            this.amount = amount;
            this.sourcePosition = sourcePosition;
            this.knockbackDirection = knockbackDirection;
            this.knockbackForce = knockbackForce;
            this.sourceTeam = sourceTeam;
            this.type = type;
            this.ignoreInvulnerability = ignoreInvulnerability;
        }
    }
}
