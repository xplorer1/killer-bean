using BeanBlaster.Combat;
using BeanBlaster.Economy;
using BeanBlaster.Save;
using UnityEngine;

namespace BeanBlaster.Core
{
    /// <summary>Owns the one persistent runtime service root.</summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameConfig config;
        [SerializeField] private WeaponDatabase weaponDatabase;

        public GameConfig Config => config;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            config ??= Resources.Load<GameConfig>("GameConfig");
            weaponDatabase ??= Resources.Load<WeaponDatabase>("WeaponDatabase");
            SaveSystem.Load();
            EconomyService.Initialize(weaponDatabase, config);
            if (GetComponent<SaveAutoFlush>() == null) gameObject.AddComponent<SaveAutoFlush>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
