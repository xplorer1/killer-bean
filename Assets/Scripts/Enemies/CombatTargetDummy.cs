using System.Collections;
using BeanBlaster.Combat;
using BeanBlaster.Core;
using UnityEngine;

namespace BeanBlaster.Enemies
{
    /// <summary>Provides visible damage and death feedback for the development combat arena.</summary>
    [RequireComponent(typeof(Health), typeof(SpriteRenderer), typeof(Collider2D))]
    public sealed class CombatTargetDummy : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private Transform healthBarFill;
        [SerializeField] private Color aliveColour = new Color(0.8f, 0.15f, 0.12f);
        [SerializeField] private Color hitColour = Color.white;
        [SerializeField] private Color deadColour = new Color(0.2f, 0.2f, 0.2f);
        [SerializeField, Min(0.01f)] private float hitFlashDuration = 0.08f;

        private Coroutine flashRoutine;
        private bool subscribed;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            RefreshHealthBar(health.CurrentHealth, health.MaxHealth);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        /// <summary>Connects a generated health-bar fill and initializes visual feedback.</summary>
        public void Configure(Transform fill)
        {
            healthBarFill = fill;
            ResolveReferences();
            Subscribe();
            bodyRenderer.color = health.IsAlive ? aliveColour : deadColour;
            bodyCollider.enabled = health.IsAlive;
            RefreshHealthBar(health.CurrentHealth, health.MaxHealth);
        }

        private void ResolveReferences()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponent<SpriteRenderer>();
            }

            if (bodyCollider == null)
            {
                bodyCollider = GetComponent<Collider2D>();
            }
        }

        private void Subscribe()
        {
            if (subscribed || health == null)
            {
                return;
            }

            health.Damaged += OnDamaged;
            health.HealthChanged += RefreshHealthBar;
            health.Died += OnDied;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || health == null)
            {
                return;
            }

            health.Damaged -= OnDamaged;
            health.HealthChanged -= RefreshHealthBar;
            health.Died -= OnDied;
            subscribed = false;
        }

        private void OnDamaged(DamageInfo info)
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }

            flashRoutine = StartCoroutine(FlashHit());
        }

        private void RefreshHealthBar(float current, float maximum)
        {
            if (healthBarFill == null)
            {
                return;
            }

            Vector3 scale = healthBarFill.localScale;
            scale.x = maximum <= 0f ? 0f : Mathf.Clamp01(current / maximum);
            healthBarFill.localScale = scale;
        }

        private void OnDied(DamageInfo info)
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
            }

            bodyRenderer.color = deadColour;
            bodyCollider.enabled = false;
        }

        private IEnumerator FlashHit()
        {
            bodyRenderer.color = hitColour;
            yield return new WaitForSeconds(hitFlashDuration);
            bodyRenderer.color = health.IsAlive ? aliveColour : deadColour;
            flashRoutine = null;
        }

        private void OnValidate()
        {
            hitFlashDuration = Mathf.Max(0.01f, hitFlashDuration);
        }
    }
}
