using BeanBlaster.UI;
using NUnit.Framework;
using UnityEngine;

namespace BeanBlaster.Tests.EditMode
{
    public sealed class SafeAreaFitterTests
    {
        [Test]
        public void CalculateAnchors_NormalizesPixelInsets()
        {
            Rect anchors = SafeAreaFitter.CalculateAnchors(
                new Rect(100f, 50f, 1800f, 980f),
                new Vector2(2000f, 1080f));

            Assert.That(anchors.xMin, Is.EqualTo(0.05f).Within(0.0001f));
            Assert.That(anchors.yMin, Is.EqualTo(50f / 1080f).Within(0.0001f));
            Assert.That(anchors.xMax, Is.EqualTo(0.95f).Within(0.0001f));
            Assert.That(anchors.yMax, Is.EqualTo(1030f / 1080f).Within(0.0001f));
        }

        [Test]
        public void CalculateAnchors_HandlesUnavailableScreenSize()
        {
            Rect anchors = SafeAreaFitter.CalculateAnchors(new Rect(), Vector2.zero);

            Assert.That(anchors, Is.EqualTo(new Rect(0f, 0f, 1f, 1f)));
        }

        [Test]
        public void CalculateAnchors_ClampsInvalidSafeArea()
        {
            Rect anchors = SafeAreaFitter.CalculateAnchors(
                Rect.MinMaxRect(-20f, -10f, 120f, 110f),
                new Vector2(100f, 100f));

            Assert.That(anchors, Is.EqualTo(new Rect(0f, 0f, 1f, 1f)));
        }
    }
}
