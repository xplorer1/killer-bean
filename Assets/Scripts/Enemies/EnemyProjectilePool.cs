using System.Collections.Generic;
using UnityEngine;

namespace BeanBlaster.Enemies
{
    public sealed class EnemyProjectilePool : MonoBehaviour
    {
        [SerializeField] private EnemyProjectile prefab;
        [SerializeField, Min(0)] private int prewarmCount = 10;
        [SerializeField, Min(1)] private int maximumCount = 100;

        private readonly Queue<EnemyProjectile> available = new Queue<EnemyProjectile>();
        private int totalCount;

        public EnemyProjectile Get()
        {
            EnsurePrewarmed();
            while (available.Count > 0)
            {
                EnemyProjectile candidate = available.Dequeue();
                if (candidate != null)
                {
                    candidate.gameObject.SetActive(true);
                    return candidate;
                }
                totalCount--;
            }

            if (prefab == null || totalCount >= maximumCount)
            {
                return null;
            }

            EnemyProjectile created = CreateInstance();
            created.gameObject.SetActive(true);
            return created;
        }

        public void Return(EnemyProjectile projectile)
        {
            if (projectile == null)
            {
                return;
            }
            projectile.ResetForReuse();
            projectile.transform.SetParent(transform, false);
            projectile.gameObject.SetActive(false);
            available.Enqueue(projectile);
        }

        public void Configure(EnemyProjectile projectilePrefab, int prewarm = 10, int maximum = 100)
        {
            prefab = projectilePrefab;
            prewarmCount = Mathf.Max(0, prewarm);
            maximumCount = Mathf.Max(1, maximum);
        }

        private void EnsurePrewarmed()
        {
            if (totalCount > 0 || prefab == null)
            {
                return;
            }
            int count = Mathf.Min(prewarmCount, maximumCount);
            for (int i = 0; i < count; i++)
            {
                EnemyProjectile instance = CreateInstance();
                instance.gameObject.SetActive(false);
                available.Enqueue(instance);
            }
        }

        private EnemyProjectile CreateInstance()
        {
            EnemyProjectile instance = Instantiate(prefab, transform);
            instance.AssignPool(this);
            totalCount++;
            return instance;
        }
    }
}
