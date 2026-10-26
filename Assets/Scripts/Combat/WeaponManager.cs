using System;
using System.Collections.Generic;
using System.Linq;
using BeanBlaster.Core;
using BeanBlaster.Economy;
using BeanBlaster.Player;
using BeanBlaster.Save;
using UnityEngine;

namespace BeanBlaster.Combat
{
    /// <summary>Owns weapon selection, persistent equip state, fire cadence, ammunition, and projectile spawning.</summary>
    public sealed class WeaponManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WeaponDatabase database;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Transform fireSocket;
        [SerializeField] private SpriteRenderer gunRenderer;
        [SerializeField] private PlayerController2D player;
        [SerializeField] private ProjectilePool projectilePool;
        [SerializeField] private Collider2D ownerCollider;

        [Header("Firing")]
        [SerializeField, Min(0f)] private float swapCooldown = 0.1f;
        [SerializeField, Min(0f)] private float dualMuzzleOffset = 0.08f;

        private WeaponData currentWeapon;
        private float fireCooldownRemaining;
        private bool emptyClickedThisPress;
        private bool useUpperDualMuzzle;

        public event Action<WeaponData> WeaponChanged;
        public event Action<WeaponData> Fired;
        public event Action EmptyClicked;
        public event Action<string, int> AmmoDisplayChanged;

        public WeaponData CurrentWeapon => currentWeapon;

        private void Awake()
        {
            database ??= Resources.Load<WeaponDatabase>("WeaponDatabase");
            input ??= GetComponent<PlayerInputReader>();
            player ??= GetComponent<PlayerController2D>();
            ownerCollider ??= GetComponent<Collider2D>();
        }

        private void OnEnable()
        {
            if (input != null)
            {
                input.NextWeaponPressed += EquipNext;
                input.PrevWeaponPressed += EquipPrevious;
                input.SelectWeaponPressed += EquipSlot;
            }

            EconomyService.AmmoChanged += OnAmmoChanged;
        }

        private void Start()
        {
            string savedWeapon = SaveSystem.Current.equippedWeaponID;
            if (!EquipInternal(savedWeapon, false))
            {
                EquipInternal(SaveData.DefaultWeaponId, true);
            }
        }

        private void OnDisable()
        {
            if (input != null)
            {
                input.NextWeaponPressed -= EquipNext;
                input.PrevWeaponPressed -= EquipPrevious;
                input.SelectWeaponPressed -= EquipSlot;
            }

            EconomyService.AmmoChanged -= OnAmmoChanged;
        }

        private void Update()
        {
            fireCooldownRemaining = Mathf.Max(0f, fireCooldownRemaining - Time.deltaTime);
            if (input == null || !input.FireHeld)
            {
                emptyClickedThisPress = false;
                return;
            }

            if (fireCooldownRemaining > 0f || currentWeapon == null || player == null || !player.IsAlive)
            {
                return;
            }

            TryFire();
        }

        /// <summary>Equips an owned weapon and updates persistent in-memory selection.</summary>
        public bool Equip(string weaponId)
        {
            return EquipInternal(weaponId, true);
        }

        private bool EquipInternal(string weaponId, bool persistSelection)
        {
            WeaponData weapon = database == null ? null : database.GetById(weaponId);
            if (weapon == null || !EconomyService.IsOwned(weapon.Id))
            {
                return false;
            }

            currentWeapon = weapon;
            fireCooldownRemaining = swapCooldown;
            emptyClickedThisPress = false;
            if (gunRenderer != null)
            {
                gunRenderer.sprite = weapon.Icon;
            }

            if (persistSelection && !string.Equals(SaveSystem.Current.equippedWeaponID, weapon.Id, StringComparison.Ordinal))
            {
                SaveSystem.Current.equippedWeaponID = weapon.Id;
                SaveSystem.MarkDirty();
            }

            WeaponChanged?.Invoke(weapon);
            RaiseAmmoDisplay();
            return true;
        }

        private void EquipNext()
        {
            CycleOwned(1);
        }

        private void EquipPrevious()
        {
            CycleOwned(-1);
        }

