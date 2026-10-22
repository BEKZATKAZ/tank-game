using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Weapons;
using Tanks.Shared.Adapters;
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
            IWeaponHolderBehaviour owner = Substitute.For<IWeaponHolderBehaviour>();
            owner.ShotDirection.Returns((Angle?)Angle.FromDegrees(75));

            _component.Handle(new WeaponUsed { Owner = owner, ShotId = new ShotId(id) });

            _component.Behaviour
                .Received(1)
                .UseWeaponClientRpc(Arg.Is<WeaponUsedFromServer>(message =>
                    message.Id == new ShotId(id) &&
                    message.Direction == Angle.FromDegrees(75)));
        }
    }
}
