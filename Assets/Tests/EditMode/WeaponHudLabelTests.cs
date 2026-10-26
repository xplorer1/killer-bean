using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BeanBlaster.Combat;
using BeanBlaster.Core;
using BeanBlaster.Economy;
using BeanBlaster.Save;
using BeanBlaster.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BeanBlaster.Tests.EditMode
{
    public sealed class WeaponHudLabelTests
    {
        private GameObject root;
        private WeaponData pistol;
        private WeaponData magnum;
        private WeaponDatabase database;
        private GameConfig config;
        private string saveDirectory;

        [SetUp]
        public void SetUp()
        {
            saveDirectory = Path.Combine(Path.GetTempPath(), "BeanBlasterWeaponHudTests", System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(saveDirectory);
            SaveSystem.SetSaveDirectoryForTests(saveDirectory);
            SaveSystem.SetValidationDataForTests(new Dictionary<string, int>
            {
                ["pistol_dual"] = int.MaxValue,
                ["magnum"] = 90
            }, 3);
            pistol = EconomyServiceTests.CreateWeapon("pistol_dual", 0, false, 0, 0, 0, 0);
            SetWeaponField(pistol, "displayName", "Dual Pistols");
            magnum = EconomyServiceTests.CreateWeapon("magnum", 2500, true, 1, 30, 90, 30);
            SetWeaponField(magnum, "displayName", "Heavy Magnum");
            database = ScriptableObject.CreateInstance<WeaponDatabase>();
            EconomyServiceTests.SetField(database, "weapons", new List<WeaponData> { pistol, magnum });
            config = ScriptableObject.CreateInstance<GameConfig>();
            EconomyService.Initialize(database, config);
            SaveSystem.Load();
            SaveSystem.Current.Unlock("magnum");
            SaveSystem.Current.SetAmmo("magnum", 12);
            root = new GameObject("Weapon HUD Test");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(pistol);
            Object.DestroyImmediate(magnum);
            Object.DestroyImmediate(database);
            Object.DestroyImmediate(config);
            SaveSystem.ClearTestOverrides();
            if (Directory.Exists(saveDirectory))
            {
                Directory.Delete(saveDirectory, true);
            }
        }

        [Test]
        public void EquippedWeaponChangeImmediatelyUpdatesVisibleNameAndAmmo()
        {
            Text text = root.AddComponent<Text>();
            WeaponManager manager = root.AddComponent<WeaponManager>();
            SetManagerField(manager, "database", database);
            WeaponHudLabel hud = root.AddComponent<WeaponHudLabel>();
            hud.Configure(text, manager);

            Assert.That(manager.Equip("magnum"), Is.True);

            Assert.That(text.text, Is.EqualTo("HEAVY MAGNUM  ·  12"));
        }

        private static void SetWeaponField(WeaponData weapon, string name, object value)
        {
            FieldInfo field = typeof(WeaponData).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(weapon, value);
        }

        private static void SetManagerField(WeaponManager manager, string name, object value)
        {
            FieldInfo field = typeof(WeaponManager).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(manager, value);
        }
    }
}
