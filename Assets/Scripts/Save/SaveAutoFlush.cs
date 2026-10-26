using System;
using BeanBlaster.Core;
using UnityEngine;

namespace BeanBlaster.Save
{
    public sealed class SaveAutoFlush : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float interval = 30f;
        private float elapsed;
        public event Action Flushed;

        private void Start()
        {
            GameConfig config = GameManager.Instance == null ? null : GameManager.Instance.Config;
            if (config != null) interval = config.AutosaveInterval;
        }

        private void Update()
        {
            Advance(Time.unscaledDeltaTime);
        }

        public bool Advance(float unscaledDeltaTime)
        {
            if (interval <= 0f || unscaledDeltaTime <= 0f) return false;
            elapsed += unscaledDeltaTime;
            if (elapsed < interval) return false;
            elapsed %= interval;
            if (!SaveSystem.IsDirty) return false;
            bool saved = SaveSystem.Save();
            if (saved) Flushed?.Invoke();
            return saved;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && SaveSystem.IsDirty) SaveSystem.Save();
        }

        private void OnApplicationQuit()
        {
            if (SaveSystem.IsDirty) SaveSystem.Save();
        }
    }
}
