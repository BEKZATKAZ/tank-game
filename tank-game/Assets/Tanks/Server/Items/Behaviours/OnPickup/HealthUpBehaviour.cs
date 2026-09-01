using Tanks.Server.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;

namespace Tanks.Server.Items.Behaviours.OnPickup
{
    [ItemBehaviour("health_up")]
    public sealed class HealthUpBehaviour : ItemBehaviourBase
    {
        public override void OnPickup(IItemOwnerBehaviour owner)
        {
            IAuthoritativeHealth health = owner
                .GetBehaviour<IHealthBehaviour>()
                .Require<IAuthoritativeHealth>();

            health.Heal(40);
        }
    }
}
