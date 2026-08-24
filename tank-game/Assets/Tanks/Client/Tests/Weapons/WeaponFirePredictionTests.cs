using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Input;
using Tanks.Client.Weapons;
using Tanks.Shared.Weapons;

namespace Tanks.Client.Tests.Weapons
{
    public class WeaponFirePredictionTests
    {
        private IInputService<TurretInput> _input;
        private IShotIdGenerator _idGenerator;
        private WeaponFirePrediction _component;

        [SetUp]
        public void Setup()
        {
            _input = Substitute.For<IInputService<TurretInput>>();
            _idGenerator = new ShotIdGenerator();
            _component = new WeaponFirePrediction(_input, _idGenerator);
            _component.Initialize(Substitute.For<IWeaponHolderBehaviour>());
        }

        [Test]
        public void Fire_SendsBufferOfLatestShots()
        {
            _component.Behaviour.IsLocalPlayer.Returns(true);
            _input.Read().Returns(new TurretInput { Fire = true });

            // First shot
            int tickBefore = 105;
            ulong ownerId = _component.Behaviour.OwnerClientId;
            ShotId shotIdBefore = _idGenerator.GenerateShotId(ownerId, tickBefore);

            _component.Behaviour.WeaponTick(Arg.Any<float>(), true, shotIdBefore).Returns(true);
            _component.Update(0.01f);
            _component.Tick(tickBefore, 0.05f);

            _component.Behaviour
                .Received(1)
                .SendInputServerRpc(Arg.Is<WeaponInput>(
                    x => AssertClientShot(x.FireBuffer.H, tickBefore, shotIdBefore)));

            // Second shot
            int tickAfter = tickBefore + 5;
            ShotId shotIdAfter = _idGenerator.GenerateShotId(ownerId, tickAfter);
            ClientShot previousShot = new ClientShot(tickBefore, shotIdBefore);

            _component.Behaviour.WeaponTick(Arg.Any<float>(), true, shotIdAfter).Returns(true);
            _component.Update(0.01f);
            _component.Tick(tickAfter, 0.05f);

            _component.Behaviour
                .Received(1)
                .SendInputServerRpc(Arg.Is<WeaponInput>(x =>
                    AssertClientShot(x.FireBuffer.H, tickAfter, shotIdAfter) &&
                    AssertClientShot(previousShot, x.FireBuffer.G)));
        }

        [Test]
        public void Idle_NothingIsSent()
        {
            _component.Behaviour.IsLocalPlayer.Returns(true);
            _input.Read().Returns(new TurretInput { Fire = false });

            _component.Update(0.01f);
            _component.Tick(tick: 99, 0.05f);

            _component.Behaviour.Received(1).WeaponTick(Arg.Any<float>(), false, default);
            _component.Behaviour
                .DidNotReceiveWithAnyArgs()
                .SendInputServerRpc(Arg.Any<WeaponInput>());
        }

        private static bool AssertClientShot(
            ClientShot actual, int expectedTick, ShotId expectedShotId)
        {
            return actual.ClientTick == expectedTick && actual.Id == expectedShotId;
        }

        private static bool AssertClientShot(ClientShot expected, ClientShot actual)
        {
            return actual.ClientTick == expected.ClientTick && actual.Id == expected.Id;
        }
    }
}
