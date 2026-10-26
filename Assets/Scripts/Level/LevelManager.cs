using System.Collections;
using BeanBlaster.Player;
using BeanBlaster.Save;
using BeanBlaster.Enemies;
using BeanBlaster.Core;
using BeanBlaster.Economy;
using UnityEngine.SceneManagement;
using System;
using UnityEngine;

namespace BeanBlaster.Level
{
    /// <summary>Owns the active player spawn and the Phase 5 checkpoint respawn loop.</summary>
    public sealed class LevelManager : MonoBehaviour
    {
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField, Min(1)] private int levelIndex = 1;
        [SerializeField] private float killPlaneY = -20f;
        [SerializeField, Min(0f)] private float respawnDelay = 1.5f;
        [SerializeField] private GameConfig config;

        private Vector3 activeRespawnPosition;
        private bool respawnPending;
        private bool completed;
        private float attemptTime;

        public int LevelIndex => levelIndex;
        public float KillPlaneY => killPlaneY;
        public int DeathsThisAttempt { get; private set; }
        public int KillsThisAttempt { get; private set; }
        public int CoinsCollectedThisAttempt { get; private set; }
        public float AttemptTime => attemptTime;
        public bool IsCompleted => completed;
        public bool IsPaused { get; private set; }
        public event Action<LevelCompletionResult> LevelCompleted;

        private void Awake()
        {
            activeRespawnPosition = playerSpawnPoint == null ? Vector3.zero : playerSpawnPoint.position;
            config ??= Resources.Load<GameConfig>("GameConfig");
        }

        private void Update() => AdvanceAttempt(Time.unscaledDeltaTime);

        public void AdvanceAttempt(float deltaTime)
        {
            if (!completed && !IsPaused && deltaTime > 0f) attemptTime += deltaTime;
        }

        /// <summary>Changes the checkpoint used for subsequent respawns and persists progress.</summary>
        public void SetCheckpoint(Vector3 position)
        {
            activeRespawnPosition = position;
            SaveSystem.MarkDirty();
            SaveSystem.Save();
        }

        /// <summary>Schedules one full-health respawn and persists permanent economy state.</summary>
        public void OnPlayerDied(PlayerController2D player)
        {
            if (player == null || respawnPending)
            {
                return;
            }

            DeathsThisAttempt++;
            respawnPending = true;
            SaveSystem.Save();
            StartCoroutine(RespawnAfterDelay(player));
        }

        /// <summary>Records one enemy death after the enemy has applied its own one-shot guard.</summary>
        public void OnEnemyKilled(EnemyAI2D enemy)
        {
            if (enemy != null)
            {
                KillsThisAttempt++;
            }
        }

        public void OnCoinsCollected(int amount)
        {
            if (amount > 0)
            {
                CoinsCollectedThisAttempt += amount;
            }
        }

        public bool CompleteLevel()
        {
            if (completed || config == null) return false;
            completed = true;
            SaveData save = SaveSystem.Current;
            LevelRecord record = save.GetOrCreateRecord(levelIndex);
            bool firstCompletion = !record.completed;
            bool noDeath = DeathsThisAttempt == 0;
            int reward = config.GetLevelCompleteCoins(levelIndex);
            EconomyService.AddCoins(reward, CoinSource.LevelComplete);
            if (noDeath)
            {
                reward += config.NoDeathBonusCoins;
                EconomyService.AddCoins(config.NoDeathBonusCoins, CoinSource.NoDeathBonus);
            }
            if (firstCompletion)
            {
                reward += config.FirstCompletionBonusCoins;
                EconomyService.AddCoins(config.FirstCompletionBonusCoins, CoinSource.FirstCompletionBonus);
            }
            record.completed = true;
            record.noDeathClear |= noDeath;
            if (record.bestTimeSeconds <= 0f || attemptTime < record.bestTimeSeconds) record.bestTimeSeconds = attemptTime;
            save.levelProgress.highestUnlockedLevel = Mathf.Max(save.levelProgress.highestUnlockedLevel, Mathf.Min(levelIndex + 1, config.TotalLevelCount));
            SaveSystem.MarkDirty();
            SaveSystem.Save();
            LevelCompleted?.Invoke(new LevelCompletionResult(levelIndex, attemptTime, DeathsThisAttempt, KillsThisAttempt, CoinsCollectedThisAttempt, reward, firstCompletion, noDeath));
            return true;
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        public void RestartLevel()
        {
            SaveSystem.Save();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void ExitToMainMenu()
        {
            SaveSystem.Save();
            Time.timeScale = 1f;
            if (config != null && !string.IsNullOrEmpty(config.MainMenuSceneName)) SceneManager.LoadScene(config.MainMenuSceneName);
        }

        private IEnumerator RespawnAfterDelay(PlayerController2D player)
        {
            yield return new WaitForSeconds(respawnDelay);
            player.RespawnAt(activeRespawnPosition);
            respawnPending = false;
        }

        private void OnValidate()
        {
            levelIndex = Mathf.Max(1, levelIndex);
            respawnDelay = Mathf.Max(0f, respawnDelay);
        }
    }
}
