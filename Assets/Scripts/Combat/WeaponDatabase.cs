using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeanBlaster.Combat
{
    /// <summary>Provides stable ID and shop-slot lookup for all configured weapons.</summary>
    [CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Bean Blaster/Weapon Database")]
    public sealed class WeaponDatabase : ScriptableObject
    {
        [SerializeField] private List<WeaponData> weapons = new List<WeaponData>();

        private Dictionary<string, WeaponData> byId;
        private Dictionary<int, WeaponData> bySlot;

        public IReadOnlyList<WeaponData> All => weapons;

        /// <summary>Returns the weapon with the specified stable ID, or null when it is unknown.</summary>
        public WeaponData GetById(string weaponId)
        {
            EnsureCache();
            return !string.IsNullOrEmpty(weaponId) && byId.TryGetValue(weaponId, out WeaponData weapon)
                ? weapon
                : null;
        }

        /// <summary>Returns the weapon assigned to a one-based shop slot, or null when absent.</summary>
        public WeaponData GetBySlot(int shopSlot)
        {
            EnsureCache();
            return bySlot.TryGetValue(shopSlot, out WeaponData weapon) ? weapon : null;
        }

        private void OnEnable()
        {
            InvalidateCache();
        }

        private void OnValidate()
        {
            InvalidateCache();
        }

        private void EnsureCache()
        {
            if (byId != null && bySlot != null)
            {
                return;
            }

            byId = new Dictionary<string, WeaponData>(StringComparer.Ordinal);
            bySlot = new Dictionary<int, WeaponData>();

            foreach (WeaponData weapon in weapons)
            {
                if (weapon == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(weapon.Id) && !byId.ContainsKey(weapon.Id))
                {
                    byId.Add(weapon.Id, weapon);
                }

                if (!bySlot.ContainsKey(weapon.ShopSlot))
                {
                    bySlot.Add(weapon.ShopSlot, weapon);
                }
            }
        }

        private void InvalidateCache()
        {
            byId = null;
            bySlot = null;
        }
    }
}
