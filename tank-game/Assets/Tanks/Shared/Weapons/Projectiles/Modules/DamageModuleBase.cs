using Tanks.Core.Helpers;

namespace Tanks.Shared.Weapons.Projectiles.Modules
{
    public abstract class DamageModuleBase : ProjectileModuleBehaviourBase
    {
        public abstract PositiveFloat MaxDamage { get; }
    }
}
