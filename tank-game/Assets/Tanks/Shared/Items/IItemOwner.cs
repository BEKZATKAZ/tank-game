using Tanks.Shared.Adapters;
using Tanks.Shared.Stats;
using Tanks.Shared.Health;
using Tanks.Shared.Items.Pickups;
using Tanks.Shared.Weapons;
using Tanks.Shared.Items.Data;

namespace Tanks.Shared.Items
{
    public interface IItemOwner
    {
        ITransform Transform { get; }
        IWeaponOwner WeaponOwner { get; }
        IStatOwner StatOwner { get; }

        IHealth Health { get; }
        IDamageable Damageable { get; }
        IHealable Healable { get; }

        void PickupItem(IItemConfig item, in PickupId pickupId);
    }
}
