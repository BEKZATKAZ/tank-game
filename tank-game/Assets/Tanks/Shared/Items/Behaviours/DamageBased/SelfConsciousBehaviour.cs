using Tanks.Shared.Entities;

namespace Tanks.Shared.Items.Behaviours.DamageBased
{
    [ItemBehaviour("self-conscious")]
    public sealed class SelfConsciousBehaviour : ItemBehaviourBase
    {
        public override void OnDealDamage(
            IEntityBehaviour target, IItemOwnerBehaviour owner, ref float damage)
        {
            if (target != owner.Entity) return;
            float multiplier = Config.GetStat("damage").Value * 0.01f;
            damage += damage * multiplier;
        }
    }
}
