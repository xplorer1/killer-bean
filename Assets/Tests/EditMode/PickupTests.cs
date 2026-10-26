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
    public sealed class PickupTests
    {
        private string directory;
        private GameObject player;
        private WeaponData weapon;
        private WeaponDatabase database;
        private GameConfig config;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "BeanBlasterPickupTests", System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            SaveSystem.SetSaveDirectoryForTests(directory);
            SaveSystem.SetValidationDataForTests(new Dictionary<string, int> { ["magnum"] = 90, ["pistol_dual"] = int.MaxValue }, 3);
            weapon = EconomyServiceTests.CreateWeapon("magnum", 1, true, 1, 30, 90, 30);
            database = ScriptableObject.CreateInstance<WeaponDatabase>();
            EconomyServiceTests.SetField(database, "weapons", new List<WeaponData> { weapon });
            config = ScriptableObject.CreateInstance<GameConfig>();
            EconomyService.Initialize(database, config);
            SaveSystem.Load();
            SaveSystem.Current.Unlock("magnum");
            player = new GameObject("Player");
            player.layer = 7;
            player.AddComponent<BoxCollider2D>();
            Health health = player.AddComponent<Health>();
            typeof(Health).GetField("team", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(health, Team.Player);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(weapon);
            Object.DestroyImmediate(database);
            Object.DestroyImmediate(config);
            SaveSystem.ClearTestOverrides();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void CoinCollectionImmediatelyMutatesDirtySave()
        {
            GameObject pickupObject = new GameObject("Coin");
            pickupObject.AddComponent<CircleCollider2D>();
            CoinPickup pickup = pickupObject.AddComponent<CoinPickup>();
            pickup.Configure(10, true);

            Assert.That(pickup.TryCollect(player.GetComponent<Collider2D>()), Is.True);
            Assert.That(SaveSystem.Current.coins, Is.EqualTo(10));
            Assert.That(SaveSystem.IsDirty, Is.True);
            Object.DestroyImmediate(pickupObject);
        }

        [Test]
        public void AmmoCollectionImmediatelyAddsConfiguredAmount()
        {
            GameObject pickupObject = new GameObject("Ammo");
            pickupObject.AddComponent<BoxCollider2D>();
            AmmoPickup pickup = pickupObject.AddComponent<AmmoPickup>();
            pickup.Configure("magnum", 8);

            Assert.That(pickup.TryCollect(player.GetComponent<Collider2D>()), Is.True);
            Assert.That(SaveSystem.Current.GetAmmo("magnum"), Is.EqualTo(8));
            Assert.That(SaveSystem.IsDirty, Is.True);
            Object.DestroyImmediate(pickupObject);
        }
    }
}
