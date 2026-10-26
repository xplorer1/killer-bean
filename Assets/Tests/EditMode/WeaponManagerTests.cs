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
    public sealed class WeaponManagerTests
    {
        private string directory;
        private GameObject managerObject;
        private WeaponManager manager;
        private WeaponDatabase database;
        private GameConfig config;
        private WeaponData pistol;
        private WeaponData magnum;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "BeanBlasterWeaponManagerTests", System.Guid.NewGuid().ToString("N"));
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

            managerObject = new GameObject("Weapon Manager Test");
            managerObject.SetActive(false);
            manager = managerObject.AddComponent<WeaponManager>();
            SetField("database", database);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(managerObject);
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
        public void EquipRejectsUnownedWeaponWithoutChangingSelection()
        {
            Assert.That(manager.Equip("magnum"), Is.False);
            Assert.That(SaveSystem.Current.equippedWeaponID, Is.EqualTo("pistol_dual"));
            Assert.That(manager.CurrentWeapon, Is.Null);
        }

        [Test]
        public void EquipOwnedWeaponUpdatesSelectionAndRaisesEvents()
        {
            SaveSystem.Current.Unlock("magnum");
            WeaponData changed = null;
            string ammoWeapon = null;
            int displayedAmmo = -1;
            manager.WeaponChanged += weapon => changed = weapon;
            manager.AmmoDisplayChanged += (weaponId, ammo) =>
            {
                ammoWeapon = weaponId;
                displayedAmmo = ammo;
            };

            Assert.That(manager.Equip("magnum"), Is.True);

            Assert.That(manager.CurrentWeapon, Is.SameAs(magnum));
            Assert.That(SaveSystem.Current.equippedWeaponID, Is.EqualTo("magnum"));
            Assert.That(SaveSystem.IsDirty, Is.True);
            Assert.That(changed, Is.SameAs(magnum));
            Assert.That(ammoWeapon, Is.EqualTo("magnum"));
            Assert.That(displayedAmmo, Is.Zero);
        }

        private void SetField(string name, object value)
        {
            FieldInfo field = typeof(WeaponManager).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(manager, value);
        }
    }
}
