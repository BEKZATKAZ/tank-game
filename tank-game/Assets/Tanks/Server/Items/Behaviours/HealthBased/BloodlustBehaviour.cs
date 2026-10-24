using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;
using Unity.Mathematics;

namespace Tanks.Server.Items.Behaviours.HealthBased
{
    [ItemBehaviour("bloodlust")]
    public sealed class BloodlustBehaviour : ItemBehaviourBase
    {
        public BloodlustBehaviour(IDamageService damageService)
        {
            _damageService = damageService;
            StatId = StatIdGenerator.GenerateNext();
        }

        public StatId StatId { get; private set; }

        private readonly IDamageService _damageService;

        public override void OnPickup(IItemOwnerBehaviour owner)
        {
            IStatOwnerBehaviour statOwner = owner.GetBehaviour<IStatOwnerBehaviour>();

            if (statOwner.HasStat(StatId))
            {
                _damageService.Damage(
                    uint.MaxValue, owner.Entity, owner.Entity, includeStats: false);

                return;
            }

            IHealthBehaviour health = owner.GetBehaviour<IHealthBehaviour>();
            Override(statOwner, health.ServerState.Value);
        }

        public override void OnHealthChanged(
            IItemOwnerBehaviour owner, IEntityBehaviour from,
            HealthState oldState, HealthState newState)
        {
            if (newState.Current <= 0) return;
            Override(owner.GetBehaviour<IStatOwnerBehaviour>(), newState);
        }

        private void Override(IStatOwnerBehaviour statOwner, HealthState state)
        {
            float interval = math.max(Config.GetStat("interval").Value, 1);
            int count = (int)((state.Max - state.Current) / interval);

            ItemStat damage = Config.GetStat("damage");
            float fullBonus = count * damage.Value;

            statOwner.OverrideStat(StatId, StatType.Damage, damage.ValueType, fullBonus);
        }
    }
}
