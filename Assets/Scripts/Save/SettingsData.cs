using System;

namespace BeanBlaster.Save
{
    /// <summary>Stores player-adjustable presentation settings.</summary>
    [Serializable]
    public sealed class SettingsData
    {
        public float masterVolume = 1f;
        public float sfxVolume = 1f;
        public bool screenShake = true;
    }
}
