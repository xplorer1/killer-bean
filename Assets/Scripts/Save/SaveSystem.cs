using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BeanBlaster.Combat;
using BeanBlaster.Core;
using UnityEngine;

namespace BeanBlaster.Save
{
    /// <summary>Loads, validates, atomically writes, and recovers the persistent game save.</summary>
    public static class SaveSystem
    {
        private const string MainFileName = "save.json";
        private const string TempFileName = "save.json.tmp";
        private const string BackupFileName = "save.json.bak";

        private static readonly object Sync = new object();
        private static SaveData current;
        private static string saveDirectoryOverride;
        private static Dictionary<string, int> testWeaponMaximums;
        private static int? testLevelCount;
        private static bool dirty;

        public static SaveData Current
        {
            get
            {
                lock (Sync)
                {
                    return current ??= LoadInternal();
                }
            }
        }

        public static bool IsDirty
        {
            get
            {
                lock (Sync)
                {
                    return dirty;
                }
            }
        }

        /// <summary>Loads and validates the current save, using backup recovery when necessary.</summary>
        public static SaveData Load()
        {
            lock (Sync)
            {
                current = LoadInternal();
                return current;
            }
        }

        /// <summary>Writes the current state atomically and reports whether persistence succeeded.</summary>
        public static bool Save()
        {
            lock (Sync)
            {
                current ??= LoadInternal();
                return SaveInternal(current);
            }
        }

        /// <summary>Marks the in-memory save as having changes that require persistence.</summary>
        public static void MarkDirty()
        {
            lock (Sync)
            {
                dirty = true;
            }
        }

        /// <summary>Replaces the in-memory state with defaults and immediately persists it.</summary>
        public static void ResetToDefaults()
        {
            lock (Sync)
            {
                current = CreateDefaults();
                dirty = true;
                SaveInternal(current);
            }
        }

        /// <summary>Validates and repairs a save without discarding forward-compatible weapon IDs.</summary>
        public static void Validate(SaveData data)
        {
            lock (Sync)
            {
                ValidateInternal(data);
            }
        }

        /// <summary>Redirects persistence to an isolated directory and clears cached state for tests.</summary>
        public static void SetSaveDirectoryForTests(string path)
        {
            lock (Sync)
            {
                saveDirectoryOverride = string.IsNullOrWhiteSpace(path) ? null : Path.GetFullPath(path);
                current = null;
                dirty = false;
            }
        }

        /// <summary>Injects validation metadata so tests do not depend on generated Resources assets.</summary>
        public static void SetValidationDataForTests(IDictionary<string, int> weaponMaximums, int totalLevelCount)
        {
            lock (Sync)
            {
                testWeaponMaximums = weaponMaximums == null
                    ? null
                    : new Dictionary<string, int>(weaponMaximums, StringComparer.Ordinal);
                testLevelCount = Mathf.Max(1, totalLevelCount);
                current = null;
                dirty = false;
            }
        }

        /// <summary>Clears all test-only overrides without touching files.</summary>
        public static void ClearTestOverrides()
        {
            lock (Sync)
            {
                saveDirectoryOverride = null;
                testWeaponMaximums = null;
                testLevelCount = null;
                current = null;
                dirty = false;
            }
        }

        private static SaveData LoadInternal()
        {
            string directory = GetSaveDirectory();
            Directory.CreateDirectory(directory);
            string mainPath = Path.Combine(directory, MainFileName);
            string backupPath = Path.Combine(directory, BackupFileName);

            if (TryLoadFile(mainPath, out SaveData loaded, out string mainError))
            {
                Debug.Log($"Loaded save from '{mainPath}'.");
                dirty = false;
                return loaded;
            }

            if (File.Exists(mainPath))
            {
                Debug.LogWarning($"Could not load primary save '{mainPath}': {mainError}");
                ArchiveCorruptFile(mainPath, directory);
                TryDeleteCorruptPrimary(mainPath);
            }

            if (TryLoadFile(backupPath, out loaded, out string backupError))
            {
                Debug.LogWarning($"Recovered save from backup '{backupPath}'.");
                current = loaded;
                dirty = true;
                SaveInternal(loaded);
                return loaded;
            }

            if (File.Exists(backupPath))
            {
                Debug.LogWarning($"Could not load backup save '{backupPath}': {backupError}");
            }

            SaveData defaults = CreateDefaults();
            current = defaults;
            dirty = true;
            Debug.Log("No valid save was found. Creating defaults.");
            SaveInternal(defaults);
            return defaults;
        }

