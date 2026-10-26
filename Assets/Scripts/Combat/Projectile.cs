using BeanBlaster.Core;
using UnityEngine;

namespace BeanBlaster.Combat
{
    /// <summary>Implements the reusable standard projectile lifecycle used by the Phase 6 firing slice.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D projectileCollider;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private TrailRenderer trailRenderer;

        private ProjectilePool pool;
        private WeaponData weapon;
        private Team sourceTeam;
        private Collider2D ownerCollider;
        private float remainingLifetime;
        private bool launched;

        public int PrefabKey { get; private set; }
        public bool IsLaunched => launched;

        private void Awake()
        {
            body ??= GetComponent<Rigidbody2D>();
            projectileCollider ??= GetComponent<Collider2D>();
            spriteRenderer ??= GetComponent<SpriteRenderer>();
            trailRenderer ??= GetComponent<TrailRenderer>();
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void Update()
        {
            if (!launched)
            {
                return;
            }

            remainingLifetime -= Time.deltaTime;
            if (remainingLifetime <= 0f)
            {
                ReturnToPool();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!launched || weapon == null)
            {
                return;
            }

            Health target = collision.collider.GetComponentInParent<Health>();
            if (target != null)
            {
                target.TryApplyDamage(new DamageInfo(
                    weapon.Damage,
                    transform.position,
                    body.velocity.sqrMagnitude > 0f ? body.velocity.normalized : (Vector2)transform.right,
                    weapon.KnockbackForce,
                    sourceTeam,
                    DamageType.Bullet));
            }

            ReturnToPool();
        }

        /// <summary>Initializes velocity, damage data, lifetime, appearance, and owner collision for one shot.</summary>
        public bool Launch(WeaponData data, Vector2 direction, Team team, Collider2D owner)
        {
            if (data == null || data.AmmoType != AmmoType.Standard || direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            ResetForReuse();
            weapon = data;
            sourceTeam = team;
            ownerCollider = owner;
            remainingLifetime = data.ProjectileLifetime;
            launched = true;
            transform.right = direction.normalized;
            body.gravityScale = data.ProjectileGravityScale;
            body.velocity = direction.normalized * data.ProjectileSpeed;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = data.ProjectileColor;
            }

            if (ownerCollider != null && projectileCollider != null)
            {
                Physics2D.IgnoreCollision(projectileCollider, ownerCollider, true);
            }

            if (trailRenderer != null)
            {
                trailRenderer.Clear();
                trailRenderer.emitting = true;
            }

            return true;
        }

        /// <summary>Clears every piece of state that may leak between pooled uses.</summary>
        public void ResetForReuse()
        {
            if (ownerCollider != null && projectileCollider != null)
            {
                Physics2D.IgnoreCollision(projectileCollider, ownerCollider, false);
            }

            launched = false;
            weapon = null;
            ownerCollider = null;
            remainingLifetime = 0f;
            if (body != null)
            {
                body.velocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.gravityScale = 0f;
            }

            if (trailRenderer != null)
            {
                trailRenderer.emitting = false;
                trailRenderer.Clear();
            }
        }

        internal void AssignPool(ProjectilePool ownerPool, int prefabKey)
        {
            pool = ownerPool;
            PrefabKey = prefabKey;
        }

        private void ReturnToPool()
        {
            if (pool != null)
            {
                pool.Return(this);
            }
            else
            {
                ResetForReuse();
                gameObject.SetActive(false);
            }
        }
    }
}
