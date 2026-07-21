using System;
using System.Collections.Generic;

namespace Tanks.Shared.Stats
{
    public enum StatValueType
    {
        Flat,
        Percent
    }

    public readonly struct AppliedStat
    {
        public AppliedStat(int tick, float value, float duration, StatValueType valueType)
        {
            AppliedTick = tick;
            Value = value;
            Duration = duration;
            ValueType = valueType;
        }

        public readonly int AppliedTick;
        public readonly float Value;
        public readonly float Duration;
        public readonly StatValueType ValueType;
    }

    public sealed class StatLogic
    {
        private readonly Dictionary<StatType, List<AppliedStat>> _applied = new();
        private int _lastTick;

        public void AddStat(StatType type, StatValueType valueType, float value, float duration = -1)
        {
            if (_applied.TryGetValue(type, out var list) == false)
            {
                list = new List<AppliedStat>();
                _applied.Add(type, list);
            }

            list.Add(new AppliedStat(_lastTick, value, duration, valueType));
        }

        public void AddAddition(StatType type, float value, float duration = -1)
        {
            AddStat(type, StatValueType.Flat, value, duration);
        }

        public void AddPercent(StatType type, float percent, float duration = -1)
        {
            AddStat(type, StatValueType.Percent, percent, duration);
        }

        public float GetStat(StatType type, float baseValue)
        {
            if (_applied.TryGetValue(type, out var list) == false) return baseValue;
            float sum = baseValue;

            foreach (AppliedStat stat in list)
            {
                if (stat.ValueType is StatValueType.Flat)
                {
                    sum += stat.Value;
                    continue;
                }

                if (stat.ValueType is StatValueType.Percent)
                {
                    float normalized = stat.Value * 0.01f; // 100% = 1, 150% = 1.5
                    sum += baseValue * normalized;
                    continue;
                }

                throw new NotImplementedException();
            }

            return sum;
        }

        public void Tick(int tick, float tickDelta)
        {
            _lastTick = tick;

            foreach (var list in _applied.Values)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    AppliedStat stat = list[i];

                    // Infinite
                    if (stat.Duration < 0) continue;

                    int elapsedTicks = tick - stat.AppliedTick;
                    float elapsedSeconds = tickDelta * elapsedTicks;

                    if (elapsedSeconds < stat.Duration) continue;

                    list.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}