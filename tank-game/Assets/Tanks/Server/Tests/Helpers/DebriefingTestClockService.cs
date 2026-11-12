using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tanks.Core.Debriefing;

namespace Tanks.Server.Tests.Helpers
{
    public sealed class DebriefingTestClockService : IDebriefingClockService
    {
        private readonly List<(float Due,
            TaskCompletionSource<bool> Completion)> _waits =
            new();

        public float Seconds { get; private set; }

        public Task DelayAsync(float seconds)
        {
            if (seconds <= 0) return Task.CompletedTask;
            TaskCompletionSource<bool> completion = new();

            _waits.Add((Seconds + seconds, completion));
            return completion.Task;
        }

        public void Advance(float seconds)
        {
            Seconds += seconds;

            foreach ((float Due, TaskCompletionSource<bool> Completion) wait in
                _waits.Where(wait => wait.Due <= Seconds).ToArray())
            {
                _waits.Remove(wait);
                wait.Completion.TrySetResult(true);
            }
        }
    }
}
