using System;
using System.Collections.Generic;

namespace BeanBlaster.Save
{
    /// <summary>Stores the complete persistent player state in a JsonUtility-compatible shape.</summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;
        public const string DefaultWeaponId = "pistol_dual";

        public int saveVersion = CurrentVersion;
        public int coins;
        public List<string> unlockedWeaponIDs = new List<string> { DefaultWeaponId };
        public string equippedWeaponID = DefaultWeaponId;
        public List<AmmoEntry> ammo = new List<AmmoEntry>();
        public LevelProgress levelProgress = new LevelProgress();
        public long lastSavedUnixTime;
        public SettingsData settings = new SettingsData();

        /// <summary>Returns the stored reserve for a weapon, or zero when no entry exists.</summary>
        public int GetAmmo(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId) || ammo == null)
            {
                return 0;
            }

            for (int i = 0; i < ammo.Count; i++)
            {
                AmmoEntry entry = ammo[i];
                if (entry != null && string.Equals(entry.weaponID, weaponId, StringComparison.Ordinal))
                {
                    return entry.amount;
                }
            }

            return 0;
        }

        /// <summary>Creates or updates a weapon's stored ammunition reserve.</summary>
        public void SetAmmo(string weaponId, int amount)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                throw new ArgumentException("A weapon ID is required.", nameof(weaponId));
            }

            ammo ??= new List<AmmoEntry>();
            for (int i = 0; i < ammo.Count; i++)
            {
                AmmoEntry entry = ammo[i];
                if (entry != null && string.Equals(entry.weaponID, weaponId, StringComparison.Ordinal))
                {
                    entry.amount = amount;
                    return;
                }
            }

            ammo.Add(new AmmoEntry { weaponID = weaponId, amount = amount });
        }

        /// <summary>Reports whether a weapon is permanently unlocked.</summary>
        public bool IsUnlocked(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId) || unlockedWeaponIDs == null)
            {
                return false;
            }

            for (int i = 0; i < unlockedWeaponIDs.Count; i++)
            {
                if (string.Equals(unlockedWeaponIDs[i], weaponId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Permanently unlocks a weapon and reports whether the collection changed.</summary>
        public bool Unlock(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return false;
            }

            unlockedWeaponIDs ??= new List<string>();
            if (IsUnlocked(weaponId))
            {
                return false;
            }

            unlockedWeaponIDs.Add(weaponId);
            return true;
        }

        /// <summary>Returns an existing level record or creates one for the supplied one-based index.</summary>
        public LevelRecord GetOrCreateRecord(int levelIndex)
        {
            if (levelIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(levelIndex), "Level indices are one-based.");
            }

            levelProgress ??= new LevelProgress();
            levelProgress.records ??= new List<LevelRecord>();
            for (int i = 0; i < levelProgress.records.Count; i++)
            {
                LevelRecord record = levelProgress.records[i];
                if (record != null && record.levelIndex == levelIndex)
                {
                    return record;
                }
            }

            LevelRecord created = new LevelRecord { levelIndex = levelIndex };
            levelProgress.records.Add(created);
            return created;
        }
    }

}
