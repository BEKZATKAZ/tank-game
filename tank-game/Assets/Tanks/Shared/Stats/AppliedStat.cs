namespace Tanks.Shared.Stats
{
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
}
