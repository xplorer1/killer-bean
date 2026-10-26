using System;

namespace BeanBlaster.Save
{
    /// <summary>Stores one weapon ammunition reserve.</summary>
    [Serializable]
    public sealed class AmmoEntry
    {
        public string weaponID = string.Empty;
        public int amount;
    }
}
