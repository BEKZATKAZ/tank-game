using Tanks.Shared.Stats;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Items.Data;

namespace Tanks.Server.Items.Behaviours.OnPickup
{
    [ItemBehaviour("no_room_for_error")]
    public sealed class NoRoomForErrorBehaviour : ItemBehaviourBase
    {
        public override void OnPickup(IItemOwner owner)
        {
            ItemStat damage = Config.GetStat("damage");
            owner.Health.SetMaxHealth(value: 1);
            owner.StatOwner.AddStat(StatType.Damage, damage.ValueType, damage.Value);
        }
    }
}
