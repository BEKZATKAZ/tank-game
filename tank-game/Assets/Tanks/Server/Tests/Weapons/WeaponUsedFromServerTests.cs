using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Weapons;
using Tanks.Shared.Weapons;

namespace Tanks.Server.Tests.Weapons
{
    public class WeaponUsedFromServerTests
    {
        private WeaponUsedFromServerSender _component;

        [SetUp]
        public void Setup()
        {
            _component = new WeaponUsedFromServerSender();
            _component.Initialize(Substitute.For<IWeaponHolderBehaviour>());
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(10)]
        [TestCase(100)]
        public void Handle_SendsClientRpc(int id)
        {
            _component.Handle(new WeaponUsed { ShotId = new ShotId(id) });

            _component.Behaviour
                .Received(1)
                .UseWeaponClientRpc(new WeaponUsedFromServer { Id = new ShotId(id) });
        }
    }
}
