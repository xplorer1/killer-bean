using System.Collections.Generic;
using BeanBlaster.Core;
using UnityEngine;

namespace BeanBlaster.Combat
{
    /// <summary>Applies deterministic enemy-only fire ticks over a ground-aligned box.</summary>
    public sealed class GroundFireZone : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private LayerMask enemyLayerMask = 1 << 8;
        [SerializeField] private Vector2 size = new Vector2(4f, 1f);
        [SerializeField, Min(0f)] private float duration = 4f;
        [SerializeField, Min(0.01f)] private float tickInterval = 0.25f;
        [SerializeField, Min(0f)] private float tickDamage = 12f;
        [SerializeField, Min(0f)] private float fadeDuration = 0.5f;

        private readonly HashSet<Health> tickTargets = new HashSet<Health>();
        private Team sourceTeam;
        private float elapsed;
        private float tickAccumulator;
        private bool playing;

        public bool IsPlaying => playing;
        public float Elapsed => elapsed;

        private void Awake()
        {
            if (visual == null)
            {
                visual = GetComponent<SpriteRenderer>();
            }
        }

        private void Update()
        {
            Advance(Time.deltaTime);
        }

        /// <summary>Starts the zone with its bottom edge resting on the supplied ground point.</summary>
        public void Play(Vector2 groundPoint, Team damageSource)
        {
            sourceTeam = damageSource;
            elapsed = 0f;
            tickAccumulator = 0f;
            playing = true;
            transform.position = groundPoint + (Vector2.up * (size.y * 0.5f));
            transform.localScale = new Vector3(size.x, size.y, 1f);
            if (visual != null)
            {
                visual.enabled = true;
                SetVisualAlpha(0.7f);
            }
        }

        /// <summary>Advances zone time; public to make tick totals deterministic in verification.</summary>
        public void Advance(float deltaTime)
        {
            if (!playing || deltaTime <= 0f)
            {
                return;
            }

            float activeDelta = Mathf.Min(deltaTime, duration - elapsed);
            elapsed += activeDelta;
            tickAccumulator += activeDelta;
            while (tickAccumulator + Mathf.Epsilon >= tickInterval)
            {
                tickAccumulator -= tickInterval;
                ApplyTick();
            }

            UpdateVisual();
            if (elapsed + Mathf.Epsilon >= duration)
            {
                playing = false;
                if (visual != null)
                {
                    visual.enabled = false;
                }

                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void ApplyTick()
        {
            tickTargets.Clear();
            Collider2D[] overlaps = Physics2D.OverlapBoxAll(transform.position, size, 0f, enemyLayerMask);
            foreach (Collider2D overlap in overlaps)
            {
                Health target = overlap != null ? overlap.GetComponentInParent<Health>() : null;
                if (target == null || !tickTargets.Add(target))
                {
                    continue;
                }

                target.TryApplyDamage(new DamageInfo(
                    tickDamage,
                    transform.position,
                    Vector2.zero,
                    0f,
                    sourceTeam,
                    DamageType.Fire));
            }
        }

        private void UpdateVisual()
        {
            if (visual == null)
            {
                return;
            }

            float pulse = Mathf.Lerp(0.5f, 0.9f, (Mathf.Sin(elapsed * 12f) + 1f) * 0.5f);
            float remaining = duration - elapsed;
            float fade = fadeDuration > 0f ? Mathf.Clamp01(remaining / fadeDuration) : 1f;
            SetVisualAlpha(pulse * fade);
        }

        private void SetVisualAlpha(float alpha)
        {
            Color color = visual.color;
            color.a = alpha;
            visual.color = color;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.4f);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}
