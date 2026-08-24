using NSubstitute;
using NUnit.Framework;
using Tanks.Audio;
using Tanks.Client.Weapons;
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
            _component = new WeaponFireSound(_audioPlayer);
            _component.Initialize(Substitute.For<IWeaponHolderBehaviour>());
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Fire_ShouldPlaySound(bool isLocalPlayer)
        {
            ITransform transform = _component.Transform;
            _component.Behaviour.IsLocalPlayer.Returns(isLocalPlayer);

            _component.Handle(new WeaponUsed());
            _audioPlayer.Received(1).PlayOneShotAttached(Arg.Any<string>(), transform, isLocalPlayer);
        }
    }
}
