using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Tanks.Core.Debriefing;
using Unity.Mathematics;

namespace Tanks.Infrastructure.Debriefing
{
    public sealed class DebriefingClockService : IDebriefingClockService
    {
        private readonly Stopwatch _clock = Stopwatch.StartNew();

        public float Seconds => (float)_clock.Elapsed.TotalSeconds;

        public Task DelayAsync(float seconds)
        {
            return Task.Delay(TimeSpan.FromSeconds(math.max(0, seconds)));
        }
    }
}
