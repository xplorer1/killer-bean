using BeanBlaster.Combat;
using BeanBlaster.Core;
using UnityEngine;

namespace BeanBlaster.Enemies
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class EnemyProjectile : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D projectileCollider;
        [SerializeField] private LayerMask groundLayerMask = 1 << 6;

        private EnemyProjectilePool pool;
        private Collider2D ownerCollider;
        private float damage;
        private float remainingLifetime;
        private bool launched;

        public bool IsLaunched => launched;

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
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
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

        private void OnTriggerEnter2D(Collider2D other)
        {
            ProcessCollision(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            ProcessCollision(collision.collider);
        }

        public void Launch(Vector2 direction, float speed, float attackDamage, Collider2D owner)
        {
            EnsureComponents();
            ResetForReuse();
            ownerCollider = owner;
            damage = attackDamage;
            remainingLifetime = 3f;
            launched = true;
            Vector2 normalized = direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector2.right;
            transform.right = normalized;
            body.velocity = normalized * speed;
            if (ownerCollider != null)
            {
                Physics2D.IgnoreCollision(projectileCollider, ownerCollider, true);
            }
        }

        public void ProcessCollision(Collider2D other)
        {
            if (!launched || other == null || other == ownerCollider)
            {
                return;
            }

            Health target = other.GetComponentInParent<Health>();
            bool hitGround = (groundLayerMask.value & (1 << other.gameObject.layer)) != 0;
            if (target != null && target.Team == Team.Player)
            {
                Vector2 direction = body.velocity.sqrMagnitude > Mathf.Epsilon ? body.velocity.normalized : (Vector2)transform.right;
                target.TryApplyDamage(new DamageInfo(damage, transform.position, direction, 4f, Team.Enemy, DamageType.Bullet));
                ReturnToPool();
            }
            else if (hitGround)
            {
                ReturnToPool();
            }
        }

        public void ResetForReuse()
        {
            if (ownerCollider != null && projectileCollider != null)
            {
                Physics2D.IgnoreCollision(projectileCollider, ownerCollider, false);
            }
            launched = false;
            ownerCollider = null;
            damage = 0f;
            remainingLifetime = 0f;
            if (body != null)
            {
                body.velocity = Vector2.zero;
            }
        }

        internal void AssignPool(EnemyProjectilePool ownerPool)
        {
            pool = ownerPool;
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
