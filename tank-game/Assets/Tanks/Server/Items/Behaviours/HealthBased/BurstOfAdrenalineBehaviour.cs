using System.Collections.Generic;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;

namespace Tanks.Server.Items.Behaviours.HealthBased
{
    [ItemBehaviour("burst-of-adrenaline")]
    public sealed class BurstOfAdrenalineBehaviour : ItemBehaviourBase
    {
        private readonly HashSet<EntityId> _activated = new();

        public override void OnPickup(IItemOwnerBehaviour owner)
        {
            IHealthBehaviour health = owner.GetBehaviour<IHealthBehaviour>();
            if (health is null) return;
            TryActivate(owner, health.ServerState.Value);
        }

        public override void OnHealthChanged(
            IItemOwnerBehaviour owner, IEntityBehaviour from,
            HealthState oldState, HealthState newState)
        {
            TryActivate(owner, newState);
        }

        public override void OnDestroy(IItemOwnerBehaviour owner) => _activated.Remove(owner.Entity.Id);
        public override void Reset() => _activated.Clear();

        private void TryActivate(IItemOwnerBehaviour owner, HealthState state)
        {
            if (state.Current <= 0 || state.Current >= Config.GetStat("health-threshold").Value) return;
            if (_activated.Contains(owner.Entity.Id)) return;

            IStatOwnerBehaviour statOwner = owner.GetBehaviour<IStatOwnerBehaviour>();
            if (statOwner is null) return;

            float duration = Config.GetStat("duration").Value;
            ItemStat speed = Config.GetStat("speed-boost");
            ItemStat rateOfFire = Config.GetStat("rate-of-fire-boost");

            statOwner.AddStat(StatType.Speed, speed.ValueType, speed.Value, duration);
            statOwner.AddStat(StatType.RateOfFire, rateOfFire.ValueType, rateOfFire.Value, duration);
            _activated.Add(owner.Entity.Id);
        }
    }
}
