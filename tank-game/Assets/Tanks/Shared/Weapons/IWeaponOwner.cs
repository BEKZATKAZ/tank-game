using Tanks.Shared.Adapters;

namespace Tanks.Shared.Weapons
{
    public interface IWeaponOwner
    {
        ITransform Transform { get; }
    }
}