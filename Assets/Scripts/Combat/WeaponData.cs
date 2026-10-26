using UnityEngine;

namespace BeanBlaster.Combat
{
    /// <summary>Defines a weapon's economy, firing, projectile, and special-mechanic tuning.</summary>
    [CreateAssetMenu(fileName = "WeaponData", menuName = "Bean Blaster/Weapon Data")]
    public sealed class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id = "pistol_dual";
        [SerializeField] private string displayName = "Dual Pistols";
        [SerializeField, TextArea] private string description = string.Empty;
        [SerializeField, Min(1)] private int shopSlot = 1;
        [SerializeField, Min(0)] private int cost;
        [SerializeField] private Sprite icon;

        [Header("Firing")]
        [SerializeField, Min(0f)] private float fireRate = 0.2f;
        [SerializeField, Min(0f)] private float damage = 15f;
        [SerializeField, Min(0f)] private float projectileSpeed = 22f;
        [SerializeField, Min(0f)] private float projectileGravityScale;
        [SerializeField, Min(0f)] private float projectileLifetime = 2f;
        [SerializeField, Min(0f)] private float knockbackForce = 2f;
        [SerializeField] private AmmoType ammoType;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Color projectileColor = Color.white;

        [Header("Ammunition")]
        [SerializeField] private bool usesAmmo;
        [SerializeField, Min(0)] private int ammoPerShot;
        [SerializeField, Min(0)] private int startingAmmo;
        [SerializeField, Min(0)] private int maxAmmo;
        [SerializeField, Min(0)] private int ammoPackSize;
        [SerializeField, Min(0)] private int ammoPackCost;

        [Header("Special Mechanics")]
        [SerializeField, Min(0)] private int pierceCount;
        [SerializeField, Min(0f)] private float aoeRadius;
        [SerializeField, Range(0f, 1f)] private float aoeEdgeDamageMultiplier;
        [SerializeField, Min(0)] private int splitCount;
        [SerializeField, Min(0f)] private float splitAngle;
        [SerializeField, Min(0f)] private float subBeamDamage;
        [SerializeField, Min(0f)] private float subBeamLifetime;
        [SerializeField, Min(0f)] private float fireZoneDuration;
        [SerializeField, Min(0f)] private float fireZoneWidth;
        [SerializeField, Min(0f)] private float fireZoneHeight;
        [SerializeField, Min(0f)] private float fireZoneTickInterval;
        [SerializeField, Min(0f)] private float fireZoneTickDamage;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public int ShopSlot => shopSlot;
        public int Cost => cost;
        public Sprite Icon => icon;
        public float FireRate => fireRate;
        public float Damage => damage;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileGravityScale => projectileGravityScale;
        public float ProjectileLifetime => projectileLifetime;
        public float KnockbackForce => knockbackForce;
        public AmmoType AmmoType => ammoType;
        public GameObject ProjectilePrefab => projectilePrefab;
        public Color ProjectileColor => projectileColor;
        public bool UsesAmmo => usesAmmo;
        public int AmmoPerShot => ammoPerShot;
        public int StartingAmmo => startingAmmo;
        public int MaxAmmo => maxAmmo;
        public int AmmoPackSize => ammoPackSize;
        public int AmmoPackCost => ammoPackCost;
        public int PierceCount => pierceCount;
        public float AoeRadius => aoeRadius;
        public float AoeEdgeDamageMultiplier => aoeEdgeDamageMultiplier;
        public int SplitCount => splitCount;
        public float SplitAngle => splitAngle;
        public float SubBeamDamage => subBeamDamage;
        public float SubBeamLifetime => subBeamLifetime;
        public float FireZoneDuration => fireZoneDuration;
        public float FireZoneWidth => fireZoneWidth;
        public float FireZoneHeight => fireZoneHeight;
        public float FireZoneTickInterval => fireZoneTickInterval;
        public float FireZoneTickDamage => fireZoneTickDamage;

        private void OnValidate()
        {
            shopSlot = Mathf.Max(1, shopSlot);
            cost = Mathf.Max(0, cost);
            fireRate = Mathf.Max(0f, fireRate);
            damage = Mathf.Max(0f, damage);
            projectileSpeed = Mathf.Max(0f, projectileSpeed);
            projectileGravityScale = Mathf.Max(0f, projectileGravityScale);
            projectileLifetime = Mathf.Max(0f, projectileLifetime);
            knockbackForce = Mathf.Max(0f, knockbackForce);
            ammoPerShot = Mathf.Max(0, ammoPerShot);
            startingAmmo = Mathf.Max(0, startingAmmo);
            maxAmmo = Mathf.Max(0, maxAmmo);
            ammoPackSize = Mathf.Max(0, ammoPackSize);
            ammoPackCost = Mathf.Max(0, ammoPackCost);
            pierceCount = Mathf.Max(0, pierceCount);
            aoeRadius = Mathf.Max(0f, aoeRadius);
            aoeEdgeDamageMultiplier = Mathf.Clamp01(aoeEdgeDamageMultiplier);
            splitCount = Mathf.Max(0, splitCount);
            splitAngle = Mathf.Max(0f, splitAngle);
            subBeamDamage = Mathf.Max(0f, subBeamDamage);
            subBeamLifetime = Mathf.Max(0f, subBeamLifetime);
            fireZoneDuration = Mathf.Max(0f, fireZoneDuration);
            fireZoneWidth = Mathf.Max(0f, fireZoneWidth);
            fireZoneHeight = Mathf.Max(0f, fireZoneHeight);
            fireZoneTickInterval = Mathf.Max(0f, fireZoneTickInterval);
            fireZoneTickDamage = Mathf.Max(0f, fireZoneTickDamage);

            if (id == "pistol_dual")
            {
                cost = 0;
                usesAmmo = false;
                ammoPerShot = 0;
            }
        }
    }
}
