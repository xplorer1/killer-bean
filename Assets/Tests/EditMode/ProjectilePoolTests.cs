using System.Reflection;
using BeanBlaster.Combat;
using BeanBlaster.Core;
using NUnit.Framework;
using UnityEngine;

namespace BeanBlaster.Tests.EditMode
{
    public sealed class ProjectilePoolTests
    {
        private GameObject poolObject;
        private GameObject prefabObject;
        private ProjectilePool pool;
        private Projectile prefab;

        [SetUp]
        public void SetUp()
        {
            poolObject = new GameObject("Projectile Pool");
            pool = poolObject.AddComponent<ProjectilePool>();
            SetField("prewarmCount", 2);
            SetField("maximumPerPrefab", 3);

            prefabObject = new GameObject("Projectile Prefab");
            prefabObject.AddComponent<Rigidbody2D>();
            prefabObject.AddComponent<BoxCollider2D>();
            prefab = prefabObject.AddComponent<Projectile>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(poolObject);
            Object.DestroyImmediate(prefabObject);
        }

        [Test]
        public void FirstGetPrewarmsAndActivatesOneProjectile()
        {
            Projectile projectile = pool.Get(prefab);

            Assert.That(projectile, Is.Not.Null);
            Assert.That(projectile.gameObject.activeSelf, Is.True);
            Assert.That(pool.GetTotalCount(prefab), Is.EqualTo(2));
        }

        [Test]
        public void ReturnedProjectileIsResetAndReused()
        {
            Projectile first = pool.Get(prefab);
            Projectile second = pool.Get(prefab);
            pool.Return(first);
            Projectile reused = pool.Get(prefab);

            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(reused, Is.SameAs(first));
            Assert.That(reused.IsLaunched, Is.False);
        }

        [Test]
        public void PoolStopsGrowingAtConfiguredMaximum()
        {
            Projectile first = pool.Get(prefab);
            Projectile second = pool.Get(prefab);
            Projectile third = pool.Get(prefab);
            Projectile rejected = pool.Get(prefab);

            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(third, Is.Not.Null);
            Assert.That(rejected, Is.Null);
            Assert.That(pool.GetTotalCount(prefab), Is.EqualTo(3));
        }

        [Test]
        public void PiercingProjectile_DamagesThreeUniqueTargetsThenStops()
        {
            WeaponData magnum = CreateMagnum();
            Assert.That(prefab.Launch(magnum, Vector2.right, Team.Player, null), Is.True);
            GameObject first = CreateEnemy("First");
            GameObject second = CreateEnemy("Second");
            GameObject third = CreateEnemy("Third");

            prefab.ProcessCollision(first.GetComponent<Collider2D>());
            Assert.That(prefab.IsLaunched, Is.True);
            prefab.ProcessCollision(second.GetComponent<Collider2D>());
            Assert.That(prefab.IsLaunched, Is.True);
            prefab.ProcessCollision(third.GetComponent<Collider2D>());

            Assert.That(first.GetComponent<Health>().CurrentHealth, Is.EqualTo(50f));
            Assert.That(second.GetComponent<Health>().CurrentHealth, Is.EqualTo(50f));
            Assert.That(third.GetComponent<Health>().CurrentHealth, Is.EqualTo(50f));
            Assert.That(prefab.IsLaunched, Is.False);
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
            Object.DestroyImmediate(third);
            Object.DestroyImmediate(magnum);
        }

        [Test]
        public void PiercingProjectile_DamagesSameTargetOnlyOnce()
        {
            WeaponData magnum = CreateMagnum();
            Assert.That(prefab.Launch(magnum, Vector2.right, Team.Player, null), Is.True);
            GameObject enemy = CreateEnemy("Enemy");
            Collider2D enemyCollider = enemy.GetComponent<Collider2D>();

            prefab.ProcessCollision(enemyCollider);
            prefab.ProcessCollision(enemyCollider);

            Assert.That(enemy.GetComponent<Health>().CurrentHealth, Is.EqualTo(50f));
            Assert.That(prefab.UniqueTargetsHit, Is.EqualTo(1));
            Assert.That(prefab.IsLaunched, Is.True);
            Object.DestroyImmediate(enemy);
            Object.DestroyImmediate(magnum);
        }

