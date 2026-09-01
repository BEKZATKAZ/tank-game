using Tanks.Server.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;

namespace Tanks.Server.Items.Behaviours.OnPickup
{
    [ItemBehaviour("no_room_for_error")]
    public sealed class NoRoomForErrorBehaviour : ItemBehaviourBase
    {
        public override void OnPickup(IItemOwnerBehaviour owner)
        {
            IAuthoritativeHealth health = owner
                .GetBehaviour<IHealthBehaviour>()
                .Require<IAuthoritativeHealth>();

            IStatOwnerBehaviour statOwner = owner.GetBehaviour<IStatOwnerBehaviour>();
            health.SetState(new HealthState(current: 1, max: 1));

            ItemStat damage = Config.GetStat("damage");
            statOwner.AddStat(StatType.Damage, damage.ValueType, damage.Value);
        }
    }
}
