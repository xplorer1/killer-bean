using System;
using System.Collections.Generic;
using BeanBlaster.Core;
using BeanBlaster.Level;
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
        [SerializeField] private LayerMask enemyLayerMask = 1 << 8;
        [SerializeField] private LayerMask groundLayerMask = 1 << 6;
        [SerializeField] private ExplosionEffect explosionEffectPrefab;
        [SerializeField, Min(0f)] private float grenadeShakeAmplitude = 0.25f;
        [SerializeField, Min(0f)] private float grenadeShakeDuration = 0.2f;

        private ProjectilePool pool;
        private WeaponData weapon;
        private Team sourceTeam;
        private Collider2D ownerCollider;
        private readonly HashSet<Health> hitTargets = new HashSet<Health>();
        private readonly HashSet<Collider2D> ignoredTargetColliders = new HashSet<Collider2D>();
        private Vector2 launchDirection;
        private float remainingLifetime;
        private bool launched;
        private bool isSplitBeam;
        private float shotDamage;

        public int PrefabKey { get; private set; }
        public bool IsLaunched => launched;
        public int UniqueTargetsHit => hitTargets.Count;

        public event Action<Vector2, float> Exploded;
        public event Action<Vector2, Vector2> BeamSplit;

        private void Awake()
        {
            EnsureComponents();
        }

        private void EnsureComponents()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (projectileCollider == null)
            {
                projectileCollider = GetComponent<Collider2D>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (trailRenderer == null)
            {
                trailRenderer = GetComponent<TrailRenderer>();
            }
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
                ResolveLifetimeExpiry();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (weapon != null && weapon.AmmoType == AmmoType.Beam)
            {
                Vector2 normal = collision.contactCount > 0
                    ? collision.GetContact(0).normal
                    : -launchDirection;
                ProcessBeamCollision(collision.collider, normal);
                return;
            }

            ProcessCollision(collision.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            ProcessCollision(other);
        }

        /// <summary>Initializes velocity, damage data, lifetime, appearance, and owner collision for one shot.</summary>
        public bool Launch(WeaponData data, Vector2 direction, Team team, Collider2D owner)
        {
            return LaunchInternal(data, direction, team, owner, false);
        }

        private bool LaunchInternal(WeaponData data, Vector2 direction, Team team, Collider2D owner, bool splitBeam)
        {
            if (data == null ||
                (data.AmmoType != AmmoType.Standard &&
                 data.AmmoType != AmmoType.Piercing &&
                 data.AmmoType != AmmoType.Explosive &&
                 data.AmmoType != AmmoType.Beam) ||
                direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            EnsureComponents();
            ResetForReuse();
            weapon = data;
            sourceTeam = team;
            ownerCollider = owner;
            remainingLifetime = splitBeam ? data.SubBeamLifetime : data.ProjectileLifetime;
            launched = true;
            isSplitBeam = splitBeam;
            shotDamage = splitBeam ? data.SubBeamDamage : data.Damage;
            launchDirection = direction.normalized;
            transform.right = launchDirection;
            body.gravityScale = data.ProjectileGravityScale;
            body.velocity = launchDirection * data.ProjectileSpeed;
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
            isSplitBeam = false;
            shotDamage = 0f;
            weapon = null;
            ownerCollider = null;
            launchDirection = Vector2.zero;
            remainingLifetime = 0f;
            foreach (Collider2D ignoredCollider in ignoredTargetColliders)
            {
                if (ignoredCollider != null && projectileCollider != null)
                {
                    Physics2D.IgnoreCollision(projectileCollider, ignoredCollider, false);
                }
            }

            ignoredTargetColliders.Clear();
            hitTargets.Clear();
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

        /// <summary>Processes one collider contact. Exposed for deterministic combat verification.</summary>
        public void ProcessCollision(Collider2D other)
        {
            if (!launched || weapon == null || other == null || other == ownerCollider)
            {
                return;
            }

            Health target = other.GetComponentInParent<Health>();
            if (weapon.AmmoType == AmmoType.Explosive)
            {
                Detonate();
                return;
            }

            if (weapon.AmmoType == AmmoType.Piercing)
            {
                ProcessPiercingCollision(other, target);
                return;
            }

            if (weapon.AmmoType == AmmoType.Beam)
            {
                ProcessBeamCollision(other, -launchDirection);
                return;
            }

            if (target != null)
            {
                TryDamage(target);
            }

            ReturnToPool();
        }

        /// <summary>Resolves beam damage or a single Ground reflection split using the collision normal.</summary>
        public void ProcessBeamCollision(Collider2D other, Vector2 collisionNormal)
        {
            if (!launched || weapon == null || weapon.AmmoType != AmmoType.Beam || other == null || other == ownerCollider)
            {
                return;
            }

            Health target = other.GetComponentInParent<Health>();
            if (target != null)
            {
                TryDamage(target);
                ReturnToPool();
                return;
            }

            bool hitGround = (groundLayerMask.value & (1 << other.gameObject.layer)) != 0;
            if (!isSplitBeam && hitGround)
            {
                SplitBeam(collisionNormal);
            }

            ReturnToPool();
        }

        private void SplitBeam(Vector2 collisionNormal)
        {
            Vector2 normal = collisionNormal.sqrMagnitude > Mathf.Epsilon
                ? collisionNormal.normalized
                : -launchDirection;
            Vector2 reflected = Vector2.Reflect(launchDirection, normal).normalized;
            int count = Mathf.Max(1, weapon.SplitCount);
            float centre = (count - 1) * 0.5f;
            for (int i = 0; i < count; i++)
            {
                float angle = (i - centre) * weapon.SplitAngle;
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * reflected;
                BeamSplit?.Invoke(transform.position, direction);

                Projectile child = pool != null ? pool.GetByPrefabKey(PrefabKey) : null;
                if (child == null)
                {
                    continue;
                }

                child.transform.position = transform.position + (Vector3)(direction * 0.05f);
                child.LaunchInternal(weapon, direction, sourceTeam, ownerCollider, true);
            }
        }

        private void ProcessPiercingCollision(Collider2D other, Health target)
        {
            if (target == null)
            {
                ReturnToPool();
                return;
            }

            if (!hitTargets.Add(target))
            {
                IgnoreTargetCollider(other);
                RestoreFlightVelocity();
                return;
            }

            if (!TryDamage(target))
            {
                hitTargets.Remove(target);
                IgnoreTargetCollider(other);
                RestoreFlightVelocity();
                return;
            }

            if (hitTargets.Count > weapon.PierceCount)
            {
                ReturnToPool();
                return;
            }

            IgnoreTargetCollider(other);
            RestoreFlightVelocity();
        }

        private bool TryDamage(Health target)
        {
            return target.TryApplyDamage(new DamageInfo(
                shotDamage,
                transform.position,
                launchDirection.sqrMagnitude > 0f ? launchDirection : (Vector2)transform.right,
                weapon.KnockbackForce,
                sourceTeam,
                DamageType.Bullet));
        }

        private void IgnoreTargetCollider(Collider2D targetCollider)
        {
            if (projectileCollider == null || targetCollider == null || !ignoredTargetColliders.Add(targetCollider))
            {
                return;
            }

            Physics2D.IgnoreCollision(projectileCollider, targetCollider, true);
        }

        private void RestoreFlightVelocity()
        {
            body.velocity = launchDirection * weapon.ProjectileSpeed;
        }

        private void ResolveLifetimeExpiry()
        {
            if (!launched || weapon == null)
            {
                return;
            }

            if (weapon.AmmoType == AmmoType.Explosive)
            {
                Detonate();
            }
            else
            {
                ReturnToPool();
            }
        }

        private void Detonate()
        {
            if (!launched || weapon == null)
            {
                return;
            }

            launched = false;
            Vector2 centre = transform.position;
            float radius = weapon.AoeRadius;
            ExplosionUtility.Apply(
                centre,
                radius,
                weapon.Damage,
                weapon.AoeEdgeDamageMultiplier,
                weapon.KnockbackForce,
                sourceTeam,
                enemyLayerMask);
            if (explosionEffectPrefab != null)
            {
                ExplosionEffect effect = Instantiate(explosionEffectPrefab, centre, Quaternion.identity);
                effect.Play(centre, radius);
            }

            CameraShake.Request(grenadeShakeAmplitude, grenadeShakeDuration);
            Exploded?.Invoke(centre, radius);
            ReturnToPool();
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
