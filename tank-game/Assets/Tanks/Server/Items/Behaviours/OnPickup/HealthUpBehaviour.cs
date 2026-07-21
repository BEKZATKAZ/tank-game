using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;

namespace Tanks.Server.Items.Behaviours.OnPickup
{
    [ItemBehaviour("health_up")]
    public sealed class HealthUpBehaviour : ItemBehaviourBase
    {
        public override void OnPickup(IItemOwner owner)
        {
            owner.Healable.Heal(40);
        }
    }
}
