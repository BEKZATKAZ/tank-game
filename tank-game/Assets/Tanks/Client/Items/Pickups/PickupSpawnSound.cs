using Tanks.Audio;
using Tanks.Shared;
using Tanks.Shared.Items.Pickups;

namespace Tanks.Client.Items.Pickups
{
    public sealed class PickupSpawnSound :
        NetworkComponent<IPickupBehaviour>,
        INetworkMessageHandler<PickupSpawned>
    {
        public PickupSpawnSound(IClientItemDatabase database, IAudioFactory audioFactory)
        {
            _database = database;
            _audioFactory = audioFactory;
        }

        private readonly IClientItemDatabase _database;
        private readonly IAudioFactory _audioFactory;

        private IAudioInstance _instance;

        public void Handle(PickupSpawned message)
        {
            IClientItemConfig config = _database.Get(message.ItemId);

            if (_instance != null)
            {
                _instance.Stop(fadeOut: false);
                _instance.Dispose();
            }

            if (string.IsNullOrEmpty(config.OnSpawnSound)) return;

            _instance = _audioFactory.CreateInstance(config.OnSpawnSound);
            _instance.AttachTo(Transform);
            _instance.Play();
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }

        protected override void OnDisable()
        {
            if (_instance is null) return;
            _instance.Stop(fadeOut: true);
            _instance.Dispose();
        }
    }
}
