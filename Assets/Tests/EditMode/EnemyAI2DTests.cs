using System.Reflection;
using BeanBlaster.Combat;
using BeanBlaster.Core;
using BeanBlaster.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace BeanBlaster.Tests.EditMode
{
    public sealed class EnemyAI2DTests
    {
        private GameObject enemyObject;
        private GameObject playerObject;
        private EnemyStats stats;
        private EnemyAI2D ai;
        private Health enemyHealth;

        [SetUp]
        public void SetUp()
        {
            playerObject = new GameObject("Player");
            playerObject.layer = 7;
            playerObject.transform.position = new Vector2(0.5f, 0f);
            playerObject.AddComponent<BoxCollider2D>();
            Health playerHealth = playerObject.AddComponent<Health>();
            SetHealthField(playerHealth, "team", Team.Player);

            enemyObject = new GameObject("Enemy");
            enemyObject.layer = 8;
            enemyObject.AddComponent<Rigidbody2D>().gravityScale = 0f;
            enemyObject.AddComponent<BoxCollider2D>();
            enemyHealth = enemyObject.AddComponent<Health>();
            SetHealthField(enemyHealth, "team", Team.Enemy);
            stats = CreateStats();
            ai = enemyObject.AddComponent<EnemyAI2D>();
            ai.Configure(stats, playerObject.transform);
            Physics2D.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(enemyObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(stats);
        }

        [Test]
        public void StunDuringAttackWindup_CancelsAttackAndRepeatedHitRefreshesTimer()
        {
            int attacks = 0;
            ai.AttackCommitted += () => attacks++;
            ai.SamplePerception();
            ai.AdvanceState(0.1f);
            ai.AdvanceState(0.1f);
            Assert.That(ai.CurrentState, Is.EqualTo(EnemyState.Attack));

            enemyHealth.TryApplyDamage(Hit(10f));
            Assert.That(ai.CurrentState, Is.EqualTo(EnemyState.Stunned));
            ai.AdvanceState(0.3f);
            float reducedTimer = ai.StunRemaining;
            enemyHealth.TryApplyDamage(Hit(10f));

            Assert.That(ai.StunRemaining, Is.GreaterThan(reducedTimer));
            ai.AdvanceState(0.3f);
            Assert.That(attacks, Is.Zero);
        }

        [Test]
        public void PlayerMoreThanTwoMetresBehind_IsNotDetected()
        {
            playerObject.transform.position = new Vector2(-3f, 0f);
            Physics2D.SyncTransforms();

            ai.SamplePerception();

            Assert.That(ai.PlayerDetected, Is.False);
            Assert.That(ai.CurrentState, Is.EqualTo(EnemyState.Patrol));
        }

        [Test]
        public void DeathResolution_IsRaisedExactlyOnce()
        {
            int deaths = 0;
            ai.DeathResolved += _ => deaths++;

            enemyHealth.TryApplyDamage(new DamageInfo(1000f, Vector2.zero, Vector2.right, 0f, Team.Player, DamageType.Bullet));
            enemyHealth.TryApplyDamage(new DamageInfo(1000f, Vector2.zero, Vector2.right, 0f, Team.Player, DamageType.Bullet));

            Assert.That(ai.CurrentState, Is.EqualTo(EnemyState.Dead));
            Assert.That(deaths, Is.EqualTo(1));
        }

        [Test]
        public void ContactDamage_HitsPlayerButNotEnemyAndStopsWhileStunned()
        {
            Health playerHealth = playerObject.GetComponent<Health>();
            GameObject friendlyEnemy = new GameObject("Friendly Enemy");
            friendlyEnemy.layer = 8;
            BoxCollider2D friendlyCollider = friendlyEnemy.AddComponent<BoxCollider2D>();
            Health friendlyHealth = friendlyEnemy.AddComponent<Health>();
            SetHealthField(friendlyHealth, "team", Team.Enemy);

            ai.ProcessContact(friendlyCollider);
            ai.ProcessContact(playerObject.GetComponent<Collider2D>());
            Assert.That(friendlyHealth.CurrentHealth, Is.EqualTo(100f));
            Assert.That(playerHealth.CurrentHealth, Is.EqualTo(90f));

            enemyHealth.TryApplyDamage(Hit(10f));
            playerHealth.ResetHealth();
            ai.ProcessContact(playerObject.GetComponent<Collider2D>());
            Assert.That(playerHealth.CurrentHealth, Is.EqualTo(100f));
            Object.DestroyImmediate(friendlyEnemy);
        }

        private static DamageInfo Hit(float knockback)
        {
            return new DamageInfo(1f, Vector2.left, Vector2.right, knockback, Team.Player, DamageType.Bullet);
        }

        private static EnemyStats CreateStats()
        {
            EnemyStats value = ScriptableObject.CreateInstance<EnemyStats>();
            SetStatsField(value, "detectionRange", 7f);
            SetStatsField(value, "attackRange", 1.2f);
            SetStatsField(value, "attackWindup", 0.6f);
            SetStatsField(value, "stunKnockbackThreshold", 8f);
            SetStatsField(value, "stunDuration", 0.5f);
            return value;
        }

        private static void SetStatsField(EnemyStats target, string name, object value)
        {
            FieldInfo field = typeof(EnemyStats).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private static void SetHealthField(Health target, string name, object value)
        {
            FieldInfo field = typeof(Health).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
