using System.Collections.Generic;
using System.IO;
using BeanBlaster.Combat;
using BeanBlaster.Core;
using BeanBlaster.Economy;
using BeanBlaster.Save;
using BeanBlaster.UI;
using NUnit.Framework;
using UnityEngine;

namespace BeanBlaster.Tests.EditMode
{
    public sealed class ShopManagerTests
    {
        private string directory;
        private WeaponDatabase database;
        private GameConfig config;
        private WeaponData pistol;
        private WeaponData magnum;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "BeanBlasterShopTests", System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            SaveSystem.SetSaveDirectoryForTests(directory);
            SaveSystem.SetValidationDataForTests(new Dictionary<string, int>
            {
                ["pistol_dual"] = int.MaxValue,
                ["magnum"] = 90
            }, 3);
            pistol = EconomyServiceTests.CreateWeapon("pistol_dual", 0, false, 0, 0, 0, 0);
            magnum = EconomyServiceTests.CreateWeapon("magnum", 2500, true, 1, 30, 90, 30);
            database = ScriptableObject.CreateInstance<WeaponDatabase>();
            EconomyServiceTests.SetField(database, "weapons", new List<WeaponData> { pistol, magnum });
            config = ScriptableObject.CreateInstance<GameConfig>();
            EconomyService.Initialize(database, config);
            SaveSystem.Load();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(pistol);
            Object.DestroyImmediate(magnum);
            Object.DestroyImmediate(database);
            Object.DestroyImmediate(config);
            SaveSystem.ClearTestOverrides();
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void InsufficientCoinsChangesNothing()
        {
            SaveSystem.Current.coins = 2499;
            ShopManager shop = new ShopManager(database, SaveSystem.Save);

            PurchaseResult result = shop.PurchaseWeapon(magnum);

            Assert.That(result, Is.EqualTo(PurchaseResult.InsufficientCoins));
            Assert.That(SaveSystem.Current.coins, Is.EqualTo(2499));
            Assert.That(SaveSystem.Current.IsUnlocked("magnum"), Is.False);
            Assert.That(SaveSystem.Current.GetAmmo("magnum"), Is.Zero);
            Assert.That(SaveSystem.Current.equippedWeaponID, Is.EqualTo("pistol_dual"));
        }

        [Test]
        public void SuccessfulPurchaseDeductsUnlocksGrantsEquipsAndPersists()
        {
            SaveSystem.Current.coins = 3000;
            ShopManager shop = new ShopManager(database, SaveSystem.Save);

            PurchaseResult result = shop.PurchaseWeapon(magnum);
            SaveData reloaded = SaveSystem.Load();

            Assert.That(result, Is.EqualTo(PurchaseResult.Success));
            Assert.That(reloaded.coins, Is.EqualTo(500));
            Assert.That(reloaded.IsUnlocked("magnum"), Is.True);
            Assert.That(reloaded.GetAmmo("magnum"), Is.EqualTo(30));
            Assert.That(reloaded.equippedWeaponID, Is.EqualTo("magnum"));
        }

        [Test]
        public void AlreadyOwnedChangesNothing()
        {
            SaveSystem.Current.coins = 3000;
            SaveSystem.Current.Unlock("magnum");
            SaveSystem.Current.SetAmmo("magnum", 7);
            ShopManager shop = new ShopManager(database, SaveSystem.Save);

            PurchaseResult result = shop.PurchaseWeapon(magnum);

            Assert.That(result, Is.EqualTo(PurchaseResult.AlreadyOwned));
            Assert.That(SaveSystem.Current.coins, Is.EqualTo(3000));
            Assert.That(SaveSystem.Current.GetAmmo("magnum"), Is.EqualTo(7));
        }

        [Test]
        public void FailedWeaponSaveRollsBackEveryMutation()
        {
            SaveSystem.Current.coins = 3000;
            ShopManager shop = new ShopManager(database, () => false);

            PurchaseResult result = shop.PurchaseWeapon(magnum);

            Assert.That(result, Is.EqualTo(PurchaseResult.SaveFailed));
            Assert.That(SaveSystem.Current.coins, Is.EqualTo(3000));
            Assert.That(SaveSystem.Current.IsUnlocked("magnum"), Is.False);
            Assert.That(SaveSystem.Current.GetAmmo("magnum"), Is.Zero);
            Assert.That(SaveSystem.Current.equippedWeaponID, Is.EqualTo("pistol_dual"));
        }

        [Test]
        public void AmmoPurchaseClampsAndThenReportsFull()
        {
            SaveSystem.Current.coins = 1000;
            SaveSystem.Current.Unlock("magnum");
            SaveSystem.Current.SetAmmo("magnum", 80);
            ShopManager shop = new ShopManager(database, SaveSystem.Save);

            Assert.That(shop.PurchaseAmmo(magnum), Is.EqualTo(AmmoPurchaseResult.Success));
            Assert.That(SaveSystem.Current.GetAmmo("magnum"), Is.EqualTo(90));
            Assert.That(SaveSystem.Current.coins, Is.EqualTo(850));
            Assert.That(shop.PurchaseAmmo(magnum), Is.EqualTo(AmmoPurchaseResult.AmmoFull));
            Assert.That(SaveSystem.Current.coins, Is.EqualTo(850));
        }

        [Test]
        public void OwnershipAndAmmoSurviveRepeatedCompletionLikeSaves()
        {
            SaveSystem.Current.Unlock("magnum");
            SaveSystem.Current.SetAmmo("magnum", 44);
            for (int i = 0; i < 10; i++)
            {
                LevelRecord record = SaveSystem.Current.GetOrCreateRecord((i % 3) + 1);
                record.completed = true;
                Assert.That(SaveSystem.Save(), Is.True);
                SaveSystem.Load();
            }

            Assert.That(SaveSystem.Current.IsUnlocked("magnum"), Is.True);
            Assert.That(SaveSystem.Current.GetAmmo("magnum"), Is.EqualTo(44));
        }
    }
}
