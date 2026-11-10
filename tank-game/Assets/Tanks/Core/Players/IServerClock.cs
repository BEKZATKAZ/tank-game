using System;
using System.Collections.Generic;

namespace Tanks.Core.Players
{
    public interface IServerClock
    {
        DateTimeOffset UtcNow { get; }

        void Synchronize(DateTimeOffset utcNow);
    }
}
