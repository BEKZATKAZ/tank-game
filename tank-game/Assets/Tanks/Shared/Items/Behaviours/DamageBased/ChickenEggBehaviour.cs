using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;

namespace Tanks.Shared.Items.Behaviours.DamageBased
{
    [ItemBehaviour("chicken-egg")]
    public sealed class ChickenEggBehaviour : ItemBehaviourBase
    {
        public override void OnDealDamage(
            IEntityBehaviour target, IItemOwnerBehaviour owner, ref float damage)
        {
            HealthState state = target.GetBehaviour<IHealthBehaviour>().ServerState.Value;

            float remainingPercent = state.NormalizedCurrent * 100;
            float threshold = Config.GetStat("threshold").Value;

            if (remainingPercent < threshold) return;

            damage += damage * Config.GetStat("damage").Value * 0.01f;
        }
    }
}
