using System.Reflection;
using BeanBlaster.Combat;
using BeanBlaster.Core;
using NUnit.Framework;
using UnityEngine;

namespace BeanBlaster.Tests.EditMode
{
    public sealed class HealthTests
    {
        private GameObject gameObject;
        private Health health;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("Health Test");
            health = gameObject.AddComponent<Health>();
            SetField("maxHealth", 100f);
            SetField("team", Team.Player);
            SetField("useInvulnerability", false);
            health.ResetHealth();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void SameTeamDamageIsRejected()
        {
            bool applied = health.TryApplyDamage(CreateDamage(20f, Team.Player));

            Assert.That(applied, Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(100f));
        }

        [Test]
        public void AcceptedDamageRaisesEventsInOrder()
        {
            string eventOrder = string.Empty;
            health.Damaged += _ => eventOrder += "D";
            health.HealthChanged += (_, _) => eventOrder += "H";

            bool applied = health.TryApplyDamage(CreateDamage(25f, Team.Enemy));

            Assert.That(applied, Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(75f));
            Assert.That(eventOrder, Is.EqualTo("DH"));
        }

        [Test]
        public void DeathEventFiresExactlyOnce()
        {
            int deaths = 0;
            health.Died += _ => deaths++;

            Assert.That(health.TryApplyDamage(CreateDamage(500f, Team.Enemy)), Is.True);
            Assert.That(health.TryApplyDamage(CreateDamage(1f, Team.Enemy)), Is.False);

            Assert.That(health.CurrentHealth, Is.Zero);
            Assert.That(health.IsAlive, Is.False);
            Assert.That(deaths, Is.EqualTo(1));
        }

        [Test]
        public void HealingCannotReviveOrExceedMaximum()
        {
            health.TryApplyDamage(CreateDamage(30f, Team.Enemy));
            health.Heal(100f);
            Assert.That(health.CurrentHealth, Is.EqualTo(100f));

            health.TryApplyDamage(CreateDamage(500f, Team.Enemy));
            health.Heal(50f);
            Assert.That(health.CurrentHealth, Is.Zero);
        }

        [Test]
        public void KillPlaneCanBypassInvulnerability()
        {
            SetField("useInvulnerability", true);
            SetField("invulnerabilityDuration", 100f);
            Assert.That(health.TryApplyDamage(CreateDamage(1f, Team.Enemy)), Is.True);

            DamageInfo kill = CreateDamage(1000f, Team.Neutral);
            kill.type = DamageType.KillPlane;
            kill.ignoreInvulnerability = true;
            Assert.That(health.TryApplyDamage(kill), Is.True);
            Assert.That(health.IsAlive, Is.False);
        }

        [Test]
        public void InvulnerabilityRejectsNormalFollowUpDamage()
        {
            SetField("useInvulnerability", true);
            SetField("invulnerabilityDuration", 100f);

            Assert.That(health.TryApplyDamage(CreateDamage(10f, Team.Enemy)), Is.True);
            Assert.That(health.TryApplyDamage(CreateDamage(10f, Team.Enemy)), Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(90f));
        }

        [Test]
        public void FireBypassesEnemyInvulnerability()
        {
            SetField("team", Team.Enemy);
            SetField("useInvulnerability", true);
            SetField("invulnerabilityDuration", 100f);
            health.ResetHealth();
            Assert.That(health.TryApplyDamage(CreateDamage(10f, Team.Player)), Is.True);

            DamageInfo fire = CreateDamage(12f, Team.Player);
            fire.type = DamageType.Fire;
            Assert.That(health.TryApplyDamage(fire), Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(78f));
        }

        private static DamageInfo CreateDamage(float amount, Team sourceTeam)
        {
            return new DamageInfo(
                amount,
                Vector2.zero,
                Vector2.zero,
                0f,
                sourceTeam,
                DamageType.Bullet);
        }

        private void SetField(string name, object value)
        {
            FieldInfo field = typeof(Health).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(health, value);
        }
    }
}
