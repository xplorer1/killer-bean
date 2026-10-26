using System.Reflection;
using BeanBlaster.Combat;
using NUnit.Framework;
using UnityEngine;

namespace BeanBlaster.Tests.EditMode
{
    public sealed class ExplosionEffectTests
    {
        private GameObject effectObject;
        private ExplosionEffect effect;
        private SpriteRenderer renderer;

        [SetUp]
        public void SetUp()
        {
            effectObject = new GameObject("Explosion Effect");
            renderer = effectObject.AddComponent<SpriteRenderer>();
            effect = effectObject.AddComponent<ExplosionEffect>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(effectObject);
        }

        [Test]
        public void Play_StartsAtConfiguredFractionOfDiameter()
        {
            effect.Play(new Vector2(2f, 3f), 3.5f);

            Assert.That(effect.transform.position, Is.EqualTo(new Vector3(2f, 3f, 0f)));
            Assert.That(effect.Radius, Is.EqualTo(3.5f));
            Assert.That(effect.transform.localScale.x, Is.EqualTo(1.05f).Within(0.001f));
            Assert.That(renderer.color.a, Is.EqualTo(0.9f).Within(0.001f));
        }

        [Test]
        public void ApplyProgress_ExpandsToDiameterAndFadesOut()
        {
            effect.Play(Vector2.zero, 3.5f);

            effect.ApplyProgress(1f);

            Assert.That(effect.transform.localScale, Is.EqualTo(new Vector3(7f, 7f, 1f)));
            Assert.That(renderer.color.a, Is.EqualTo(0f));
        }

        [Test]
        public void Duration_DefaultsToSpecificationValue()
        {
            Assert.That(effect.Duration, Is.EqualTo(0.3f).Within(0.001f));
            FieldInfo durationField = typeof(ExplosionEffect).GetField(
                "duration",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(durationField, Is.Not.Null);
        }
    }
}
