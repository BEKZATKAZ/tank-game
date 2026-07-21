using Tanks.Core.Helpers;
using Tanks.Shared.Adapters;
using Tanks.Shared.Stats;

namespace Tanks.Shared.Weapons
{
    public interface IWeaponOwner
    {
        ITransform Transform { get; }
        ITransform MuzzlePoint { get; }
        IStatOwner EffectOwner { get; }

        void SwitchTo(Required<Weapon> weapon);
        void SwitchToDefault();
    }
}
