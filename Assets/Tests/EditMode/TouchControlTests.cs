using BeanBlaster.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BeanBlaster.Tests.EditMode
{
    public sealed class TouchControlTests
    {
        private GameObject inputObject;
        private GameObject eventSystemObject;
        private MobileInputState input;
        private EventSystem eventSystem;

        [SetUp]
        public void SetUp()
        {
            inputObject = new GameObject("Input");
            input = inputObject.AddComponent<MobileInputState>();
            eventSystemObject = new GameObject("Event System");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(eventSystemObject);
            Object.DestroyImmediate(inputObject);
        }

        [Test]
        public void JumpButton_IgnoresReleaseFromDifferentPointer()
        {
            GameObject buttonObject = new GameObject("Jump Button");
            TouchActionButton button = buttonObject.AddComponent<TouchActionButton>();
            button.Configure(input, TouchActionButtonRole.Jump);

            button.OnPointerDown(Pointer(10));
            button.OnPointerUp(Pointer(11));
            Assert.That(input.JumpHeld, Is.True);

            button.OnPointerUp(Pointer(10));
            Assert.That(input.JumpHeld, Is.False);
            Object.DestroyImmediate(buttonObject);
        }

        [Test]
        public void AimStick_IgnoresReleaseFromDifferentPointer()
        {
            GameObject stickObject = new GameObject("Aim Stick", typeof(RectTransform));
            RectTransform area = stickObject.GetComponent<RectTransform>();
            area.sizeDelta = new Vector2(200f, 200f);
            VirtualTouchStick stick = stickObject.AddComponent<VirtualTouchStick>();
            stick.Configure(input, area, null, VirtualTouchStickRole.AimAndFire);

            stick.OnPointerDown(Pointer(20));
            stick.OnPointerUp(Pointer(21));
            Assert.That(input.AimEngaged, Is.True);

            stick.OnPointerUp(Pointer(20));
            Assert.That(input.AimEngaged, Is.False);
            Object.DestroyImmediate(stickObject);
        }

        private PointerEventData Pointer(int pointerId)
        {
            return new PointerEventData(eventSystem)
            {
                pointerId = pointerId,
                position = Vector2.zero
            };
        }
    }
}
