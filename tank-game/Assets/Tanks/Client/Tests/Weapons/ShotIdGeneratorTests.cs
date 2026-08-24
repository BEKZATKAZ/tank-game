using NUnit.Framework;
using System.Collections.Generic;
using Tanks.Client.Weapons;
using Tanks.Shared.Weapons;

namespace Tanks.Client.Tests.Weapons
{
    public class ShotIdGeneratorTests
    {
        [Test]
        public void SameTicks_DifferentClients_ReturnsUnique()
        {
            IShotIdGenerator generator = new ShotIdGenerator();
            HashSet<ShotId> ids = new HashSet<ShotId>();

            for (ulong clientId = 1; clientId <= 4; clientId++)
            {
                for (int tick = 1; tick <= 999; tick++)
                {
                    ShotId id = generator.GenerateShotId(clientId, tick);
                    if (ids.Add(id)) continue;
                    Assert.Fail($"Duplicate detected '{id}'");
                }
            }
        }
    }
}
