using System.Collections.Generic;
using Tanks.Shared.Entities;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;

namespace Tanks.Server.Items.Behaviours.OnPickup
{
    [ItemBehaviour("dna-reconstruction")]
    public sealed class DNAReconstructionBehaviour : ItemBehaviourBase
    {
        private sealed class PlayerEntry
        {
            public IStatOwnerBehaviour StatOwner;
            public readonly List<StatId> StatIds = new();
        }

        private readonly StatType[] _types =
        {
            StatType.Damage,
            StatType.Speed,
            StatType.Defense,
            StatType.Resistance,
            StatType.RateOfFire
        };

        private readonly string[] _statNames =
        {
            "damage", "speed", "defense", "resistance", "rate-of-fire"
        };

        private readonly Dictionary<EntityId, PlayerEntry> _entries = new();

        public override void OnPickup(IItemOwnerBehaviour owner)
        {
            IStatOwnerBehaviour statOwner = owner.GetBehaviour<IStatOwnerBehaviour>();
            if (statOwner is null) return;

            int boostIndex = UnityEngine.Random.Range(0, _types.Length);
            int penaltyIndex = UnityEngine.Random.Range(0, _types.Length - 1);
            if (penaltyIndex >= boostIndex) penaltyIndex++;

            ItemStat boost = Config.GetStat(_statNames[boostIndex] + "-boost");
            ItemStat penalty = Config.GetStat(_statNames[penaltyIndex] + "-penalty");

            if (_entries.TryGetValue(owner.Entity.Id, out PlayerEntry entry) == false)
            {
                entry = new PlayerEntry { StatOwner = statOwner };
                _entries.Add(owner.Entity.Id, entry);
            }

            entry.StatIds.Add(statOwner.AddStat(_types[boostIndex], boost.ValueType, boost.Value));
            entry.StatIds.Add(statOwner.AddStat(_types[penaltyIndex], penalty.ValueType, penalty.Value));
        }

        public override void OnDestroy(IItemOwnerBehaviour owner)
        {
            if (_entries.TryGetValue(owner.Entity.Id, out PlayerEntry entry) == false) return;

            RemoveStats(entry);
            _entries.Remove(owner.Entity.Id);
        }

        public override void Reset()
        {
            foreach (PlayerEntry entry in _entries.Values) RemoveStats(entry);
            _entries.Clear();
        }

        private void RemoveStats(PlayerEntry entry)
        {
            foreach (StatId id in entry.StatIds) entry.StatOwner.RemoveStat(id);
        }
    }
}
