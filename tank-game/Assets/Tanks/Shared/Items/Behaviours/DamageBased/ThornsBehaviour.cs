using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Unity.Mathematics;

namespace Tanks.Shared.Items.Behaviours.DamageBased
{
    [ItemBehaviour("thorns")]
    public sealed class ThornsBehaviour : ItemBehaviourBase
    {
        public ThornsBehaviour(IDamageService damageService)
        {
            _damageService = damageService;
        }

        private readonly IDamageService _damageService;

        public override void OnHealthChanged(
            IItemOwnerBehaviour owner, IEntityBehaviour from,
            HealthState oldState, HealthState newState)
        {
            if (from is null || from == owner.Entity) return;

            float damage = oldState.Current - newState.Current;
            if (damage <= 0) return;

            float multiplier = Config.GetStat("damage").Value * 0.01f;
            damage *= multiplier;

            uint reflected = (uint)math.ceil(damage);
            _damageService.Damage(reflected, from, owner.Entity, includeStats: false);
        }
    }
}
