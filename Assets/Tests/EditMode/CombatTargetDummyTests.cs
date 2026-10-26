using System.Reflection;
using BeanBlaster.Combat;
using BeanBlaster.Core;
using BeanBlaster.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace BeanBlaster.Tests.EditMode
{
    public sealed class CombatTargetDummyTests
    {
        private GameObject targetObject;
        private Health health;
        private BoxCollider2D collider;
        private Transform fill;

        [SetUp]
        public void SetUp()
        {
            targetObject = new GameObject("Target");
            targetObject.layer = 8;
            collider = targetObject.AddComponent<BoxCollider2D>();
            targetObject.AddComponent<SpriteRenderer>();
            health = targetObject.AddComponent<Health>();
            SetField(health, "team", Team.Enemy);
            GameObject fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(targetObject.transform, false);
            fill = fillObject.transform;
            targetObject.AddComponent<CombatTargetDummy>().Configure(fill);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(targetObject);
        }

        [Test]
        public void Damage_UpdatesHealthBarScale()
        {
            health.TryApplyDamage(new DamageInfo(25f, Vector2.zero, Vector2.right, 0f, Team.Player, DamageType.Bullet));

            Assert.That(fill.localScale.x, Is.EqualTo(0.75f).Within(0.001f));
        }

        [Test]
        public void Death_DisablesTargetCollider()
        {
            health.TryApplyDamage(new DamageInfo(100f, Vector2.zero, Vector2.right, 0f, Team.Player, DamageType.Bullet));

            Assert.That(collider.enabled, Is.False);
            Assert.That(fill.localScale.x, Is.EqualTo(0f));
        }

        private static void SetField(Health target, string name, object value)
        {
            FieldInfo field = typeof(Health).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
