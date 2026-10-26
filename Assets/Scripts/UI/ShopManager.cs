using System;
using BeanBlaster.Combat;
using BeanBlaster.Economy;
using BeanBlaster.Save;
using UnityEngine;

namespace BeanBlaster.UI
{
    public enum PurchaseResult
    {
        Success,
        AlreadyOwned,
        InsufficientCoins,
        InvalidWeapon,
        NotPurchasable,
        SaveFailed
    }

    public enum AmmoPurchaseResult
    {
        Success,
        NotOwned,
        InsufficientCoins,
        AmmoFull,
        WeaponHasNoAmmo,
        InvalidWeapon,
        SaveFailed
    }

    public enum WeaponState
    {
        Locked,
        Unlocked,
        Equipped
    }

    /// <summary>Executes validated, rollback-safe weapon and ammunition transactions.</summary>
    public sealed class ShopManager
    {
        private readonly WeaponDatabase database;
        private readonly Func<bool> persist;

        public event Action<WeaponData, PurchaseResult> WeaponPurchaseAttempted;
        public event Action<WeaponData, AmmoPurchaseResult> AmmoPurchaseAttempted;

        /// <summary>Creates a shop service using Resources data and real save persistence.</summary>
        public ShopManager()
            : this(Resources.Load<WeaponDatabase>("WeaponDatabase"), SaveSystem.Save)
        {
        }

        /// <summary>Creates a shop service with explicit dependencies.</summary>
        public ShopManager(WeaponDatabase database, Func<bool> persist)
        {
            this.database = database;
            this.persist = persist ?? throw new ArgumentNullException(nameof(persist));
        }

        /// <summary>Purchases, grants ammunition for, equips, and persists a weapon atomically.</summary>
        public PurchaseResult PurchaseWeapon(WeaponData weapon)
        {
            PurchaseResult validation = ValidateWeaponPurchase(weapon);
            if (validation != PurchaseResult.Success)
            {
                WeaponPurchaseAttempted?.Invoke(weapon, validation);
                return validation;
            }

            SaveData save = SaveSystem.Current;
            int oldCoins = save.coins;
            string oldEquipped = save.equippedWeaponID;
            int oldAmmo = save.GetAmmo(weapon.Id);

            save.coins -= weapon.Cost;
            save.Unlock(weapon.Id);
            save.SetAmmo(weapon.Id, Mathf.Clamp(oldAmmo + weapon.StartingAmmo, 0, weapon.MaxAmmo));
            save.equippedWeaponID = weapon.Id;
            SaveSystem.MarkDirty();

            PurchaseResult result = PurchaseResult.Success;
            if (!persist())
            {
                save.coins = oldCoins;
                save.unlockedWeaponIDs.RemoveAll(id => string.Equals(id, weapon.Id, StringComparison.Ordinal));
                save.SetAmmo(weapon.Id, oldAmmo);
                save.equippedWeaponID = oldEquipped;
                SaveSystem.MarkDirty();
                EconomyService.NotifyStateRestored(weapon.Id);
                result = PurchaseResult.SaveFailed;
            }

            WeaponPurchaseAttempted?.Invoke(weapon, result);
            return result;
        }

        /// <summary>Purchases one ammunition pack and persists it atomically.</summary>
        public AmmoPurchaseResult PurchaseAmmo(WeaponData weapon)
        {
            AmmoPurchaseResult validation = ValidateAmmoPurchase(weapon);
            if (validation != AmmoPurchaseResult.Success)
            {
                AmmoPurchaseAttempted?.Invoke(weapon, validation);
                return validation;
            }

            SaveData save = SaveSystem.Current;
            int oldCoins = save.coins;
            int oldAmmo = save.GetAmmo(weapon.Id);
            save.coins -= weapon.AmmoPackCost;
            save.SetAmmo(weapon.Id, Mathf.Min(weapon.MaxAmmo, oldAmmo + weapon.AmmoPackSize));
            SaveSystem.MarkDirty();

            AmmoPurchaseResult result = AmmoPurchaseResult.Success;
            if (!persist())
            {
                save.coins = oldCoins;
                save.SetAmmo(weapon.Id, oldAmmo);
                SaveSystem.MarkDirty();
                EconomyService.NotifyStateRestored(weapon.Id);
                result = AmmoPurchaseResult.SaveFailed;
            }

            AmmoPurchaseAttempted?.Invoke(weapon, result);
            return result;
        }

        /// <summary>Equips an owned weapon and immediately persists the selection.</summary>
        public bool EquipWeapon(WeaponData weapon)
        {
            if (!IsKnown(weapon) || !EconomyService.IsOwned(weapon.Id))
            {
                return false;
            }

            SaveData save = SaveSystem.Current;
            string previous = save.equippedWeaponID;
            save.equippedWeaponID = weapon.Id;
            SaveSystem.MarkDirty();
            if (persist())
            {
                return true;
            }

            save.equippedWeaponID = previous;
            SaveSystem.MarkDirty();
            return false;
        }

        /// <summary>Returns the locked, unlocked, or equipped presentation state for a weapon.</summary>
        public WeaponState GetState(WeaponData weapon)
        {
            if (!IsKnown(weapon) || !EconomyService.IsOwned(weapon.Id))
            {
                return WeaponState.Locked;
            }

            return string.Equals(SaveSystem.Current.equippedWeaponID, weapon.Id, StringComparison.Ordinal)
                ? WeaponState.Equipped
                : WeaponState.Unlocked;
        }

        private PurchaseResult ValidateWeaponPurchase(WeaponData weapon)
        {
            if (!IsKnown(weapon))
            {
                return PurchaseResult.InvalidWeapon;
            }

            if (EconomyService.IsOwned(weapon.Id))
            {
                return PurchaseResult.AlreadyOwned;
            }

            if (weapon.Cost <= 0 && !string.Equals(weapon.Id, SaveData.DefaultWeaponId, StringComparison.Ordinal))
            {
                return PurchaseResult.NotPurchasable;
            }

            return EconomyService.Coins < weapon.Cost
                ? PurchaseResult.InsufficientCoins
                : PurchaseResult.Success;
        }

        private AmmoPurchaseResult ValidateAmmoPurchase(WeaponData weapon)
        {
            if (!IsKnown(weapon))
            {
                return AmmoPurchaseResult.InvalidWeapon;
            }

            if (!EconomyService.IsOwned(weapon.Id))
            {
                return AmmoPurchaseResult.NotOwned;
            }

            if (!weapon.UsesAmmo)
            {
                return AmmoPurchaseResult.WeaponHasNoAmmo;
            }

            if (SaveSystem.Current.GetAmmo(weapon.Id) >= weapon.MaxAmmo)
            {
                return AmmoPurchaseResult.AmmoFull;
            }

            return EconomyService.Coins < weapon.AmmoPackCost
                ? AmmoPurchaseResult.InsufficientCoins
                : AmmoPurchaseResult.Success;
        }

        private bool IsKnown(WeaponData weapon)
        {
            return weapon != null && database != null && ReferenceEquals(database.GetById(weapon.Id), weapon);
        }
    }
}
