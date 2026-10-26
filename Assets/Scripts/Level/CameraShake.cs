using System;
using UnityEngine;

namespace BeanBlaster.Level
{
    /// <summary>Receives global shake requests and applies temporary local camera offsets.</summary>
    public sealed class CameraShake : MonoBehaviour
    {
        [SerializeField] private Transform shakeTarget;

        private Vector3 restingLocalPosition;
        private float remainingDuration;
        private float activeAmplitude;

        public static event Action<float, float> ShakeRequested;

        public bool IsShaking => remainingDuration > 0f;

        private void Awake()
        {
            if (shakeTarget == null)
            {
                shakeTarget = transform;
            }

            restingLocalPosition = shakeTarget.localPosition;
        }

        private void OnEnable()
        {
            ShakeRequested += BeginShake;
        }

        private void OnDisable()
        {
            ShakeRequested -= BeginShake;
            RestorePosition();
        }

        private void LateUpdate()
        {
            if (remainingDuration <= 0f)
            {
                RestorePosition();
                return;
            }

            remainingDuration = Mathf.Max(0f, remainingDuration - Time.deltaTime);
            Vector2 offset = UnityEngine.Random.insideUnitCircle * activeAmplitude;
            shakeTarget.localPosition = restingLocalPosition + new Vector3(offset.x, offset.y, 0f);
            if (remainingDuration <= 0f)
            {
                RestorePosition();
            }
        }

        /// <summary>Broadcasts a sanitized camera-shake request.</summary>
        public static void Request(float amplitude, float duration)
        {
            float safeAmplitude = Mathf.Max(0f, amplitude);
            float safeDuration = Mathf.Max(0f, duration);
            if (safeAmplitude <= 0f || safeDuration <= 0f)
            {
                return;
            }

            ShakeRequested?.Invoke(safeAmplitude, safeDuration);
        }

        private void BeginShake(float amplitude, float duration)
        {
            if (shakeTarget == null)
            {
                shakeTarget = transform;
                restingLocalPosition = shakeTarget.localPosition;
            }

            activeAmplitude = Mathf.Max(activeAmplitude, amplitude);
            remainingDuration = Mathf.Max(remainingDuration, duration);
        }

        private void RestorePosition()
        {
            if (shakeTarget != null)
            {
                shakeTarget.localPosition = restingLocalPosition;
            }

            remainingDuration = 0f;
            activeAmplitude = 0f;
        }
    }
}
