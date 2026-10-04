using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Player;
using Tanks.Core.Helpers;
using Tanks.Shared.Stats;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Data;
using Unity.Mathematics;

namespace Tanks.Client.Tests.Player
{
    public class RecoilTests
    {
        private RecoilApplier _component;
        private IWeaponHolderBehaviour _behaviour;
        private IStatOwnerBehaviour _statOwner;
        private IRecoilConfig _recoil;
        private IReadOnlyWeapon _weapon;

        [SetUp]
        public void Setup()
        {
            _component = new RecoilApplier();
            _behaviour = Substitute.For<IWeaponHolderBehaviour>();
            _statOwner = Substitute.For<IStatOwnerBehaviour>();
            _recoil = Substitute.For<IRecoilConfig>();
            _weapon = Substitute.For<IReadOnlyWeapon>();

            _behaviour.IsLocalPlayer.Returns(true);
            _behaviour.GetBehaviour<IStatOwnerBehaviour>().Returns(_statOwner);
            _weapon.Recoil.Returns(_recoil);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Initialize_OtherAndLocalPlayer_DisablesOrEnables(bool isLocalPlayer)
        {
            _behaviour.IsLocalPlayer.Returns(isLocalPlayer);
            _component.Initialize(_behaviour);
            Assert.That(_component.IsEnabled, Is.EqualTo(isLocalPlayer));
        }

        [Test]
        public void Handle_WithNoRecoilResistance_AppliesRecoil()
        {
            ConfigureRecoil(amount: 10, duration: 1);

            HandleWeaponUsed();
            _component.LateUpdate(0.5f);

            Assert.That(_behaviour.Velocity.Linear, Is.EqualTo(new float2(10, 0)));
        }

        [Test]
        public void Handle_WhenAmountIsZero_DoesNotApplyRecoil()
        {
            ConfigureRecoil(amount: 0, duration: 1);

            HandleWeaponUsed();
            _component.LateUpdate(0.1f);

            Assert.That(_behaviour.Velocity.Linear, Is.EqualTo(float2.zero));
        }

        [Test]
        public void LateUpdate_AppliesFullRecoilAtStart()
        {
            ConfigureRecoil(amount: 10, duration: 1);

            HandleWeaponUsed();
            _component.LateUpdate(0);

            Assert.That(_behaviour.Velocity.Linear, Is.EqualTo(new float2(10, 0)));
        }

        [Test]
        public void LateUpdate_InterpolatesRecoilOverDuration()
        {
            ConfigureRecoil(amount: 10, duration: 1);
            HandleWeaponUsed();

            _component.LateUpdate(0.25f);
            Assert.That(_behaviour.Velocity.Linear, Is.EqualTo(new float2(10, 0)));

            _component.LateUpdate(0.25f);
            Assert.That(_behaviour.Velocity.Linear, Is.EqualTo(new float2(17.5f, 0)));
        }

        [Test]
        public void LateUpdate_AfterDuration_DoesNotApplyMoreRecoil()
        {
            ConfigureRecoil(amount: 10, duration: 1);
            HandleWeaponUsed();

            _component.LateUpdate(1);
            _component.LateUpdate(0.1f);

            Assert.That(_behaviour.Velocity.Linear, Is.EqualTo(new float2(10, 0)));
        }

        private void ConfigureRecoil(float amount, float duration)
        {
            _behaviour.IsLocalPlayer.Returns(true);
            _component.Initialize(_behaviour);

            _recoil.Amount.Returns(amount);
            _recoil.DurationSeconds.Returns(duration);
            _statOwner.GetStat(StatType.Resistance, 0).Returns(0);

            _component.Transform.Right.Returns(new float2(1, 0));
            _component.Transform.Up.Returns(new float2(0, 1));
            _behaviour.MuzzlePoint.Right.Returns(new float2(-1, 0));
        }

        private void HandleWeaponUsed()
        {
            _component.Handle(new WeaponUsed
            {
                Weapon = new Required<IReadOnlyWeapon>(_weapon)
            });
        }
    }
}
