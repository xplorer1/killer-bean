namespace BeanBlaster.Core
{
    /// <summary>Defines an object that can accept team-aware combat damage.</summary>
    public interface IDamageable
    {
        Team Team { get; }
        bool IsAlive { get; }

        /// <summary>Attempts to apply damage and reports whether the request was accepted.</summary>
        bool TryApplyDamage(DamageInfo info);
    }
}
