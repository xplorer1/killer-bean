using BeanBlaster.Player;
using NUnit.Framework;
using UnityEngine;

namespace BeanBlaster.Tests.EditMode
{
    public sealed class MobileInputStateTests
    {
        private GameObject gameObject;
        private MobileInputState input;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("Mobile Input State Test");
            input = gameObject.AddComponent<MobileInputState>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void Move_IsClampedToUnitCircle()
        {
            input.SetMove(new Vector2(3f, 4f));

            Assert.That(input.Move.magnitude, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void Aim_PersistsAfterStickIsReleased()
        {
            input.SetAim(Vector2.up, true);
            input.SetAim(Vector2.zero, false);

            Assert.That(input.AimDirection, Is.EqualTo(Vector2.up));
            Assert.That(input.HasAimDirection, Is.True);
            Assert.That(input.AimEngaged, Is.False);
        }

        [Test]
        public void JumpPress_IsLatchedOncePerPress()
        {
            input.SetJumpHeld(true);
            input.SetJumpHeld(true);

            Assert.That(input.ConsumeJumpPress(), Is.True);
            Assert.That(input.ConsumeJumpPress(), Is.False);

            input.SetJumpHeld(false);
            input.SetJumpHeld(true);
            Assert.That(input.ConsumeJumpPress(), Is.True);
        }

        [Test]
        public void ReleaseAll_ClearsHeldControls()
        {
            input.SetMove(Vector2.left);
            input.SetAim(Vector2.up, true);
            input.SetJumpHeld(true);
            input.SetFireHeld(true);

            input.ReleaseAll();

            Assert.That(input.Move, Is.EqualTo(Vector2.zero));
            Assert.That(input.AimEngaged, Is.False);
            Assert.That(input.JumpHeld, Is.False);
            Assert.That(input.FireHeld, Is.False);
            Assert.That(input.ConsumeJumpPress(), Is.False);
        }

        [Test]
        public void ButtonSignals_AreForwarded()
        {
            int next = 0;
            int previous = 0;
            int pause = 0;
            input.NextWeaponPressed += () => next++;
            input.PrevWeaponPressed += () => previous++;
            input.PausePressed += () => pause++;

            input.PressNextWeapon();
            input.PressPreviousWeapon();
            input.PressPause();

            Assert.That(next, Is.EqualTo(1));
            Assert.That(previous, Is.EqualTo(1));
            Assert.That(pause, Is.EqualTo(1));
        }
    }
}
