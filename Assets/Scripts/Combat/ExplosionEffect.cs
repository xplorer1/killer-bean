using UnityEngine;

namespace BeanBlaster.Combat
{
    /// <summary>Displays a short expanding, fading explosion circle and destroys itself.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ExplosionEffect : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField, Min(0.01f)] private float duration = 0.3f;
        [SerializeField, Range(0f, 1f)] private float startingScaleFraction = 0.15f;
        [SerializeField] private Color colour = new Color(1f, 0.45f, 0.05f, 0.9f);

        private float radius;
        private float elapsed;
        private bool playing;

        public float Radius => radius;
        public float Duration => duration;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void Update()
        {
            if (!playing)
            {
                return;
            }

            elapsed += Time.deltaTime;
            ApplyProgress(Mathf.Clamp01(elapsed / duration));
            if (elapsed >= duration)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>Starts an explosion sized to the supplied world-space radius.</summary>
        public void Play(Vector2 position, float worldRadius)
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            transform.position = position;
            radius = Mathf.Max(0f, worldRadius);
            elapsed = 0f;
            playing = true;
            ApplyProgress(0f);
        }

        /// <summary>Applies a normalized visual state, allowing deterministic verification.</summary>
        public void ApplyProgress(float normalizedProgress)
        {
            float progress = Mathf.Clamp01(normalizedProgress);
            float diameter = radius * 2f;
            float scale = Mathf.Lerp(diameter * startingScaleFraction, diameter, progress);
            transform.localScale = new Vector3(scale, scale, 1f);
            if (spriteRenderer != null)
            {
                Color current = colour;
                current.a = colour.a * (1f - progress);
                spriteRenderer.color = current;
            }
        }

        private void OnValidate()
        {
            duration = Mathf.Max(0.01f, duration);
            startingScaleFraction = Mathf.Clamp01(startingScaleFraction);
        }
    }
}
