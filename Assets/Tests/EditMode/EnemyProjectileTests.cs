using System.Reflection;
using BeanBlaster.Combat;
using BeanBlaster.Core;
using BeanBlaster.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace BeanBlaster.Tests.EditMode
{
    public sealed class EnemyProjectileTests
    {
        [Test]
        public void ProjectileDamagesPlayerButNeverEnemy()
        {
            GameObject projectileObject = CreateProjectileObject();
            EnemyProjectile projectile = projectileObject.GetComponent<EnemyProjectile>();
            GameObject player = CreateTarget("Player", Team.Player, 7);
            GameObject enemy = CreateTarget("Enemy", Team.Enemy, 8);
            projectile.Launch(Vector2.right, 10f, 8f, null);

            projectile.ProcessCollision(enemy.GetComponent<Collider2D>());
            Assert.That(enemy.GetComponent<Health>().CurrentHealth, Is.EqualTo(100f));
            Assert.That(projectile.IsLaunched, Is.True);
            projectile.ProcessCollision(player.GetComponent<Collider2D>());

            Assert.That(player.GetComponent<Health>().CurrentHealth, Is.EqualTo(92f));
            Assert.That(projectile.IsLaunched, Is.False);
            Object.DestroyImmediate(projectileObject);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(enemy);
        }

        [Test]
        public void ProjectileReturnsToPoolOnGroundAndIsReused()
        {
            GameObject prefabObject = CreateProjectileObject();
            GameObject poolObject = new GameObject("Enemy Projectile Pool");
            EnemyProjectilePool pool = poolObject.AddComponent<EnemyProjectilePool>();
            pool.Configure(prefabObject.GetComponent<EnemyProjectile>(), 1, 1);
            GameObject ground = new GameObject("Ground");
            ground.layer = 6;
            BoxCollider2D groundCollider = ground.AddComponent<BoxCollider2D>();
            EnemyProjectile first = pool.Get();
            first.Launch(Vector2.right, 10f, 8f, null);

            first.ProcessCollision(groundCollider);
            EnemyProjectile reused = pool.Get();

            Assert.That(reused, Is.SameAs(first));
            Object.DestroyImmediate(prefabObject);
            Object.DestroyImmediate(poolObject);
            Object.DestroyImmediate(ground);
        }

        private static GameObject CreateProjectileObject()
        {
            GameObject value = new GameObject("Enemy Projectile");
            value.AddComponent<Rigidbody2D>();
            value.AddComponent<BoxCollider2D>();
            value.AddComponent<EnemyProjectile>();
            return value;
        }

        private static GameObject CreateTarget(string name, Team team, int layer)
        {
            GameObject value = new GameObject(name);
            value.layer = layer;
            value.AddComponent<BoxCollider2D>();
            Health health = value.AddComponent<Health>();
            FieldInfo field = typeof(Health).GetField("team", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(health, team);
            return value;
        }
    }
}
