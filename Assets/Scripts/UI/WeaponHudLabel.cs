using BeanBlaster.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace BeanBlaster.UI
{
    /// <summary>Provides immediate visible confirmation when the equipped weapon or ammunition changes.</summary>
    public sealed class WeaponHudLabel : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private WeaponManager weaponManager;

        private WeaponData displayedWeapon;
        private int displayedAmmo = int.MaxValue;

        public void Configure(Text targetLabel, WeaponManager manager)
        {
            Unsubscribe();
            label = targetLabel;
            weaponManager = manager;
            Subscribe();
            if (weaponManager != null && weaponManager.CurrentWeapon != null)
            {
                OnWeaponChanged(weaponManager.CurrentWeapon);
            }
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (weaponManager == null)
            {
                return;
            }

            weaponManager.WeaponChanged -= OnWeaponChanged;
            weaponManager.AmmoDisplayChanged -= OnAmmoChanged;
            weaponManager.WeaponChanged += OnWeaponChanged;
            weaponManager.AmmoDisplayChanged += OnAmmoChanged;
        }

        private void Unsubscribe()
        {
            if (weaponManager == null)
            {
                return;
            }

            weaponManager.WeaponChanged -= OnWeaponChanged;
            weaponManager.AmmoDisplayChanged -= OnAmmoChanged;
        }

        private void OnWeaponChanged(WeaponData weapon)
        {
            displayedWeapon = weapon;
            displayedAmmo = weapon != null && weapon.UsesAmmo ? 0 : int.MaxValue;
            Refresh();
        }

        private void OnAmmoChanged(string weaponId, int amount)
        {
            if (displayedWeapon == null || displayedWeapon.Id != weaponId)
            {
                return;
            }

            displayedAmmo = amount;
            Refresh();
        }

        private void Refresh()
        {
            if (label == null)
            {
                return;
            }

            if (displayedWeapon == null)
            {
                label.text = "WEAPON: --";
                return;
            }

            string ammo = displayedAmmo == int.MaxValue ? "UNLIMITED" : displayedAmmo.ToString();
            label.text = $"{displayedWeapon.DisplayName.ToUpperInvariant()}  ·  {ammo}";
        }
    }
}
