using NUnit.Framework;
using System.Collections.Generic;

namespace Tanks.Server.Tests
{
    public class ConnectedClientServiceTests
    {
        [Test]
        public void AddClient_StoresData()
        {
            IConnectedClientService service = new ConnectedClientService();

            Assert.AreEqual(0, service.ConnectedClientCount,
                "How do we have players if we just created the service?");

            service.AddClient(9, new ClientData("test1"));
            service.AddClient(10, new ClientData("test2"));

            Assert.AreEqual(2, service.ConnectedClientCount, "Player count dismatch");
            Assert.AreEqual(new ulong[] { 9, 10 }, service.GetConnectedClientIds());

            Assert.AreEqual("test1", service.GetClientData(9).DisplayName);
            Assert.AreEqual("test2", service.GetClientData(10).DisplayName);

            service.RemoveClient(10);

            Assert.AreEqual(1, service.ConnectedClientCount, "Player count dismatch");
            Assert.AreEqual(new ulong[] { 9 }, service.GetConnectedClientIds());

            Assert.AreEqual("test1", service.GetClientData(9).DisplayName);
            Assert.Throws<KeyNotFoundException>(() => service.GetClientData(10));
        }
    }
}
