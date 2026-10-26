using System.Collections.Generic;
using BeanBlaster.Core;
using UnityEngine;

namespace BeanBlaster.Combat
{
    /// <summary>Applies deduplicated radial damage and knockback using collider-edge distance.</summary>
    public static class ExplosionUtility
    {
        /// <summary>Damages each valid target once and returns the number of targets affected.</summary>
        public static int Apply(
            Vector2 centre,
            float radius,
            float centreDamage,
            float edgeDamageMultiplier,
            float centreKnockback,
            Team sourceTeam,
            LayerMask targetMask,
            DamageType damageType = DamageType.Explosion,
            bool guaranteedStun = true)
        {
            if (radius <= 0f || centreDamage <= 0f)
            {
                return 0;
            }

            Collider2D[] colliders = Physics2D.OverlapCircleAll(centre, radius, targetMask);
            HashSet<Health> processed = new HashSet<Health>();
            int damagedCount = 0;
            float clampedEdgeMultiplier = Mathf.Clamp01(edgeDamageMultiplier);

            foreach (Collider2D collider in colliders)
            {
                Health target = collider.GetComponentInParent<Health>();
                if (target == null || !processed.Add(target))
                {
                    continue;
                }

                Vector2 closestPoint = collider.ClosestPoint(centre);
                float normalizedDistance = Mathf.Clamp01(Vector2.Distance(centre, closestPoint) / radius);
                float strength = Mathf.Lerp(1f, clampedEdgeMultiplier, normalizedDistance);
                Vector2 direction = closestPoint - centre;
                if (direction.sqrMagnitude <= Mathf.Epsilon)
                {
                    direction = (Vector2)target.transform.position - centre;
                }

                if (direction.sqrMagnitude <= Mathf.Epsilon)
                {
                    direction = Vector2.up;
                }

                bool applied = target.TryApplyDamage(new DamageInfo(
                    centreDamage * strength,
                    centre,
                    direction.normalized,
                    centreKnockback * strength,
                    sourceTeam,
                    damageType,
                    guaranteedStun));
                if (applied)
                {
                    damagedCount++;
                }
            }

            return damagedCount;
        }
    }
}
