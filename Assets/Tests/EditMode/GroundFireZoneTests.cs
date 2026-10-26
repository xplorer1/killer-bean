using System.Reflection;
using BeanBlaster.Combat;
using BeanBlaster.Core;
using NUnit.Framework;
using UnityEngine;

namespace BeanBlaster.Tests.EditMode
{
    public sealed class GroundFireZoneTests
    {
        private GameObject zoneObject;
        private GroundFireZone zone;

        [SetUp]
        public void SetUp()
        {
            zoneObject = new GameObject("Ground Fire Zone");
            zone = zoneObject.AddComponent<GroundFireZone>();
        }

        [TearDown]
        public void TearDown()
        {
            if (zoneObject != null)
            {
                Object.DestroyImmediate(zoneObject);
            }
        }

        [Test]
        public void FullExposure_DealsExactlyOneHundredNinetyTwoFireDamage()
        {
            GameObject enemy = CreateCharacter("Enemy", Team.Enemy, 300f, 8);
            enemy.transform.position = new Vector2(0f, 0.5f);
            Physics2D.SyncTransforms();
            zone.Play(Vector2.zero, Team.Player);

            for (int i = 0; i < 16; i++)
            {
                zone.Advance(0.25f);
            }

            Assert.That(enemy.GetComponent<Health>().CurrentHealth, Is.EqualTo(108f));
            Assert.That(zone.IsPlaying, Is.False);
            Object.DestroyImmediate(enemy);
        }

        [Test]
        public void FireZone_NeverDamagesPlayer()
        {
            GameObject player = CreateCharacter("Player", Team.Player, 300f, 8);
            player.transform.position = new Vector2(0f, 0.5f);
            Physics2D.SyncTransforms();
            zone.Play(Vector2.zero, Team.Player);

            zone.Advance(1f);

            Assert.That(player.GetComponent<Health>().CurrentHealth, Is.EqualTo(300f));
            Object.DestroyImmediate(player);
        }

        [Test]
        public void MultipleColliders_DamageOneHealthOnlyOncePerTick()
        {
            GameObject enemy = CreateCharacter("Enemy", Team.Enemy, 100f, 8);
            GameObject child = new GameObject("Extra Collider");
            child.layer = 8;
            child.transform.SetParent(enemy.transform);
            child.AddComponent<BoxCollider2D>();
            enemy.transform.position = new Vector2(0f, 0.5f);
            Physics2D.SyncTransforms();
            zone.Play(Vector2.zero, Team.Player);

            zone.Advance(0.25f);

            Assert.That(enemy.GetComponent<Health>().CurrentHealth, Is.EqualTo(88f));
            Object.DestroyImmediate(enemy);
        }

        private static GameObject CreateCharacter(string name, Team team, float maxHealth, int layer)
        {
            GameObject character = new GameObject(name);
            character.layer = layer;
            character.AddComponent<BoxCollider2D>();
            Health health = character.AddComponent<Health>();
            SetHealthField(health, "team", team);
            SetHealthField(health, "maxHealth", maxHealth);
            health.ResetHealth();
            return character;
        }

        private static void SetHealthField(Health health, string name, object value)
        {
            FieldInfo field = typeof(Health).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(health, value);
        }
    }
}
