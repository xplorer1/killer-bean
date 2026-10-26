using System;
using BeanBlaster.Combat;
using BeanBlaster.Core;
using BeanBlaster.Save;
using UnityEngine;

namespace BeanBlaster.Economy
{
    /// <summary>Provides the only supported mutation path for persistent coins and ammunition.</summary>
    public static class EconomyService
    {
        private static WeaponDatabase weaponDatabase;
        private static GameConfig gameConfig;

        public static event Action<int> CoinsChanged;
        public static event Action<string, int> AmmoChanged;

        public static int Coins => SaveSystem.Current.coins;

        /// <summary>Configures runtime data dependencies.</summary>
        public static void Initialize(WeaponDatabase database, GameConfig config)
        {
            weaponDatabase = database;
            gameConfig = config;
        }

        /// <summary>Adds a non-negative coin award to the in-memory save immediately.</summary>
        public static void AddCoins(int amount, CoinSource source)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"Rejected negative coin award {amount} from {source}.");
                return;
            }

            if (amount == 0)
            {
                return;
            }

            SaveData save = SaveSystem.Current;
            save.coins = (int)Math.Min(int.MaxValue, (long)save.coins + amount);
            SaveSystem.MarkDirty();
            CoinsChanged?.Invoke(save.coins);
        }

        /// <summary>Spends coins when the amount is valid and affordable.</summary>
        public static bool TrySpendCoins(int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"Rejected negative coin spend {amount}.");
                return false;
            }

            SaveData save = SaveSystem.Current;
            if (save.coins < amount)
            {
                return false;
            }

            if (amount == 0)
            {
                return true;
            }

            save.coins -= amount;
            SaveSystem.MarkDirty();
            CoinsChanged?.Invoke(save.coins);
            return true;
        }

        /// <summary>Returns stored ammo, or int.MaxValue when the configured weapon is unlimited.</summary>
        public static int GetAmmo(string weaponId)
        {
            WeaponData weapon = GetWeapon(weaponId);
            if (weapon == null)
            {
                return 0;
            }

            if (!weapon.UsesAmmo || IsUnlimitedForOwnedWeapon(weaponId))
            {
                return int.MaxValue;
            }

            return SaveSystem.Current.GetAmmo(weaponId);
        }

        /// <summary>Adds ammunition to a known ammo-using weapon and clamps it to the reserve maximum.</summary>
        public static void AddAmmo(string weaponId, int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"Rejected negative ammo award {amount} for '{weaponId}'.");
                return;
            }

            WeaponData weapon = GetWeapon(weaponId);
            if (weapon == null || !weapon.UsesAmmo || amount == 0)
            {
                return;
            }

            SaveData save = SaveSystem.Current;
            int updated = Mathf.Clamp(save.GetAmmo(weaponId) + amount, 0, weapon.MaxAmmo);
            if (updated == save.GetAmmo(weaponId))
            {
                return;
            }

            save.SetAmmo(weaponId, updated);
            SaveSystem.MarkDirty();
            AmmoChanged?.Invoke(weaponId, updated);
        }

        /// <summary>Consumes ammunition when available, respecting unlimited-ammo configuration.</summary>
        public static bool TryConsumeAmmo(string weaponId, int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"Rejected negative ammo consumption {amount} for '{weaponId}'.");
                return false;
            }

            WeaponData weapon = GetWeapon(weaponId);
            if (weapon == null)
            {
                return false;
            }

            if (!weapon.UsesAmmo || IsUnlimitedForOwnedWeapon(weaponId) || amount == 0)
            {
                return true;
            }

            SaveData save = SaveSystem.Current;
            int available = save.GetAmmo(weaponId);
            if (available < amount)
            {
                return false;
            }

            int updated = available - amount;
            save.SetAmmo(weaponId, updated);
            SaveSystem.MarkDirty();
            AmmoChanged?.Invoke(weaponId, updated);
            return true;
        }

        /// <summary>Reports permanent ownership of a weapon ID.</summary>
        public static bool IsOwned(string weaponId)
        {
            return SaveSystem.Current.IsUnlocked(weaponId);
        }

        internal static void NotifyStateRestored(string weaponId)
        {
            CoinsChanged?.Invoke(SaveSystem.Current.coins);
            if (!string.IsNullOrEmpty(weaponId))
            {
                AmmoChanged?.Invoke(weaponId, SaveSystem.Current.GetAmmo(weaponId));
            }
        }

        private static bool IsUnlimitedForOwnedWeapon(string weaponId)
        {
            return gameConfig != null &&
                   gameConfig.UnlimitedAmmoForOwnedWeapons &&
                   IsOwned(weaponId);
        }

        private static WeaponData GetWeapon(string weaponId)
        {
            EnsureDependencies();
            return weaponDatabase == null ? null : weaponDatabase.GetById(weaponId);
        }

        private static void EnsureDependencies()
        {
            if (weaponDatabase == null)
            {
                weaponDatabase = Resources.Load<WeaponDatabase>("WeaponDatabase");
            }

            if (gameConfig == null)
            {
                gameConfig = Resources.Load<GameConfig>("GameConfig");
            }
        }
    }
}
