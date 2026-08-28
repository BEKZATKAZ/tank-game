using Tanks.Core.Helpers;
using Tanks.Shared.Adapters;
using Tanks.Shared.Entities;
using Tanks.Shared.Stats;

namespace Tanks.Shared.Weapons
{
    public interface IWeaponOwner
    {
        ITransform Transform { get; }
        ITransform MuzzlePoint { get; }
        IVelocity Velocity { get; }
        IStatOwner EffectOwner { get; }
        IEntity Entity { get; }

        void SwitchTo(Required<Weapon> weapon);
        void SwitchToDefault();
    }
}
