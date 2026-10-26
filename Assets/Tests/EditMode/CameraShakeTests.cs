using BeanBlaster.Level;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;

namespace BeanBlaster.Tests.EditMode
{
    public sealed class CameraShakeTests
    {
        [Test]
        public void Request_BroadcastsSpecifiedValues()
        {
            float receivedAmplitude = 0f;
            float receivedDuration = 0f;
            void Handler(float amplitude, float duration)
            {
                receivedAmplitude = amplitude;
                receivedDuration = duration;
            }

            CameraShake.ShakeRequested += Handler;
            try
            {
                CameraShake.Request(0.25f, 0.2f);
            }
            finally
            {
                CameraShake.ShakeRequested -= Handler;
            }

            Assert.That(receivedAmplitude, Is.EqualTo(0.25f));
            Assert.That(receivedDuration, Is.EqualTo(0.2f));
        }

        [Test]
        public void Request_IgnoresZeroOrNegativeValues()
        {
            int requestCount = 0;
            void Handler(float _, float __) => requestCount++;
            CameraShake.ShakeRequested += Handler;
            try
            {
                CameraShake.Request(0f, 0.2f);
                CameraShake.Request(0.25f, 0f);
                CameraShake.Request(-1f, -1f);
            }
            finally
            {
                CameraShake.ShakeRequested -= Handler;
            }

            Assert.That(requestCount, Is.EqualTo(0));
        }

        [Test]
        public void Receiver_BeginsShakeAndRestoresPositionWhenDisabled()
        {
            GameObject cameraObject = new GameObject("Camera");
            cameraObject.transform.localPosition = new Vector3(2f, 3f, -10f);
            CameraShake shake = cameraObject.AddComponent<CameraShake>();
            InvokeLifecycle(shake, "Awake");
            InvokeLifecycle(shake, "OnEnable");

            CameraShake.Request(0.25f, 0.2f);
            Assert.That(shake.IsShaking, Is.True);

            InvokeLifecycle(shake, "OnDisable");
            Assert.That(cameraObject.transform.localPosition, Is.EqualTo(new Vector3(2f, 3f, -10f)));
            Object.DestroyImmediate(cameraObject);
        }

        private static void InvokeLifecycle(CameraShake shake, string methodName)
        {
            MethodInfo method = typeof(CameraShake).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(shake, null);
        }
    }
}
