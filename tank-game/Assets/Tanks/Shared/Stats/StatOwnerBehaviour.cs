using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;

namespace Tanks.Shared.Stats
{
    public sealed class StatOwnerBehaviour : NetworkBehaviourExtended, IStatOwnerBehaviour
    {
        public StatTypeParametersConfig Config;

        private readonly Dictionary<StatType, Dictionary<StatId, AppliedStat>> _applied = new();
        private int _lastTick;
        private float _tickDelta;

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        private void OverrideStatEveryoneRpc(StatOverriddenFromServer message)
        {
            if (_applied.TryGetValue(message.Type, out var stats) == false ||
                stats.ContainsKey(message.Id) == false)
            {
                OverrideStat(
                    message.Id, message.Type, message.ValueType,
                    message.Value, message.Duration);

                return;
            }

            stats[message.Id] = new AppliedStat(
                _lastTick, message.Value, message.Duration, message.ValueType);
        }

        public bool HasStat(StatId id)
        {
            foreach (var stats in _applied.Values)
            {
                if (stats.ContainsKey(id)) return true;
            }

            return false;
        }

        public void OverrideStat(
            StatId id, StatType type, StatValueType valueType,
            float value, float duration = -1)
        {
            if (_applied.TryGetValue(type, out var stats) == false)
            {
                stats = new Dictionary<StatId, AppliedStat>();
                _applied.Add(type, stats);
            }

            stats[id] = new AppliedStat(_lastTick, value, duration, valueType);

            if (IsServer == false) return;
            
            OverrideStatEveryoneRpc(new StatOverriddenFromServer
            {
                Id = id,
                Type = type,
                ValueType = valueType,
                Value = value,
                Duration = duration
            });
        }

        public StatId AddStat(
            StatType type, StatValueType valueType, float value, float duration = -1)
        {
            StatId id = StatIdGenerator.GenerateNext();
            OverrideStat(id, type, valueType, value, duration);
            return id;
        }

        public void RemoveStat(StatId id)
        {
            foreach (var stats in _applied)
            {
                if (stats.Value.Remove(id) == false) continue;

                OverrideStatEveryoneRpc(new StatOverriddenFromServer
                {
                    Duration = 0,
                    Id = id,
                    Type = stats.Key,
                    Value = 0,
                    ValueType = StatValueType.Flat
                });
            }
        }

        public float GetStat(StatType type, float baseValue)
        {
            if (_applied.TryGetValue(type, out var stats) == false) return baseValue;

            StatTypeParameters parameters = Config.GetParameters(type);
            float sum = baseValue;

            foreach (AppliedStat stat in stats.Values)
            {
                sum = math.clamp(sum, parameters.MinValue, parameters.MaxValue);

                _ = stat.ValueType switch
                {
                    StatValueType.Flat => sum += stat.Value,
                    StatValueType.Percent => sum += baseValue * (stat.Value * 0.01f),
                    _ => throw new NotImplementedException()
                };
            }

            return math.clamp(sum, parameters.MinValue, parameters.MaxValue);
        }

        public void GetStatTimers(StatType type, List<StatTimer> results)
        {
            results.Clear();

            if (_applied.TryGetValue(type, out var stats) == false) return;

            foreach ((StatId id, AppliedStat stat) in stats)
            {
                if (stat.Duration <= 0) continue;

                float elapsedSeconds = (_lastTick - stat.AppliedTick) * _tickDelta;
                float remainingSeconds = stat.Duration - elapsedSeconds;

                if (remainingSeconds <= 0) continue;

                results.Add(new StatTimer(id, remainingSeconds));
            }
        }

        public float GetDiminishingReturns(StatType type, float value)
        {
            StatTypeParameters parameters = Config.GetParameters(type);
            int diminishingBase = parameters.DiminishingBase;

            return value switch
            {
                > 0 => diminishingBase / (diminishingBase + value),
                < 0 => 1 + (-value / diminishingBase),
                _ => 1
            };
        }

        public override void OnTick(int tick, float tickDelta)
        {
            _lastTick = tick;
            _tickDelta = tickDelta;

            foreach (var stats in _applied.Values)
            {
                Span<StatId> toRemove = stackalloc StatId[stats.Count];
                int count = 0;

                foreach ((StatId id, AppliedStat stat) in stats)
                {
                    // Infinite
                    if (stat.Duration < 0) continue;

                    int elapsedTicks = tick - stat.AppliedTick;
                    float elapsedSeconds = tickDelta * elapsedTicks;

                    if (elapsedSeconds < stat.Duration) continue;

                    toRemove[count++] = id;
                }

                for (int i = 0; i < count; i++)
                {
                    stats.Remove(toRemove[i]);
                }
            }
        }
    }
}
