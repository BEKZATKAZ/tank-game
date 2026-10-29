using NSubstitute;
using NUnit.Framework;
using Tanks.Audio;
using Tanks.Client.Rendering;
using Tanks.Client.Weapons;
using Tanks.Core.Helpers;
using Tanks.Shared.Adapters;
using Tanks.Shared.Weapons;

namespace Tanks.Client.Tests.Weapons
{
    public class WeaponFireSoundTests
    {
        private IAudioPlayer _audioPlayer;
        private WeaponFireSound _component;

        [SetUp]
        public void Setup()
        {
            _audioPlayer = Substitute.For<IAudioPlayer>();
            _component = new WeaponFireSound(_audioPlayer, Substitute.For<ICameraShakeService>());
            _component.Initialize(Substitute.For<IWeaponHolderBehaviour>());
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Fire_ShouldPlaySound(bool isLocalPlayer)
        {
            ITransform transform = _component.Transform;
            IReadOnlyWeapon weapon = Substitute.For<IReadOnlyWeapon>();

            weapon.Recoil.Amount.Returns(10);
            weapon.Recoil.DurationSeconds.Returns(0.6f);

            _component.Behaviour.Entity.IsPossessed.Returns(isLocalPlayer);
            _component.Handle(new WeaponUsed { Weapon = weapon.AsRequired() });

            _audioPlayer.Received()
                .PlayOneShotAttached(Arg.Any<string>(), transform, isLocalPlayer);
        }
    }
}
