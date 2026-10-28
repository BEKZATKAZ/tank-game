using System.Collections.Generic;
using Tanks.Shared.Entities;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;

namespace Tanks.Server.Items.Behaviours.OnPickup
{
    public sealed class StatsLightPlayerEntry
    {
        public EntityId TargetId;
        public int NextTypeIndex;
        public int? LastTick;
        public StatId? ActiveStatId;
    }

    [ItemBehaviour("stats-light")]
    public sealed class StatsLightBehaviour : ItemBehaviourBase
    {
        private readonly StatType[] _types =
        {
            StatType.Speed,
            StatType.Damage,
            StatType.Defense,
            StatType.RateOfFire,
            StatType.Resistance
        };

        private readonly List<StatsLightPlayerEntry> _entries = new();

        private bool TryGetBoost(StatType type, out ItemStat boost)
        {
            try
            {
                string name = type switch
                {
                    StatType.Damage => "damage-boost",
                    StatType.Speed => "speed-boost",
                    StatType.Defense => "defense-boost",
                    StatType.RateOfFire => "rate-of-fire-boost",
                    StatType.Resistance => "resistance-boost",
                    _ => null
                };

                if (name is null)
                {
                    boost = default;
                    return false;
                }

                boost = Config.GetStat(name);
                return true;
            }
            catch (KeyNotFoundException)
            {
                UnityEngine.Debug.LogWarningFormat("Boost not found for stat '{0}'", type);
                boost = default;
                return false;
            }
        }

        public override void OnPickup(IItemOwnerBehaviour owner)
        {
            if (_entries.Exists(x => x.TargetId == owner.Entity.Id)) return;
            _entries.Add(new StatsLightPlayerEntry { TargetId = owner.Entity.Id });
        }

        public override void OnDestroy(IItemOwnerBehaviour owner)
        {
            int index = _entries.FindIndex(x => x.TargetId == owner.Entity.Id);
            if (index == -1) return;
            _entries.RemoveAt(index);
        }

        public override void OnTick(IItemOwnerBehaviour owner, int tick, float tickDelta)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                StatsLightPlayerEntry entry = _entries[i];
                if (entry.TargetId != owner.Entity.Id) continue;

                float elapsed = entry.LastTick.HasValue
                    ? (tick - entry.LastTick.Value) * tickDelta
                    : float.PositiveInfinity;
                float intervalSeconds = Config.GetStat("interval").Value;

                if (elapsed < intervalSeconds) return;
                IStatOwnerBehaviour statOwner = owner.GetBehaviour<IStatOwnerBehaviour>();

                if (statOwner is null) return;
                StatType type = _types[entry.NextTypeIndex];

                if (TryGetBoost(type, out ItemStat boost))
                {
                    if (entry.ActiveStatId is StatId id) statOwner.RemoveStat(id);
                    entry.ActiveStatId = statOwner.AddStat(type, boost.ValueType, boost.Value, intervalSeconds);
                    entry.LastTick = tick;
                }

                if (++entry.NextTypeIndex < _types.Length) return;
                entry.NextTypeIndex = 0;

                return;
            }
        }
    }
}
