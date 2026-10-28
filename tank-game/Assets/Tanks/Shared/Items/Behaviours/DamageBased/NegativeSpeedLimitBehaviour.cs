using Tanks.Shared.Entities;

namespace Tanks.Shared.Items.Behaviours.DamageBased
{
    [ItemBehaviour("negative-speed-limit")]
    public sealed class NegativeSpeedLimitBehaviour : ItemBehaviourBase
    {
        public override void OnDealDamage(
            IEntityBehaviour target, IItemOwnerBehaviour owner, ref float damage)
        {
            if (target != owner.Entity) return;
            float multiplier = Config.GetStat("decrease").Value * 0.01f;
            damage -= damage * multiplier;
        }
    }
}
