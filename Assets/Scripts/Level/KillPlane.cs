using BeanBlaster.Combat;
using BeanBlaster.Core;
using UnityEngine;

namespace BeanBlaster.Level
{
    /// <summary>Applies unavoidable lethal damage when the player enters the level kill plane.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class KillPlane : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float lethalDamage = 1000000f;

        private void OnTriggerEnter2D(Collider2D other)
        {
            Health health = other.GetComponentInParent<Health>();
            if (health == null || health.Team != Team.Player)
            {
                return;
            }

            health.TryApplyDamage(new DamageInfo(
                lethalDamage,
                transform.position,
                Vector2.zero,
                0f,
                Team.Neutral,
                DamageType.KillPlane,
                true));
        }

        private void Reset()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;
        }
    }
}
