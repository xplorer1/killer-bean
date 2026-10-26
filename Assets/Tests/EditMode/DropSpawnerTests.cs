using System.Collections.Generic;
using System.Linq;
using BeanBlaster.Combat;
using BeanBlaster.Economy;
using BeanBlaster.Save;
using NUnit.Framework;
using UnityEngine;

namespace BeanBlaster.Tests.EditMode
{
    public sealed class DropSpawnerTests
    {
        [TestCase(25)]
        [TestCase(40)]
        [TestCase(120)]
        public void CoinSplit_PreservesExactTotalAndNeverExceedsEight(int total)
        {
            IReadOnlyList<int> values = DropSpawner.BuildCoinValues(total);
            Assert.That(values.Sum(), Is.EqualTo(total));
            Assert.That(values.Count, Is.LessThanOrEqualTo(8));
            Assert.That(values.All(value => value > 0), Is.True);
        }

        [Test]
        public void AmmoEligibility_RequiresOwnedAmmoWeaponBelowMaximum()
        {
            WeaponData owned = EconomyServiceTests.CreateWeapon("owned", 1, true, 1, 10, 30, 10);
            WeaponData full = EconomyServiceTests.CreateWeapon("full", 1, true, 1, 10, 30, 10);
            WeaponData locked = EconomyServiceTests.CreateWeapon("locked", 1, true, 1, 10, 30, 10);
            WeaponData unlimited = EconomyServiceTests.CreateWeapon("pistol_dual", 0, false, 0, 0, 0, 0);
            WeaponDatabase database = ScriptableObject.CreateInstance<WeaponDatabase>();
            EconomyServiceTests.SetField(database, "weapons", new List<WeaponData> { owned, full, locked, unlimited });
            SaveData save = new SaveData();
            save.Unlock("owned");
            save.Unlock("full");
            save.SetAmmo("owned", 5);
            save.SetAmmo("full", 30);

            List<WeaponData> eligible = DropSpawner.GetEligibleAmmoWeapons(database, save);

            Assert.That(eligible.Select(value => value.Id), Is.EqualTo(new[] { "owned" }));
            Object.DestroyImmediate(owned);
            Object.DestroyImmediate(full);
            Object.DestroyImmediate(locked);
            Object.DestroyImmediate(unlimited);
            Object.DestroyImmediate(database);
        }
    }
}
