using NSubstitute;
using NUnit.Framework;
using Tanks.Audio;
using Tanks.Client.Items;
using Tanks.Client.Items.Pickups;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Pickups;

namespace Tanks.Client.Tests.Items.Pickups
{
    public class PickupSpawnSoundTests
    {
        private IClientItemDatabase _database;
        private IAudioFactory _audioFactory;
        private PickupSpawnSound _component;

        [SetUp]
        public void Setup()
        {
            _database = Substitute.For<IClientItemDatabase>();
            _audioFactory = Substitute.For<IAudioFactory>();

            _component = new PickupSpawnSound(_database, _audioFactory);
            _component.Initialize(Substitute.For<IPickupBehaviour>());
        }

        [TearDown]
        public void TearDown()
        {
            _component.Destroy();
        }

        [Test]
        public void Spawn_ValidSound_ShouldPlaySound()
        {
            ItemIdentifier id = new ItemIdentifier("test");
            IClientItemConfig config = Substitute.For<IClientItemConfig>();
            IAudioInstance instance = Substitute.For<IAudioInstance>();

            config.OnSpawnSound.Returns("spawn-sound");
            _database.Get(id).Returns(config);

            _audioFactory.CreateInstance(Arg.Any<string>()).Returns(instance);
            _component.Handle(new PickupSpawned { ItemId = id });

            instance.Received(1).AttachTo(_component.Transform);
            instance.Received(1).Play();
            instance.DidNotReceive().Dispose();

            // Dispose previous
            _component.Handle(new PickupSpawned { ItemId = id });

            instance.Received(1).Stop(Arg.Any<bool>());
            instance.Received(1).Dispose();
        }

        [Test]
        public void Spawn_InvalidSound_ShouldSkipSound()
        {
            ItemIdentifier id = new ItemIdentifier("test");
            _component.Handle(new PickupSpawned { ItemId = id });
            _audioFactory.DidNotReceiveWithAnyArgs().CreateInstance(Arg.Any<string>());
        }
    }
}
