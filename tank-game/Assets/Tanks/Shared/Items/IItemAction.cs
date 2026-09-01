using Tanks.Core.Helpers;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Data;

namespace Tanks.Shared.Items
{
    public interface IItemAction
    {
        void Invoke(IItemOwnerBehaviour owner);
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

        public void Invoke(IItemOwnerBehaviour owner)
        {
            IWeaponHolderBehaviour weaponHolder = owner.GetBehaviour<IWeaponHolderBehaviour>();
            Weapon created = _factory.Create(_config, weaponHolder.MuzzlePoint);
            weaponHolder.SwitchTo(created);
        }
    }
}
