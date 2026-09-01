using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Entities;
using Tanks.Core.Helpers;
using Tanks.Shared.Entities;
using UnityEngine;

namespace Tanks.Client.Tests.Entities
{
    public class ClientEntityServiceTests : MonoBehaviour
    {
        [Test]
        public void LocalPlayer_SetsCorrectly()
        {
            IClientEntityService service = new ClientEntityService();
            Assert.Null(service.GetPossessedPlayer(), "Should be null when just created");

            IEntityBehaviour entity = Substitute.For<IEntityBehaviour>();
            service.PossessPlayer(entity.AsRequired());
            Assert.AreEqual(entity, service.GetPossessedPlayer());
        }
    }
}
