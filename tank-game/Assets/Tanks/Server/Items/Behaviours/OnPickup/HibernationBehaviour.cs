using System;
using System.Collections.Generic;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;

namespace Tanks.Server.Items.Behaviours.OnPickup
{
    [ItemBehaviour("hibernation")]
    public sealed class HibernationBehaviour : ItemBehaviourBase
    {
        private sealed class PlayerEntry
        {
            public IEffectOwnerBehaviour Effects;
            public Action<EffectType, EffectEndReason> Handler;
            public Action<EffectId, EffectType, EffectEndReason> EntryHandler;
            public EffectId Id;
            public bool Pending;
        }

        public HibernationBehaviour(IDamageService damage) => _damage = damage;

        private readonly IDamageService _damage;
        private readonly Dictionary<IItemOwnerBehaviour, PlayerEntry> _entries = new();
        public override bool OncePerOwner => true;

        public override void OnPickup(IItemOwnerBehaviour owner)
        {
            if (_entries.TryGetValue(owner, out PlayerEntry entry) == false)
            {
                entry = new PlayerEntry
                {
                    Effects = owner.Entity.GetBehaviour<IEffectOwnerBehaviour>()
                };

                if (entry.Effects == null) return;

                PlayerEntry captured = entry;
                entry.Handler = (type, reason) => OnEffectEnded(owner, captured, type, reason);
                entry.EntryHandler = (id, type, reason) =>
                {
                    if (id.Equals(captured.Id) && reason != EffectEndReason.Expired)
                    {
                        captured.Pending = false;
                    }
                };

                entry.Effects.Ended += entry.Handler;
                entry.Effects.EntryEnded += entry.EntryHandler;
                _entries.Add(owner, entry);
            }

            entry.Pending = true;
            entry.Id = entry.Effects.ApplyEffect(
                EffectType.Frozen, Config.GetStat("duration").Value,
                Config.Identifier.ToString(), owner.Entity, restart: true);
        }

        private void OnEffectEnded(
            IItemOwnerBehaviour owner, PlayerEntry entry, EffectType type, EffectEndReason reason)
        {
            if (type != EffectType.Frozen || entry.Pending == false) return;

            entry.Pending = false;

            if (reason != EffectEndReason.Expired) return;

            HealthState state = owner.Entity.GetBehaviour<IHealthBehaviour>().ServerState.Value;
            if (state.Current <= 0) return;

            _damage.Heal(state.Max, owner.Entity);
        }

        public override void OnDestroy(IItemOwnerBehaviour owner)
        {
            if (_entries.Remove(owner, out PlayerEntry entry) == false) return;

            entry.Effects.Ended -= entry.Handler;
            entry.Effects.EntryEnded -= entry.EntryHandler;
        }
    }
}
