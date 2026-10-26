using UnityEngine;

namespace BeanBlaster.Enemies
{
    /// <summary>Defines movement, perception, combat, reward, and body tuning for an enemy type.</summary>
    [CreateAssetMenu(fileName = "EnemyStats", menuName = "Bean Blaster/Enemy Stats")]
    public sealed class EnemyStats : ScriptableObject
    {
        [SerializeField, Min(1f)] private float maxHealth = 60f;
        [SerializeField, Min(0f)] private float patrolSpeed = 2f;
        [SerializeField, Min(0f)] private float chaseSpeed = 3.5f;
        [SerializeField, Min(0f)] private float detectionRange = 7f;
        [SerializeField, Min(0f)] private float loseSightTime = 3f;
        [SerializeField, Min(0f)] private float attackRange = 1.2f;
        [SerializeField, Min(0f)] private float preferredDistance;
        [SerializeField, Min(0f)] private float attackDamage = 10f;
        [SerializeField, Min(0f)] private float attackCooldown = 1f;
        [SerializeField, Min(0f)] private float attackWindup = 0.25f;
        [SerializeField, Min(0f)] private float projectileSpeed;
        [SerializeField, Min(0f)] private float contactDamage = 10f;
        [SerializeField, Min(0f)] private float stunKnockbackThreshold = 8f;
        [SerializeField, Min(0f)] private float stunDuration = 0.5f;
        [SerializeField, Min(0)] private int coinValue = 25;
        [SerializeField] private Vector2 size = new Vector2(0.8f, 1.2f);
        [SerializeField] private Color color = Color.red;
        [SerializeField] private bool usesRangedAttack;

        public float MaxHealth => maxHealth;
        public float PatrolSpeed => patrolSpeed;
        public float ChaseSpeed => chaseSpeed;
        public float DetectionRange => detectionRange;
        public float LoseSightTime => loseSightTime;
        public float AttackRange => attackRange;
        public float PreferredDistance => preferredDistance;
        public float AttackDamage => attackDamage;
        public float AttackCooldown => attackCooldown;
        public float AttackWindup => attackWindup;
        public float ProjectileSpeed => projectileSpeed;
        public float ContactDamage => contactDamage;
        public float StunKnockbackThreshold => stunKnockbackThreshold;
        public float StunDuration => stunDuration;
        public int CoinValue => coinValue;
        public Vector2 Size => size;
        public Color Color => color;
        public bool UsesRangedAttack => usesRangedAttack;

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            patrolSpeed = Mathf.Max(0f, patrolSpeed);
            chaseSpeed = Mathf.Max(0f, chaseSpeed);
            detectionRange = Mathf.Max(0f, detectionRange);
            loseSightTime = Mathf.Max(0f, loseSightTime);
            attackRange = Mathf.Max(0f, attackRange);
            preferredDistance = Mathf.Max(0f, preferredDistance);
            attackDamage = Mathf.Max(0f, attackDamage);
            attackCooldown = Mathf.Max(0f, attackCooldown);
            attackWindup = Mathf.Max(0f, attackWindup);
            projectileSpeed = Mathf.Max(0f, projectileSpeed);
            contactDamage = Mathf.Max(0f, contactDamage);
            stunKnockbackThreshold = Mathf.Max(0f, stunKnockbackThreshold);
            stunDuration = Mathf.Max(0f, stunDuration);
            coinValue = Mathf.Max(0, coinValue);
            size.x = Mathf.Max(0.01f, size.x);
            size.y = Mathf.Max(0.01f, size.y);
        }
    }
}
