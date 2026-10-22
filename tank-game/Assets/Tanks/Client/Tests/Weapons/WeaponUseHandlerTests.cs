using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Weapons;
using Tanks.Shared.Adapters;
using Tanks.Shared.Weapons;

namespace Tanks.Client.Tests.Weapons
{
    public class WeaponUseHandlerTests
    {
        private WeaponUseHandler _component;
        private IWeaponHolderBehaviour _behaviour;
        private IReadOnlyWeapon _weapon;

        [SetUp]
        public void Setup()
        {
            _weapon = Substitute.For<IReadOnlyWeapon>();
            _weapon.Cooldown.Returns(Substitute.For<IReadOnlyCooldown>());

            _behaviour = Substitute.For<IWeaponHolderBehaviour>();
            _behaviour.GetDeltaMultiplier().Returns(1);
            _behaviour.CurrentWeapon.Returns(_weapon);
            _behaviour.IsLocalPlayer.Returns(false);

            _component = new WeaponUseHandler();
            _component.Initialize(_behaviour);
        }

        [TearDown]
        public void TearDown()
        {
            _component.Destroy();
        }

        [Test]
        public void Tick_WhenNoShotsAreScheduled_DoesNotHoldWeapon()
        {
            _component.Tick(10, 0.1f);
            _behaviour.Received(1).WeaponTick(0.1f, false, default);
        }

        [Test]
        public void Handle_SchedulesShot()
        {
            _weapon.Cooldown.DurationSeconds.Returns(0.5f);

            _component.Tick(10, 0.1f);
            _component.Handle(new WeaponUsedFromServer
            {
                Id = new ShotId(123),
                Direction = Angle.FromDegrees(75)
            });
            _component.Tick(15, 0.1f);

            _behaviour.Received(1).WeaponTick(0.1f, true, new ShotId(123), Angle.FromDegrees(75));
        }

        [Test]
        public void Tick_AtScheduledTick_UsesWeapon()
        {
            _weapon.Cooldown.DurationSeconds.Returns(0.5f);

            _component.Tick(10, 0.1f);
            _component.Handle(new WeaponUsedFromServer { Id = new ShotId(123) });
            _component.Tick(15, 0.1f);

            _behaviour.Received(1).WeaponTick(0.1f, true, new ShotId(123), default);
        }

        [Test]
        public void Tick_ConsumesScheduledShot()
        {
            _weapon.Cooldown.DurationSeconds.Returns(0.5f);

            _component.Tick(10, 0.1f);
            _component.Handle(new WeaponUsedFromServer { Id = new ShotId(123) });

            _component.Tick(15, 0.1f);
            _behaviour.ClearReceivedCalls();
            _component.Tick(16, 0.1f);

            _behaviour.DidNotReceive().WeaponTick(0.1f, true, new ShotId(123), Arg.Any<Angle>());
            _behaviour.Received(1).WeaponTick(0.1f, false, default);
        }

        [Test]
        public void Handle_RespectsCooldownBetweenShots()
        {
            _weapon.Cooldown.DurationSeconds.Returns(0.5f);
            _component.Tick(10, 0.1f);

            _component.Handle(new WeaponUsedFromServer { Id = new ShotId(1) });
            _component.Handle(new WeaponUsedFromServer { Id = new ShotId(2) });

            for (int tick = 10; tick <= 15; tick++) _component.Tick(tick, 0.1f);

            _behaviour.Received(1).WeaponTick(0.1f, true, new ShotId(1), default);
            _behaviour.Received(1).WeaponTick(0.1f, true, new ShotId(2), default);
        }

        [Test]
        public void Handle_UsesCurrentTick_WhenCooldownHasAlreadyElapsed()
        {
            _weapon.Cooldown.DurationSeconds.Returns(0.5f);

            _component.Tick(10, 0.1f);
            _component.Handle(new WeaponUsedFromServer { Id = new ShotId(123) });

            _component.Tick(20, 0.1f);
            _component.Handle(new WeaponUsedFromServer { Id = new ShotId(456) });

            _component.Tick(20, 0.1f);
            _behaviour.Received(1).WeaponTick(0.1f, true, new ShotId(456), default);
        }

        [Test]
        public void Initialize_WhenLocalPlayer_DisablesComponent()
        {
            _behaviour.IsLocalPlayer.Returns(true);

            WeaponUseHandler component = new WeaponUseHandler();
            component.Initialize(_behaviour);
            Assert.False(component.IsEnabled);

            component.Destroy();
            _behaviour.DidNotReceive().RegisterMessageHandler(component);
        }
    }
}
