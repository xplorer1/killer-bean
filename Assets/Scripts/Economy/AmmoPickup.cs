using BeanBlaster.Combat;
using BeanBlaster.Core;
using UnityEngine;

namespace BeanBlaster.Economy
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class AmmoPickup : MonoBehaviour
    {
        [SerializeField] private string weaponId;
        [SerializeField, Min(1)] private int amount = 1;
        private bool collected;

        public void Configure(string targetWeaponId, int ammoAmount)
        {
            weaponId = targetWeaponId;
            amount = Mathf.Max(1, ammoAmount);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryCollect(other);
        }

        public bool TryCollect(Collider2D other)
        {
            Health health = other == null ? null : other.GetComponentInParent<Health>();
            if (collected || health == null || health.Team != Team.Player)
            {
                return false;
            }
            collected = true;
            EconomyService.AddAmmo(weaponId, amount);
            gameObject.SetActive(false);
            return true;
        }
    }
}