        [Test]
        public void PiercingProjectile_StopsOnGeometryAndClearsHitState()
        {
            WeaponData magnum = CreateMagnum();
            Assert.That(prefab.Launch(magnum, Vector2.right, Team.Player, null), Is.True);
            GameObject enemy = CreateEnemy("Enemy");
            GameObject ground = new GameObject("Ground");
            ground.layer = 6;
            BoxCollider2D groundCollider = ground.AddComponent<BoxCollider2D>();

            prefab.ProcessCollision(enemy.GetComponent<Collider2D>());
            prefab.ProcessCollision(groundCollider);

            Assert.That(prefab.IsLaunched, Is.False);
            Assert.That(prefab.UniqueTargetsHit, Is.EqualTo(0));
            Object.DestroyImmediate(enemy);
            Object.DestroyImmediate(ground);
            Object.DestroyImmediate(magnum);
        }

        [Test]
        public void ExplosiveProjectile_DetonatesOnceOnContact()
        {
            WeaponData grenade = CreateGrenade();
            GameObject enemy = CreateEnemy("Explosion Target");
            SetHealthField(enemy.GetComponent<Health>(), "maxHealth", 200f);
            enemy.GetComponent<Health>().ResetHealth();
            Physics2D.SyncTransforms();
            int explosionCount = 0;
            prefab.Exploded += (_, __) => explosionCount++;
            Assert.That(prefab.Launch(grenade, Vector2.right, Team.Player, null), Is.True);

            prefab.ProcessCollision(enemy.GetComponent<Collider2D>());
            InvokeLifetimeExpiry(prefab);

            Assert.That(explosionCount, Is.EqualTo(1));
            Assert.That(enemy.GetComponent<Health>().CurrentHealth, Is.EqualTo(80f).Within(0.001f));
            Assert.That(prefab.IsLaunched, Is.False);
            Object.DestroyImmediate(enemy);
            Object.DestroyImmediate(grenade);
        }

        [Test]
        public void ExplosiveProjectile_DetonatesWhenFuseExpires()
        {
            WeaponData grenade = CreateGrenade();
            GameObject enemy = CreateEnemy("Fuse Target");
            SetHealthField(enemy.GetComponent<Health>(), "maxHealth", 200f);
            enemy.GetComponent<Health>().ResetHealth();
            Physics2D.SyncTransforms();
            Assert.That(prefab.Launch(grenade, Vector2.right, Team.Player, null), Is.True);

            InvokeLifetimeExpiry(prefab);

            Assert.That(enemy.GetComponent<Health>().CurrentHealth, Is.EqualTo(80f).Within(0.001f));
            Assert.That(prefab.IsLaunched, Is.False);
            Object.DestroyImmediate(enemy);
            Object.DestroyImmediate(grenade);
        }

        private static WeaponData CreateMagnum()
        {
            WeaponData weapon = ScriptableObject.CreateInstance<WeaponData>();
            SetWeaponField(weapon, "id", "magnum");
            SetWeaponField(weapon, "ammoType", AmmoType.Piercing);
            SetWeaponField(weapon, "damage", 50f);
            SetWeaponField(weapon, "projectileSpeed", 30f);
            SetWeaponField(weapon, "projectileLifetime", 2f);
            SetWeaponField(weapon, "knockbackForce", 10f);
            SetWeaponField(weapon, "pierceCount", 2);
            return weapon;
        }

        private static WeaponData CreateGrenade()
        {
            WeaponData weapon = ScriptableObject.CreateInstance<WeaponData>();
            SetWeaponField(weapon, "id", "explosive_gl");
            SetWeaponField(weapon, "ammoType", AmmoType.Explosive);
            SetWeaponField(weapon, "damage", 120f);
            SetWeaponField(weapon, "projectileSpeed", 14f);
            SetWeaponField(weapon, "projectileGravityScale", 1.5f);
            SetWeaponField(weapon, "projectileLifetime", 3f);
            SetWeaponField(weapon, "knockbackForce", 9f);
            SetWeaponField(weapon, "aoeRadius", 3.5f);
            SetWeaponField(weapon, "aoeEdgeDamageMultiplier", 0.4f);
            return weapon;
        }

        private static void InvokeLifetimeExpiry(Projectile projectile)
        {
            MethodInfo method = typeof(Projectile).GetMethod(
                "ResolveLifetimeExpiry",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(projectile, null);
        }

        private static GameObject CreateEnemy(string name)
        {
            GameObject enemy = new GameObject(name);
            enemy.layer = 8;
            enemy.AddComponent<BoxCollider2D>();
            Health health = enemy.AddComponent<Health>();
            SetHealthField(health, "team", Team.Enemy);
            return enemy;
        }

        private static void SetWeaponField(WeaponData weapon, string name, object value)
        {
            FieldInfo field = typeof(WeaponData).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(weapon, value);
        }

        private static void SetHealthField(Health health, string name, object value)
        {
            FieldInfo field = typeof(Health).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(health, value);
        }

        private void SetField(string name, object value)
        {
            FieldInfo field = typeof(ProjectilePool).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(pool, value);
        }
    }
}
