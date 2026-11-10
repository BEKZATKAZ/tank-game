using System;
using System.Diagnostics;
using Tanks.Core.Players;

namespace Tanks.Infrastructure
{
    public sealed class ServerClock : IServerClock
    {
        private readonly Stopwatch _elapsed = new();
        private DateTimeOffset _utcNow;

        public DateTimeOffset UtcNow => _utcNow + _elapsed.Elapsed;

        public void Synchronize(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
            _elapsed.Restart();
        }
    }
}
