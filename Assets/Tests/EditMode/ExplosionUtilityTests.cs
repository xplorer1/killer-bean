using System.Reflection;
using BeanBlaster.Combat;
using BeanBlaster.Core;
using NUnit.Framework;
using UnityEngine;

namespace BeanBlaster.Tests.EditMode
{
    public sealed class ExplosionUtilityTests
    {
        private readonly System.Collections.Generic.List<GameObject> objects =
            new System.Collections.Generic.List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject value in objects)
            {
                Object.DestroyImmediate(value);
            }

            objects.Clear();
        }

        [Test]
        public void Apply_UsesFullDamageAtCentreAndFortyPercentAtEdge()
        {
            Health centre = CreateTarget("Centre", Vector2.zero, 200f);
            Health edge = CreateTarget("Edge", new Vector2(4f, 0f), 200f);
            Physics2D.SyncTransforms();

            int count = ExplosionUtility.Apply(
                Vector2.zero,
                3.5f,
                100f,
                0.4f,
                9f,
                Team.Player,
                1 << 8);

            Assert.That(count, Is.EqualTo(2));
            Assert.That(centre.CurrentHealth, Is.EqualTo(100f).Within(0.001f));
            Assert.That(edge.CurrentHealth, Is.EqualTo(160f).Within(0.25f));
        }

        [Test]
        public void Apply_DamagesHealthOnlyOnceAcrossMultipleColliders()
        {
            Health target = CreateTarget("Multi Collider", Vector2.zero, 200f);
            target.gameObject.AddComponent<CircleCollider2D>().radius = 0.25f;
            Physics2D.SyncTransforms();

            int count = ExplosionUtility.Apply(
                Vector2.zero,
                3.5f,
                100f,
                0.4f,
                9f,
                Team.Player,
                1 << 8);

            Assert.That(count, Is.EqualTo(1));
            Assert.That(target.CurrentHealth, Is.EqualTo(100f).Within(0.001f));
        }

        [Test]
        public void Apply_RespectsTargetMaskAndTeamRules()
        {
            Health player = CreateTarget("Player", Vector2.zero, 100f, Team.Player, 7);
            Physics2D.SyncTransforms();

            int count = ExplosionUtility.Apply(
                Vector2.zero,
                3.5f,
                100f,
                0.4f,
                9f,
                Team.Player,
                (1 << 7) | (1 << 8));

            Assert.That(count, Is.EqualTo(0));
            Assert.That(player.CurrentHealth, Is.EqualTo(100f));
        }

        private Health CreateTarget(
            string name,
            Vector2 position,
            float maxHealth,
            Team team = Team.Enemy,
            int layer = 8)
        {
            GameObject targetObject = new GameObject(name);
            objects.Add(targetObject);
            targetObject.layer = layer;
            targetObject.transform.position = position;
            targetObject.AddComponent<BoxCollider2D>().size = Vector2.one;
            Health health = targetObject.AddComponent<Health>();
            SetField(health, "maxHealth", maxHealth);
            SetField(health, "team", team);
            health.ResetHealth();
            return health;
        }

        private static void SetField(Health health, string name, object value)
        {
            FieldInfo field = typeof(Health).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(health, value);
        }
    }
}
