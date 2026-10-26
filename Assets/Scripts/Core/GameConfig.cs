using System.Collections.Generic;
using UnityEngine;

namespace BeanBlaster.Core
{
    /// <summary>Stores game-wide tuning, scene names, persistence intervals, and coin rewards.</summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Bean Blaster/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Gameplay")]
        [SerializeField] private bool unlimitedAmmoForOwnedWeapons;
        [SerializeField] private float killPlaneY = -20f;

        [Header("Scenes")]
        [SerializeField] private List<string> levelSceneNames = new List<string>
        {
            "Level_01",
            "Level_02",
            "Level_03"
        };
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Saving")]
        [SerializeField, Min(0f)] private float autosaveInterval = 30f;

        [Header("Coin Rewards")]
        [SerializeField, Min(0)] private int levelCompleteBaseCoins = 500;
        [SerializeField, Min(0)] private int levelIndexCoinMultiplier = 150;
        [SerializeField, Min(0)] private int noDeathBonusCoins = 250;
        [SerializeField, Min(0)] private int firstCompletionBonusCoins = 300;
        [SerializeField, Min(0)] private int placedCoinPickupValue = 10;

        public bool UnlimitedAmmoForOwnedWeapons => unlimitedAmmoForOwnedWeapons;
        public float KillPlaneY => killPlaneY;
        public IReadOnlyList<string> LevelSceneNames => levelSceneNames;
        public string MainMenuSceneName => mainMenuSceneName;
        public float AutosaveInterval => autosaveInterval;
        public int LevelCompleteBaseCoins => levelCompleteBaseCoins;
        public int LevelIndexCoinMultiplier => levelIndexCoinMultiplier;
        public int NoDeathBonusCoins => noDeathBonusCoins;
        public int FirstCompletionBonusCoins => firstCompletionBonusCoins;
        public int PlacedCoinPickupValue => placedCoinPickupValue;
        public int TotalLevelCount => levelSceneNames?.Count ?? 0;

        /// <summary>Calculates the base completion reward for a one-based level index.</summary>
        public int GetLevelCompleteCoins(int levelIndex)
        {
            return levelCompleteBaseCoins + levelIndexCoinMultiplier * Mathf.Max(1, levelIndex);
        }

        private void OnValidate()
        {
            levelSceneNames ??= new List<string>();
            mainMenuSceneName ??= string.Empty;
            autosaveInterval = Mathf.Max(0f, autosaveInterval);
            levelCompleteBaseCoins = Mathf.Max(0, levelCompleteBaseCoins);
            levelIndexCoinMultiplier = Mathf.Max(0, levelIndexCoinMultiplier);
            noDeathBonusCoins = Mathf.Max(0, noDeathBonusCoins);
            firstCompletionBonusCoins = Mathf.Max(0, firstCompletionBonusCoins);
            placedCoinPickupValue = Mathf.Max(0, placedCoinPickupValue);
        }
    }
}
