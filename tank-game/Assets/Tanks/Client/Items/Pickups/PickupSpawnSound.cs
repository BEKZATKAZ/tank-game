using FMOD.Studio;
using FMODUnity;
using Tanks.Shared;
using Tanks.Shared.Items.Pickups;
using VContainer;

namespace Tanks.Client.Items.Pickups
{
    public sealed class PickupSpawnSound :
        NetworkComponent<PickupBehaviour>,
        INetworkMessageHandler<PickupSpawned>
    {
        [Inject] private readonly IClientItemDatabase _database;

        private EventInstance _instance;

        public void Handle(PickupSpawned message)
        {
            IClientItemConfig config = _database.Get(message.ItemId);

            if (_instance.isValid())
            {
                _instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                _instance.release();
            }

            if (config.OnSpawnSound.IsNull) return;

            _instance = RuntimeManager.CreateInstance(config.OnSpawnSound);
            RuntimeManager.AttachInstanceToGameObject(_instance, gameObject);

            _instance.start();
        }

        protected override void Awake()
        {
            base.Awake();
            Behaviour.RegisterMessageHandler(this);
        }

        private void OnDisable()
        {
            if (_instance.isValid() == false) return;
            _instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            _instance.release();
        }
    }
}
