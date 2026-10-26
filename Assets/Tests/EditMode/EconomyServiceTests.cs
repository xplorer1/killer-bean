using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BeanBlaster.Combat;
using BeanBlaster.Core;
using BeanBlaster.Economy;
using BeanBlaster.Save;
using NUnit.Framework;
using UnityEngine;

namespace BeanBlaster.Tests.EditMode
{
    public sealed class EconomyServiceTests
    {
        private string directory;
        private WeaponDatabase database;
        private GameConfig config;
        private WeaponData pistol;
        private WeaponData magnum;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "BeanBlasterEconomyTests", System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            SaveSystem.SetSaveDirectoryForTests(directory);
            SaveSystem.SetValidationDataForTests(new Dictionary<string, int>
            {
                ["pistol_dual"] = int.MaxValue,
                ["magnum"] = 90
            }, 3);

            pistol = CreateWeapon("pistol_dual", 0, false, 0, 0, 0, 0);
            magnum = CreateWeapon("magnum", 2500, true, 1, 30, 90, 30);
            database = ScriptableObject.CreateInstance<WeaponDatabase>();
            SetField(database, "weapons", new List<WeaponData> { pistol, magnum });
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
        public void TryConsumeAmmoFailsAtZeroAndNeverGoesNegative()
        {
            SaveSystem.Current.Unlock("magnum");

            Assert.That(EconomyService.TryConsumeAmmo("magnum", 1), Is.False);
            Assert.That(SaveSystem.Current.GetAmmo("magnum"), Is.Zero);
        }

        [Test]
        public void AddAmmoClampsToMaximumReserve()
        {
            EconomyService.AddAmmo("magnum", 500);

            Assert.That(SaveSystem.Current.GetAmmo("magnum"), Is.EqualTo(90));
        }

        [Test]
        public void UnlimitedOwnedWeaponConsumesNoStoredAmmo()
        {
            SaveSystem.Current.Unlock("magnum");
            SaveSystem.Current.SetAmmo("magnum", 12);
            SetField(config, "unlimitedAmmoForOwnedWeapons", true);

            Assert.That(EconomyService.TryConsumeAmmo("magnum", 5), Is.True);
            Assert.That(SaveSystem.Current.GetAmmo("magnum"), Is.EqualTo(12));
            Assert.That(EconomyService.GetAmmo("magnum"), Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void NegativeMutationsChangeNothing()
        {
            SaveSystem.Current.coins = 50;
            SaveSystem.Current.SetAmmo("magnum", 10);

            EconomyService.AddCoins(-10, CoinSource.Debug);
            EconomyService.AddAmmo("magnum", -4);

            Assert.That(EconomyService.TrySpendCoins(-1), Is.False);
            Assert.That(EconomyService.TryConsumeAmmo("magnum", -1), Is.False);
            Assert.That(SaveSystem.Current.coins, Is.EqualTo(50));
            Assert.That(SaveSystem.Current.GetAmmo("magnum"), Is.EqualTo(10));
        }

        internal static WeaponData CreateWeapon(
            string id,
            int cost,
            bool usesAmmo,
            int ammoPerShot,
            int startingAmmo,
            int maxAmmo,
            int ammoPackSize,
            int ammoPackCost = 150)
        {
            WeaponData weapon = ScriptableObject.CreateInstance<WeaponData>();
            SetField(weapon, "id", id);
            SetField(weapon, "displayName", id);
            SetField(weapon, "cost", cost);
            SetField(weapon, "usesAmmo", usesAmmo);
            SetField(weapon, "ammoPerShot", ammoPerShot);
            SetField(weapon, "startingAmmo", startingAmmo);
            SetField(weapon, "maxAmmo", maxAmmo);
            SetField(weapon, "ammoPackSize", ammoPackSize);
            SetField(weapon, "ammoPackCost", ammoPackCost);
            return weapon;
        }

        internal static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing test field '{name}'.");
            field.SetValue(target, value);
        }
    }
}
