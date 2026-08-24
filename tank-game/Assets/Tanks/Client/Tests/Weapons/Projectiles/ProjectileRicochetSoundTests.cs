using NSubstitute;
using NUnit.Framework;
using Tanks.Audio;
using Tanks.Client.Weapons;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Mathematics;

namespace Tanks.Client.Tests.Weapons.Projectiles
{
    public class ProjectileRicochetSoundTests
    {
        private IAudioPlayer _audioPlayer;
        private ProjectileRicochetSound _component;

        [SetUp]
        public void Setup()
        {
            _audioPlayer = Substitute.For<IAudioPlayer>();
            _component = new ProjectileRicochetSound(_audioPlayer);
            _component.Initialize(Substitute.For<IProjectileBehaviour>());
        }

        [TestCase(10, 2)]
        [TestCase(-5, 1)]
        [TestCase(1, 0)]
        [TestCase(0, 0)]
        public void Fire_ShouldPlaySound(float x, float y)
        {
            _component.Handle(new ProjectileRicocheted { HitPoint = new float2(x, y) });
            _audioPlayer.Received(1).PlayOneShot(Arg.Any<string>(), new float2(x, y));
        }
    }
}
