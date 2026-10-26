using System.Collections.Generic;
using System.Linq;
using BeanBlaster.Combat;
using BeanBlaster.Enemies;
using BeanBlaster.Save;
using UnityEngine;

namespace BeanBlaster.Economy
{
    public sealed class DropSpawner : MonoBehaviour
    {
        [SerializeField] private EnemyAI2D enemy;
        [SerializeField] private EnemyStats stats;
        [SerializeField] private CoinPickup coinPrefab;
        [SerializeField] private AmmoPickup ammoPrefab;
        [SerializeField] private WeaponDatabase weaponDatabase;
        private bool spawned;

        private void OnEnable()
        {
            enemy ??= GetComponent<EnemyAI2D>();
            if (enemy != null)
            {
                enemy.DeathResolved += OnEnemyDeath;
            }
        }

        private void OnDisable()
        {
            if (enemy != null)
            {
                enemy.DeathResolved -= OnEnemyDeath;
            }
        }

        private void OnEnemyDeath(EnemyAI2D _)
        {
            if (spawned || stats == null)
            {
                return;
            }
            spawned = true;
            foreach (int value in BuildCoinValues(stats.CoinValue))
            {
                if (coinPrefab == null) continue;
                CoinPickup pickup = Instantiate(coinPrefab, transform.position, Quaternion.identity);
                pickup.Configure(value, false);
                Rigidbody2D body = pickup.GetComponent<Rigidbody2D>();
                if (body != null) body.velocity = new Vector2(Random.Range(-3f, 3f), Random.Range(4f, 7f));
            }

            List<WeaponData> eligible = GetEligibleAmmoWeapons(weaponDatabase, SaveSystem.Current);
            if (ammoPrefab != null && eligible.Count > 0 && Random.value < 0.15f)
            {
                WeaponData weapon = eligible[Random.Range(0, eligible.Count)];
                AmmoPickup pickup = Instantiate(ammoPrefab, transform.position, Quaternion.identity);
                pickup.Configure(weapon.Id, Mathf.Max(1, Mathf.CeilToInt(weapon.AmmoPackSize * 0.25f)));
            }
        }

        public static IReadOnlyList<int> BuildCoinValues(int total)
        {
            if (total <= 0) return new int[0];
            int count = Mathf.Min(8, Mathf.CeilToInt(total / 10f));
            int baseValue = total / count;
            int remainder = total % count;
            int[] values = new int[count];
            for (int i = 0; i < count; i++) values[i] = baseValue + (i < remainder ? 1 : 0);
            return values;
        }

        public static List<WeaponData> GetEligibleAmmoWeapons(WeaponDatabase database, SaveData save)
        {
            if (database == null || save == null) return new List<WeaponData>();
            return database.All.Where(weapon => weapon != null && weapon.UsesAmmo && save.IsUnlocked(weapon.Id) && save.GetAmmo(weapon.Id) < weapon.MaxAmmo).ToList();
        }
    }
}
