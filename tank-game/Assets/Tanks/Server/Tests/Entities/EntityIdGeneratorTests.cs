using NUnit.Framework;
using System.Collections.Generic;
using Tanks.Server.Entities;
using Tanks.Shared.Entities;

namespace Tanks.Server.Tests.Entities
{
    public class EntityIdGeneratorTests
    {
        [Test]
        public void GenerateNext_Unique()
        {
            HashSet<EntityId> ids = new HashSet<EntityId>(1000);

            for (int i = 0; i < 1000; i++)
            {
                if (ids.Add(EntityIdGenerator.GenerateNext())) continue;
                Assert.Fail("Entity ID duplicate detected");
            }
        }
    }
}
