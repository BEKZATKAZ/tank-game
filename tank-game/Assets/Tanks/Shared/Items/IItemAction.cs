using Tanks.Core.Helpers;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Data;

namespace Tanks.Shared.Items
{
    public interface IItemAction
    {
        void Invoke(IItemOwner owner);
    }

    public sealed class GiveWeaponAction : IItemAction
    {
        public GiveWeaponAction(Required<IWeaponFactory> factory, Required<IWeaponConfig> config)
        {
            _factory = factory.Value;
            _config = config.Value;
        }

        private readonly IWeaponFactory _factory;
        private readonly IWeaponConfig _config;

        public void Invoke(IItemOwner owner)
        {
            Weapon created = _factory.Create(_config, owner.WeaponOwner.MuzzlePoint);
            owner.WeaponOwner.SwitchTo(created);
        }
    }
}
