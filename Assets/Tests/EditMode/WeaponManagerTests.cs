using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BeanBlaster.Combat;
using BeanBlaster.Core;
using BeanBlaster.Economy;
using BeanBlaster.Player;
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
        private WeaponData grenade;
        private WeaponData laser;
        private WeaponData napalm;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "BeanBlasterWeaponManagerTests", System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            SaveSystem.SetSaveDirectoryForTests(directory);
            SaveSystem.SetValidationDataForTests(new Dictionary<string, int>
            {
                ["pistol_dual"] = int.MaxValue,
                ["magnum"] = 90,
                ["explosive_gl"] = 30,
                ["beam_splitter"] = 150,
                ["fire_bomb"] = 15
            }, 3);

            pistol = EconomyServiceTests.CreateWeapon("pistol_dual", 0, false, 0, 0, 0, 0);
            magnum = EconomyServiceTests.CreateWeapon("magnum", 2500, true, 1, 30, 90, 30);
            grenade = EconomyServiceTests.CreateWeapon("explosive_gl", 7500, true, 1, 10, 30, 10, 400);
            laser = EconomyServiceTests.CreateWeapon("beam_splitter", 15000, true, 1, 50, 150, 50, 500);
            napalm = EconomyServiceTests.CreateWeapon("fire_bomb", 20000, true, 1, 5, 15, 5, 800);
            database = ScriptableObject.CreateInstance<WeaponDatabase>();
            EconomyServiceTests.SetField(database, "weapons", new List<WeaponData> { pistol, magnum, grenade, laser, napalm });
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
            Object.DestroyImmediate(grenade);
            Object.DestroyImmediate(laser);
            Object.DestroyImmediate(napalm);
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

        [Test]
        public void DualPistolsLaunchPooledShotsFromAlternatingMuzzles()
        {
            GameObject projectilePrefabObject = new GameObject("Pistol Projectile Prefab");
            projectilePrefabObject.AddComponent<Rigidbody2D>();
            projectilePrefabObject.AddComponent<BoxCollider2D>();
            projectilePrefabObject.AddComponent<Projectile>();
            EconomyServiceTests.SetField(pistol, "projectilePrefab", projectilePrefabObject);

            GameObject poolObject = new GameObject("Projectile Pool");
            ProjectilePool pool = poolObject.AddComponent<ProjectilePool>();
            GameObject socketObject = new GameObject("Fire Socket");
            socketObject.transform.position = new Vector3(2f, 3f, 0f);
            PlayerInputReader input = managerObject.AddComponent<PlayerInputReader>();
            SetField("projectilePool", pool);
            SetField("fireSocket", socketObject.transform);
            SetField("input", input);
            SetField("currentWeapon", pistol);

            int firedCount = 0;
            manager.Fired += _ => firedCount++;
            InvokeTryFire();
            InvokeTryFire();

            Projectile[] instances = poolObject.GetComponentsInChildren<Projectile>(true);
            List<Projectile> active = new List<Projectile>();
            foreach (Projectile instance in instances)
            {
                if (instance.gameObject.activeSelf)
                {
                    active.Add(instance);
                }
            }

            Assert.That(firedCount, Is.EqualTo(2));
            Assert.That(active.Count, Is.EqualTo(2));
            Assert.That(active[0].GetComponent<Rigidbody2D>().velocity.x, Is.EqualTo(22f).Within(0.001f));
            Assert.That(active[1].GetComponent<Rigidbody2D>().velocity.x, Is.EqualTo(22f).Within(0.001f));
            Assert.That(active[0].transform.position.y, Is.Not.EqualTo(active[1].transform.position.y));
            float[] muzzleHeights = { active[0].transform.position.y, active[1].transform.position.y };
            System.Array.Sort(muzzleHeights);
            Assert.That(muzzleHeights[0], Is.EqualTo(2.92f).Within(0.001f));
            Assert.That(muzzleHeights[1], Is.EqualTo(3.08f).Within(0.001f));

            Object.DestroyImmediate(projectilePrefabObject);
            Object.DestroyImmediate(poolObject);
            Object.DestroyImmediate(socketObject);
        }

        [Test]
        public void EverySpecialWeaponConsumesExactlyOneRoundPerShot()
        {
            GameObject projectilePrefabObject = new GameObject("Shared Projectile Prefab");
            projectilePrefabObject.AddComponent<Rigidbody2D>();
            projectilePrefabObject.AddComponent<BoxCollider2D>();
            projectilePrefabObject.AddComponent<Projectile>();
            GameObject poolObject = new GameObject("Projectile Pool");
            ProjectilePool pool = poolObject.AddComponent<ProjectilePool>();
            GameObject socketObject = new GameObject("Fire Socket");
            PlayerInputReader input = managerObject.AddComponent<PlayerInputReader>();
            SetField("projectilePool", pool);
            SetField("fireSocket", socketObject.transform);
            SetField("input", input);

            WeaponData[] weapons = { magnum, grenade, laser, napalm };
            AmmoType[] ammoTypes = { AmmoType.Piercing, AmmoType.Explosive, AmmoType.Beam, AmmoType.Incendiary };
            for (int i = 0; i < weapons.Length; i++)
            {
                WeaponData weapon = weapons[i];
                EconomyServiceTests.SetField(weapon, "projectilePrefab", projectilePrefabObject);
                EconomyServiceTests.SetField(weapon, "ammoType", ammoTypes[i]);
                EconomyServiceTests.SetField(weapon, "projectileSpeed", 10f);
                EconomyServiceTests.SetField(weapon, "projectileLifetime", 1f);
                SaveSystem.Current.Unlock(weapon.Id);
                SaveSystem.Current.SetAmmo(weapon.Id, 5);
                SetField("currentWeapon", weapon);

                InvokeTryFire();

                Assert.That(EconomyService.GetAmmo(weapon.Id), Is.EqualTo(4), weapon.Id);
            }

            Object.DestroyImmediate(projectilePrefabObject);
            Object.DestroyImmediate(poolObject);
            Object.DestroyImmediate(socketObject);
        }

        [Test]
        public void EmptySpecialWeaponRaisesOneEmptyClickUntilTriggerRelease()
        {
            SaveSystem.Current.Unlock("magnum");
            SaveSystem.Current.SetAmmo("magnum", 0);
            SetField("currentWeapon", magnum);
            int emptyClicks = 0;
            manager.EmptyClicked += () => emptyClicks++;

            InvokeTryFire();
            InvokeTryFire();

            Assert.That(emptyClicks, Is.EqualTo(1));
        }

        private void SetField(string name, object value)
        {
            FieldInfo field = typeof(WeaponManager).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(manager, value);
        }

        private void InvokeTryFire()
        {
            MethodInfo method = typeof(WeaponManager).GetMethod("TryFire", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(manager, null);
        }
    }
}
