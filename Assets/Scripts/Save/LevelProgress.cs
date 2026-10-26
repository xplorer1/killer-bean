using System;
using System.Collections.Generic;

namespace BeanBlaster.Save
{
    /// <summary>Stores level unlocking and per-level records.</summary>
    [Serializable]
    public sealed class LevelProgress
    {
        public int highestUnlockedLevel = 1;
        public List<LevelRecord> records = new List<LevelRecord>();
    }
}
