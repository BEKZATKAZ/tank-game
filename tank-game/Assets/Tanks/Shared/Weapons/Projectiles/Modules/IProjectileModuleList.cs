using System;

namespace Tanks.Shared.Weapons.Projectiles.Modules
{
    public interface IProjectileModuleList : INetworkBehaviourExtended
    {
        void Call(Action<ProjectileModuleBehaviourBase> action);
    }
}
