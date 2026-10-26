using System;
using BeanBlaster.Core;
using UnityEngine;

namespace BeanBlaster.Combat
{
    /// <summary>Implements team-aware health, invulnerability, healing, reset, and one-shot death events.</summary>
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField] private Team team = Team.Neutral;
        [SerializeField, Min(0f)] private float invulnerabilityDuration;
        [SerializeField] private bool useInvulnerability;

        private float currentHealth;
        private float invulnerableUntil;
        private bool initialized;
        private bool deathRaised;

        public event Action<DamageInfo> Damaged;
        public event Action<float, float> HealthChanged;
        public event Action<DamageInfo> Died;

        public Team Team => team;
        public bool IsAlive => EnsureInitializedAndGetHealth() > 0f;
        public float CurrentHealth => EnsureInitializedAndGetHealth();
        public float MaxHealth => maxHealth;
        public bool IsInvulnerable => useInvulnerability && Time.time < invulnerableUntil;

        private void Awake()
        {
            InitializeIfNeeded();
        }

        /// <summary>Attempts to apply damage after checking life, team, and invulnerability rules.</summary>
        public bool TryApplyDamage(DamageInfo info)
        {
            InitializeIfNeeded();
            if (currentHealth <= 0f || info.amount <= 0f || info.sourceTeam == team)
            {
                return false;
            }

            bool fireBypassesEnemyInvulnerability = info.type == DamageType.Fire && team == Team.Enemy;
            if (IsInvulnerable && !info.ignoreInvulnerability && !fireBypassesEnemyInvulnerability)
            {
                return false;
            }

            currentHealth = Mathf.Clamp(currentHealth - info.amount, 0f, maxHealth);
            if (useInvulnerability && !fireBypassesEnemyInvulnerability && currentHealth > 0f)
            {
                invulnerableUntil = Time.time + invulnerabilityDuration;
            }

            Damaged?.Invoke(info);
            HealthChanged?.Invoke(currentHealth, maxHealth);
            if (currentHealth <= 0f && !deathRaised)
            {
                deathRaised = true;
                Died?.Invoke(info);
            }

            return true;
        }

        /// <summary>Restores positive health without exceeding the configured maximum.</summary>
        public void Heal(float amount)
        {
            InitializeIfNeeded();
            if (amount <= 0f || currentHealth <= 0f || currentHealth >= maxHealth)
            {
                return;
            }

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>Restores full health and clears death and invulnerability state.</summary>
        public void ResetHealth()
        {
            initialized = true;
            currentHealth = maxHealth;
            invulnerableUntil = 0f;
            deathRaised = false;
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void InitializeIfNeeded()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            currentHealth = maxHealth;
        }

        private float EnsureInitializedAndGetHealth()
        {
            InitializeIfNeeded();
            return currentHealth;
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            invulnerabilityDuration = Mathf.Max(0f, invulnerabilityDuration);
            if (initialized)
            {
                currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            }
        }
    }
}
