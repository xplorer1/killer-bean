using System.Collections;
using BeanBlaster.Player;
using BeanBlaster.Save;
using BeanBlaster.Enemies;
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

        private Vector3 activeRespawnPosition;
        private bool respawnPending;

        public int LevelIndex => levelIndex;
        public float KillPlaneY => killPlaneY;
        public int DeathsThisAttempt { get; private set; }
        public int KillsThisAttempt { get; private set; }

        private void Awake()
        {
            activeRespawnPosition = playerSpawnPoint == null ? Vector3.zero : playerSpawnPoint.position;
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
