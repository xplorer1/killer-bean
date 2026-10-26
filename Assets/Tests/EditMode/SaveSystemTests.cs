using System.Collections.Generic;
using System.IO;
using BeanBlaster.Save;
using NUnit.Framework;
using UnityEngine;

namespace BeanBlaster.Tests.EditMode
{
    public sealed class SaveSystemTests
    {
        private string directory;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "BeanBlasterTests", System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            SaveSystem.SetSaveDirectoryForTests(directory);
            SaveSystem.SetValidationDataForTests(
                new Dictionary<string, int>
                {
                    ["pistol_dual"] = int.MaxValue,
                    ["magnum"] = 90,
                    ["explosive_gl"] = 30,
                    ["beam_splitter"] = 150,
                    ["fire_bomb"] = 15
                },
                3);
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.ClearTestOverrides();
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void NoFileCreatesDefaults()
        {
            SaveData data = SaveSystem.Load();

            Assert.That(data.coins, Is.Zero);
            Assert.That(data.unlockedWeaponIDs, Is.EqualTo(new[] { "pistol_dual" }));
            Assert.That(data.equippedWeaponID, Is.EqualTo("pistol_dual"));
            Assert.That(File.Exists(Path.Combine(directory, "save.json")), Is.True);
        }

        [Test]
        public void SaveThenLoadRoundTripsAllFields()
        {
            SaveData data = SaveSystem.Current;
            data.coins = 4321;
            data.Unlock("magnum");
            data.equippedWeaponID = "magnum";
            data.SetAmmo("magnum", 27);
            data.levelProgress.highestUnlockedLevel = 3;
            LevelRecord record = data.GetOrCreateRecord(2);
            record.completed = true;
            record.bestTimeSeconds = 42.5f;
            record.noDeathClear = true;
            data.settings.masterVolume = 0.75f;
            data.settings.sfxVolume = 0.5f;
            data.settings.screenShake = false;

            Assert.That(SaveSystem.Save(), Is.True);
            SaveData loaded = SaveSystem.Load();

            Assert.That(loaded.coins, Is.EqualTo(4321));
            Assert.That(loaded.IsUnlocked("magnum"), Is.True);
            Assert.That(loaded.equippedWeaponID, Is.EqualTo("magnum"));
            Assert.That(loaded.GetAmmo("magnum"), Is.EqualTo(27));
            Assert.That(loaded.levelProgress.highestUnlockedLevel, Is.EqualTo(3));
            LevelRecord loadedRecord = loaded.GetOrCreateRecord(2);
            Assert.That(loadedRecord.completed, Is.True);
            Assert.That(loadedRecord.bestTimeSeconds, Is.EqualTo(42.5f));
            Assert.That(loadedRecord.noDeathClear, Is.True);
            Assert.That(loaded.settings.masterVolume, Is.EqualTo(0.75f));
            Assert.That(loaded.settings.sfxVolume, Is.EqualTo(0.5f));
            Assert.That(loaded.settings.screenShake, Is.False);
        }

        [Test]
        public void CorruptMainRecoversValidBackupAndArchivesCorruptFile()
        {
            SaveData data = SaveSystem.Current;
            data.coins = 125;
            Assert.That(SaveSystem.Save(), Is.True);
            data.coins = 250;
            Assert.That(SaveSystem.Save(), Is.True);

            File.WriteAllText(Path.Combine(directory, "save.json"), "not-json");
            SaveData recovered = SaveSystem.Load();

            Assert.That(recovered.coins, Is.EqualTo(125));
            Assert.That(Directory.GetFiles(directory, "save.corrupt_*.json").Length, Is.EqualTo(1));
        }

        [Test]
        public void CorruptMainAndBackupReturnDefaultsWithoutException()
        {
            File.WriteAllText(Path.Combine(directory, "save.json"), "garbage");
            File.WriteAllText(Path.Combine(directory, "save.json.bak"), "also-garbage");

            SaveData loaded = null;
            Assert.DoesNotThrow(() => loaded = SaveSystem.Load());
            Assert.That(loaded.coins, Is.Zero);
            Assert.That(loaded.IsUnlocked("pistol_dual"), Is.True);
        }

        [Test]
        public void ValidationRepairsCoreInvariantsAndClampsKnownAmmo()
        {
            SaveData data = new SaveData
            {
                coins = -99,
                unlockedWeaponIDs = new List<string> { "magnum", "magnum", "future_weapon" },
                equippedWeaponID = "not_owned",
                ammo = new List<AmmoEntry>
                {
                    new AmmoEntry { weaponID = "magnum", amount = 500 },
                    new AmmoEntry { weaponID = "magnum", amount = 40 },
                    new AmmoEntry { weaponID = "future_weapon", amount = 12 }
                },
                levelProgress = new LevelProgress { highestUnlockedLevel = 99 }
            };

            SaveSystem.Validate(data);

            Assert.That(data.coins, Is.Zero);
            Assert.That(data.IsUnlocked("pistol_dual"), Is.True);
            Assert.That(data.unlockedWeaponIDs.FindAll(id => id == "magnum").Count, Is.EqualTo(1));
            Assert.That(data.IsUnlocked("future_weapon"), Is.True);
            Assert.That(data.equippedWeaponID, Is.EqualTo("pistol_dual"));
            Assert.That(data.GetAmmo("magnum"), Is.EqualTo(90));
            Assert.That(data.GetAmmo("future_weapon"), Is.EqualTo(12));
            Assert.That(data.levelProgress.highestUnlockedLevel, Is.EqualTo(3));
        }

        [Test]
        public void DirtyFlagClearsOnlyAfterSuccessfulSave()
        {
            SaveSystem.Load();
            SaveSystem.MarkDirty();
            Assert.That(SaveSystem.IsDirty, Is.True);

            Assert.That(SaveSystem.Save(), Is.True);
            Assert.That(SaveSystem.IsDirty, Is.False);
        }
    }
}