        private static bool TryLoadFile(string path, out SaveData data, out string error)
        {
            data = null;
            error = string.Empty;
            if (!File.Exists(path))
            {
                error = "File does not exist.";
                return false;
            }

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json))
                {
                    error = "File is empty.";
                    return false;
                }

                data = JsonUtility.FromJson<SaveData>(json);
                if (data == null)
                {
                    error = "JSON produced no save data.";
                    return false;
                }

                Migrate(data);
                ValidateInternal(data);
                return true;
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is ArgumentException ||
                exception is NotSupportedException)
            {
                error = exception.Message;
                data = null;
                return false;
            }
        }

        private static bool SaveInternal(SaveData data)
        {
            string directory = GetSaveDirectory();
            string mainPath = Path.Combine(directory, MainFileName);
            string tempPath = Path.Combine(directory, TempFileName);
            string backupPath = Path.Combine(directory, BackupFileName);

            try
            {
                Directory.CreateDirectory(directory);
                ValidateInternal(data);
                data.lastSavedUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string json = JsonUtility.ToJson(data, true);

                byte[] bytes = new UTF8Encoding(false).GetBytes(json);
                using (FileStream stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                if (File.Exists(mainPath))
                {
                    ReplaceExistingSave(tempPath, mainPath, backupPath);
                }
                else
                {
                    File.Move(tempPath, mainPath);
                }

                dirty = false;
                return true;
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is ArgumentException ||
                exception is NotSupportedException)
            {
                Debug.LogError($"Failed to save game: {exception}");
                dirty = true;
                TryDeleteTemp(tempPath);
                return false;
            }
        }

        private static void ReplaceExistingSave(string tempPath, string mainPath, string backupPath)
        {
            try
            {
                File.Replace(tempPath, mainPath, backupPath);
            }
            catch (PlatformNotSupportedException)
            {
                FallbackReplace(tempPath, mainPath, backupPath);
            }
        }

        private static void FallbackReplace(string tempPath, string mainPath, string backupPath)
        {
            string stagedBackup = backupPath + ".staged";
            if (File.Exists(stagedBackup))
            {
                File.Delete(stagedBackup);
            }

            File.Copy(mainPath, stagedBackup, true);
            File.Copy(tempPath, mainPath, true);
            File.Copy(stagedBackup, backupPath, true);
            File.Delete(stagedBackup);
            File.Delete(tempPath);
        }

        private static void ValidateInternal(SaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            data.coins = Mathf.Max(0, data.coins);
            data.unlockedWeaponIDs ??= new List<string>();
            data.ammo ??= new List<AmmoEntry>();
            data.levelProgress ??= new LevelProgress();
            data.levelProgress.records ??= new List<LevelRecord>();
            data.settings ??= new SettingsData();

            data.unlockedWeaponIDs = data.unlockedWeaponIDs
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (!data.unlockedWeaponIDs.Contains(SaveData.DefaultWeaponId))
            {
                data.unlockedWeaponIDs.Insert(0, SaveData.DefaultWeaponId);
            }

            Dictionary<string, int> maximums = GetWeaponMaximums();
            bool equippedKnown = maximums.ContainsKey(data.equippedWeaponID);
            if (!data.IsUnlocked(data.equippedWeaponID) || !equippedKnown)
            {
                data.equippedWeaponID = SaveData.DefaultWeaponId;
            }

            Dictionary<string, int> mergedAmmo = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (AmmoEntry entry in data.ammo)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.weaponID))
                {
                    continue;
                }

                int amount = Mathf.Max(0, entry.amount);
                if (maximums.TryGetValue(entry.weaponID, out int maximum))
                {
                    amount = Mathf.Min(amount, Mathf.Max(0, maximum));
                }

                if (!mergedAmmo.TryGetValue(entry.weaponID, out int existing) || amount > existing)
                {
                    mergedAmmo[entry.weaponID] = amount;
                }
            }

            data.ammo = mergedAmmo
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new AmmoEntry { weaponID = pair.Key, amount = pair.Value })
                .ToList();

            int levelCount = GetTotalLevelCount();
            data.levelProgress.highestUnlockedLevel = Mathf.Clamp(
                data.levelProgress.highestUnlockedLevel,
                1,
                levelCount);
            data.levelProgress.records = data.levelProgress.records
                .Where(record => record != null && record.levelIndex >= 1 && record.levelIndex <= levelCount)
                .GroupBy(record => record.levelIndex)
                .Select(group => group.First())
                .OrderBy(record => record.levelIndex)
                .ToList();
            data.settings.masterVolume = Mathf.Clamp01(data.settings.masterVolume);
            data.settings.sfxVolume = Mathf.Clamp01(data.settings.sfxVolume);
        }

        private static void Migrate(SaveData data)
        {
            switch (data.saveVersion)
            {
                case SaveData.CurrentVersion:
                    break;
                default:
                    data.saveVersion = SaveData.CurrentVersion;
                    break;
            }
        }

        private static Dictionary<string, int> GetWeaponMaximums()
        {
            if (testWeaponMaximums != null)
            {
                Dictionary<string, int> copy = new Dictionary<string, int>(testWeaponMaximums, StringComparer.Ordinal);
                copy[SaveData.DefaultWeaponId] = int.MaxValue;
                return copy;
            }

            Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [SaveData.DefaultWeaponId] = int.MaxValue
            };
            WeaponDatabase database = Resources.Load<WeaponDatabase>("WeaponDatabase");
            if (database == null)
            {
                return result;
            }

            foreach (WeaponData weapon in database.All)
            {
                if (weapon != null && !string.IsNullOrWhiteSpace(weapon.Id))
                {
                    result[weapon.Id] = weapon.MaxAmmo;
                }
            }

            return result;
        }

        private static int GetTotalLevelCount()
        {
            if (testLevelCount.HasValue)
            {
                return Mathf.Max(1, testLevelCount.Value);
            }

            GameConfig config = Resources.Load<GameConfig>("GameConfig");
            return config == null ? 3 : Mathf.Max(1, config.TotalLevelCount);
        }

        private static SaveData CreateDefaults()
        {
            SaveData data = new SaveData();
            ValidateInternal(data);
            return data;
        }

        private static string GetSaveDirectory()
        {
            return saveDirectoryOverride ?? Application.persistentDataPath;
        }

        private static void ArchiveCorruptFile(string mainPath, string directory)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                string archivePath = Path.Combine(directory, $"save.corrupt_{timestamp}.json");
                int suffix = 1;
                while (File.Exists(archivePath))
                {
                    archivePath = Path.Combine(directory, $"save.corrupt_{timestamp}_{suffix}.json");
                    suffix++;
                }

                File.Copy(mainPath, archivePath, false);
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is ArgumentException ||
                exception is NotSupportedException)
            {
                Debug.LogWarning($"Could not archive corrupt save: {exception.Message}");
            }
        }

        private static void TryDeleteTemp(string tempPath)
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"Could not remove temporary save '{tempPath}': {exception.Message}");
            }
        }

        private static void TryDeleteCorruptPrimary(string mainPath)
        {
            try
            {
                File.Delete(mainPath);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"Could not remove corrupt primary save '{mainPath}': {exception.Message}");
            }
        }
    }
}
