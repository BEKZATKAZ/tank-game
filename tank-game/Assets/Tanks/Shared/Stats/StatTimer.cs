namespace Tanks.Shared.Stats
{
    public readonly struct StatTimer
    {
        public StatTimer(StatId id, float remainingSeconds)
        {
            Id = id;
            RemainingSeconds = remainingSeconds;
        }

        public readonly StatId Id;
        public readonly float RemainingSeconds;
    }
}