        private void EquipSlot(int zeroBasedSlot)
        {
            WeaponData weapon = database == null ? null : database.GetBySlot(zeroBasedSlot + 1);
            if (weapon != null)
            {
                Equip(weapon.Id);
            }
        }

        private void CycleOwned(int direction)
        {
            List<WeaponData> owned = GetOwnedWeapons();
            if (owned.Count == 0)
            {
                return;
            }

            int currentIndex = currentWeapon == null ? -1 : owned.FindIndex(weapon => weapon.Id == currentWeapon.Id);
            int nextIndex = currentIndex < 0
                ? 0
                : (currentIndex + direction + owned.Count) % owned.Count;
            Equip(owned[nextIndex].Id);
        }

        private List<WeaponData> GetOwnedWeapons()
        {
            return database == null
                ? new List<WeaponData>()
                : database.All
                    .Where(weapon => weapon != null && EconomyService.IsOwned(weapon.Id))
                    .OrderBy(weapon => weapon.ShopSlot)
                    .ToList();
        }

        private void TryFire()
        {
            if (currentWeapon.UsesAmmo && EconomyService.GetAmmo(currentWeapon.Id) < currentWeapon.AmmoPerShot)
            {
                RaiseEmptyClickOnce();
                return;
            }

            if (projectilePool == null || currentWeapon.ProjectilePrefab == null || fireSocket == null)
            {
                Debug.LogError($"Weapon '{currentWeapon.Id}' is missing its projectile pool, prefab, or fire socket.", this);
                return;
            }

            Projectile prefab = currentWeapon.ProjectilePrefab.GetComponent<Projectile>();
            if (prefab == null)
            {
                Debug.LogError($"Projectile prefab for '{currentWeapon.Id}' has no Projectile component.", currentWeapon.ProjectilePrefab);
                return;
            }

            Projectile projectile = projectilePool.Get(prefab);
            if (projectile == null)
            {
                return;
            }

            if (currentWeapon.UsesAmmo && !EconomyService.TryConsumeAmmo(currentWeapon.Id, currentWeapon.AmmoPerShot))
            {
                projectilePool.Return(projectile);
                RaiseEmptyClickOnce();
                return;
            }

            Vector2 aimDirection = input.AimDirection;
            Vector3 spawnPosition = fireSocket.position;
            if (currentWeapon.AmmoType == AmmoType.Standard && currentWeapon.Id == SaveData.DefaultWeaponId)
            {
                Vector2 perpendicular = new Vector2(-aimDirection.y, aimDirection.x);
                spawnPosition += (Vector3)(perpendicular * (useUpperDualMuzzle ? dualMuzzleOffset : -dualMuzzleOffset));
                useUpperDualMuzzle = !useUpperDualMuzzle;
            }

            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            projectile.transform.SetPositionAndRotation(spawnPosition, Quaternion.Euler(0f, 0f, angle));
            if (!projectile.Launch(currentWeapon, aimDirection, Team.Player, ownerCollider))
            {
                if (currentWeapon.UsesAmmo)
                {
                    EconomyService.AddAmmo(currentWeapon.Id, currentWeapon.AmmoPerShot);
                }

                projectilePool.Return(projectile);
                return;
            }

            fireCooldownRemaining = currentWeapon.FireRate;
            Fired?.Invoke(currentWeapon);
            RaiseAmmoDisplay();
        }

        private void RaiseEmptyClickOnce()
        {
            if (emptyClickedThisPress)
            {
                return;
            }

            emptyClickedThisPress = true;
            EmptyClicked?.Invoke();
        }

        private void OnAmmoChanged(string weaponId, int amount)
        {
            if (currentWeapon != null && string.Equals(currentWeapon.Id, weaponId, StringComparison.Ordinal))
            {
                AmmoDisplayChanged?.Invoke(weaponId, EconomyService.GetAmmo(weaponId));
            }
        }

        private void RaiseAmmoDisplay()
        {
            if (currentWeapon != null)
            {
                AmmoDisplayChanged?.Invoke(currentWeapon.Id, EconomyService.GetAmmo(currentWeapon.Id));
            }
        }

        private void OnValidate()
        {
            swapCooldown = Mathf.Max(0f, swapCooldown);
            dualMuzzleOffset = Mathf.Max(0f, dualMuzzleOffset);
        }
    }
}
