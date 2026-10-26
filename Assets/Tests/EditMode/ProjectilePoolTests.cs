using System.Reflection;
using BeanBlaster.Combat;
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

        private void SetField(string name, object value)
        {
            FieldInfo field = typeof(ProjectilePool).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(pool, value);
        }
    }
}
