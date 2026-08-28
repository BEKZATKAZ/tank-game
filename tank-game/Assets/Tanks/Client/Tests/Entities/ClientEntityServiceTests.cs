using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Entities;
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
            Assert.Null(service.GetLocalPlayer(), "Should be null when just created");

            IEntity entity = Substitute.For<IEntity>();
            service.SetLocalPlayer(entity);
            Assert.AreEqual(entity, service.GetLocalPlayer());
        }
    }
}
