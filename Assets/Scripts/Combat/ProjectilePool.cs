using System.Collections.Generic;
using UnityEngine;

namespace BeanBlaster.Combat
{
    /// <summary>Maintains an independent bounded projectile pool for each prefab.</summary>
    public sealed class ProjectilePool : MonoBehaviour
    {
        [SerializeField, Min(0)] private int prewarmCount = 20;
        [SerializeField, Min(1)] private int maximumPerPrefab = 200;

        private readonly Dictionary<int, PoolBucket> buckets = new Dictionary<int, PoolBucket>();

        /// <summary>Obtains an inactive projectile or grows its prefab pool up to the configured maximum.</summary>
        public Projectile Get(Projectile prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("Cannot obtain a projectile without a prefab.", this);
                return null;
            }

            int key = prefab.GetInstanceID();
            if (!buckets.TryGetValue(key, out PoolBucket bucket))
            {
                bucket = new PoolBucket(prefab);
                buckets.Add(key, bucket);
                int initialCount = Mathf.Min(prewarmCount, maximumPerPrefab);
                for (int i = 0; i < initialCount; i++)
                {
                    CreateInstance(bucket);
                }
            }

            while (bucket.available.Count > 0)
            {
                Projectile candidate = bucket.available.Dequeue();
                if (candidate == null)
                {
                    bucket.totalCount--;
                    continue;
                }

                candidate.gameObject.SetActive(true);
                return candidate;
            }

            if (bucket.totalCount >= maximumPerPrefab)
            {
                return null;
            }

            Projectile created = CreateInstance(bucket);
            bucket.available.Dequeue();
            created.gameObject.SetActive(true);
            return created;
        }

        /// <summary>Obtains another projectile from the same prefab bucket as an active pooled shot.</summary>
        internal Projectile GetByPrefabKey(int prefabKey)
        {
            if (prefabKey == 0 || !buckets.TryGetValue(prefabKey, out PoolBucket bucket))
            {
                return null;
            }

            while (bucket.available.Count > 0)
            {
                Projectile candidate = bucket.available.Dequeue();
                if (candidate == null)
                {
                    bucket.totalCount--;
                    continue;
                }

                candidate.gameObject.SetActive(true);
                return candidate;
            }

            if (bucket.totalCount >= maximumPerPrefab)
            {
                return null;
            }

            Projectile created = CreateInstance(bucket);
            bucket.available.Dequeue();
            created.gameObject.SetActive(true);
            return created;
        }

        /// <summary>Returns an active projectile to its originating prefab pool.</summary>
        public void Return(Projectile projectile)
        {
            if (projectile == null || projectile.PrefabKey == 0)
            {
                return;
            }

            if (!buckets.TryGetValue(projectile.PrefabKey, out PoolBucket bucket))
            {
                Destroy(projectile.gameObject);
                return;
            }

            projectile.ResetForReuse();
            projectile.transform.SetParent(transform, false);
            projectile.gameObject.SetActive(false);
            bucket.available.Enqueue(projectile);
        }

        /// <summary>Returns the number of instantiated objects for a prefab.</summary>
        public int GetTotalCount(Projectile prefab)
        {
            return prefab != null && buckets.TryGetValue(prefab.GetInstanceID(), out PoolBucket bucket)
                ? bucket.totalCount
                : 0;
        }

        private Projectile CreateInstance(PoolBucket bucket)
        {
            Projectile instance = Instantiate(bucket.prefab, transform);
            instance.name = bucket.prefab.name;
            instance.AssignPool(this, bucket.prefab.GetInstanceID());
            instance.gameObject.SetActive(false);
            bucket.available.Enqueue(instance);
            bucket.totalCount++;
            return instance;
        }

        private void OnValidate()
        {
            prewarmCount = Mathf.Max(0, prewarmCount);
            maximumPerPrefab = Mathf.Max(1, maximumPerPrefab);
            prewarmCount = Mathf.Min(prewarmCount, maximumPerPrefab);
        }

        private sealed class PoolBucket
        {
            public readonly Projectile prefab;
            public readonly Queue<Projectile> available = new Queue<Projectile>();
            public int totalCount;

            public PoolBucket(Projectile prefab)
            {
                this.prefab = prefab;
            }
        }
    }
}
