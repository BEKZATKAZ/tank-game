using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Weapons;
using Tanks.Shared.Weapons;
using Tanks.Utils;

namespace Tanks.Server.Tests.Weapons
{
    public class WeaponUserAuthorityTests
    {
        private WeaponUserAuthority _component;
        private IReadOnlyWeapon _weapon;
        private IReadOnlyCooldown _cooldown;

        [SetUp]
        public void Setup()
        {
            _component = new WeaponUserAuthority();
            _component.Initialize(Substitute.For<IWeaponHolderBehaviour>());

            _weapon = Substitute.For<IReadOnlyWeapon>();
            _cooldown = Substitute.For<IReadOnlyCooldown>();

            _cooldown.DurationSeconds.Returns(0.1f);
            _weapon.Cooldown.Returns(_cooldown);

            _component.Behaviour.CurrentWeapon.Returns(_weapon);
        }

        [TearDown]
        public void TearDown()
        {
            _component.Destroy();
        }

        [Test]
        public void Handle_IgnoresInputAlreadyProcessed()
        {
            _component.Scheduler.LastProcessedClientTick = 10;

            _component.Handle(CreateInput(10, new ShotId(1)));
            _component.Tick(1, 0.1f);

            _component.Behaviour.Received(1).WeaponTick(0.1f, false, default);
        }

        [Test]
        public void Handle_IgnoresOlderInput()
        {
            _component.Scheduler.LastProcessedClientTick = 10;

            _component.Handle(CreateInput(9, new ShotId(1)));
            _component.Tick(1, 0.1f);

            _component.Behaviour.Received(1).WeaponTick(0.1f, false, default);
        }

        [Test]
        public void Handle_AcceptsNewerInput()
        {
            _component.Scheduler.LastProcessedClientTick = 10;

            _component.Handle(CreateInput(11, new ShotId(1)));
            _component.Tick(1, 0.1f);

            _component.Behaviour.Received(1).WeaponTick(0.1f, true, new ShotId(1));
        }

        [Test]
        public void Handle_KeepsNewestInput()
        {
            _component.Handle(CreateInput(10, new ShotId(10)));
            _component.Handle(CreateInput(20, new ShotId(20)));
            _component.Tick(1, 0.1f);

            _component.Behaviour.Received(1)
                .WeaponTick(0.1f, true, new ShotId(20));

            _component.Behaviour.DidNotReceive()
                .WeaponTick(Arg.Any<float>(), true, new ShotId(10));
        }

        [Test]
        public void Handle_DoesNotReplacePendingInputWithOlderInput()
        {
            _component.Handle(CreateInput(20, new ShotId(20)));
            _component.Handle(CreateInput(10, new ShotId(10)));

            _component.Tick(1, 0.1f);
            _component.Behaviour.Received(1).WeaponTick(0.1f, true, new ShotId(20));
        }

        [Test]
        public void Handle_ReplacesPendingInputWithSameTick()
        {
            _component.Handle(CreateInput(10, new ShotId(10)));
            _component.Handle(CreateInput(10, new ShotId(20)));
            _component.Tick(1, 0.1f);

            _component.Behaviour.Received(1).WeaponTick(0.1f, true, new ShotId(20));
            _component.Behaviour.DidNotReceive().WeaponTick(Arg.Any<float>(), true, new ShotId(10));
        }

        [Test]
        public void Tick_ProcessesPendingInputOnlyOnce()
        {
            _component.Handle(CreateInput(10, new ShotId(123)));

            _component.Tick(1, 0.1f);
            _component.Tick(2, 0.1f);

            _component.Behaviour.Received(1).WeaponTick(0.1f, true, new ShotId(123));
        }

        [Test]
        public void Tick_NoPendingInput_UsesFallback()
        {
            _component.Tick(1, 0.1f);
            _component.Behaviour.Received(1).WeaponTick(0.1f, false, default);
        }

        private static WeaponInput CreateInput(int clientTick, ShotId shotId)
        {
            WeaponInput input = new WeaponInput
            {
                FireBuffer = new RingBuffer8<ClientShot>()
                    .Add(new ClientShot(clientTick, shotId))
            };

            return input;
        }
    }
}
