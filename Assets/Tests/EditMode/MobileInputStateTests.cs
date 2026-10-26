using BeanBlaster.Player;
using NUnit.Framework;
using System.Reflection;
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

        [Test]
        public void PlayerInputReader_ConsumesTouchMovementJumpAndFire()
        {
            PlayerInputReader reader = gameObject.AddComponent<PlayerInputReader>();
            reader.SetMobileInput(input);
            input.SetMove(new Vector2(-0.75f, 0f));
            input.SetJumpHeld(true);
            input.SetAim(Vector2.up, true);

            Assert.That(reader.MoveX, Is.EqualTo(-0.75f));
            Assert.That(reader.JumpHeld, Is.True);
            Assert.That(reader.JumpPressedThisFrame, Is.True);
            Assert.That(reader.JumpPressedThisFrame, Is.False);
            Assert.That(reader.FireHeld, Is.True);
        }

        [Test]
        public void PlayerInputReader_UsesTouchAimAndForwardsButtons()
        {
            PlayerInputReader reader = gameObject.AddComponent<PlayerInputReader>();
            reader.SetMobileInput(input);
            int next = 0;
            int previous = 0;
            int pause = 0;
            reader.NextWeaponPressed += () => next++;
            reader.PrevWeaponPressed += () => previous++;
            reader.PausePressed += () => pause++;
            input.SetAim(Vector2.up, true);

            typeof(PlayerInputReader).GetMethod("UpdateAim", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(reader, null);
            input.PressNextWeapon();
            input.PressPreviousWeapon();
            input.PressPause();

            Assert.That(reader.AimDirection, Is.EqualTo(Vector2.up));
            Assert.That(next, Is.EqualTo(1));
            Assert.That(previous, Is.EqualTo(1));
            Assert.That(pause, Is.EqualTo(1));
        }
    }
}
