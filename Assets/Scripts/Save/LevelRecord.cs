using System;

namespace BeanBlaster.Save
{
    /// <summary>Stores persistent results for one level.</summary>
    [Serializable]
    public sealed class LevelRecord
    {
        public int levelIndex;
        public bool completed;
        public float bestTimeSeconds;
        public bool noDeathClear;
    }
}
