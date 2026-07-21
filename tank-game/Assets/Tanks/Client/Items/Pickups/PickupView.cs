using Tanks.Shared;
using Tanks.Shared.Items.Pickups;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Items.Pickups
{
    public sealed class PickupView :
        NetworkComponent<PickupBehaviour>,
        INetworkMessageHandler<PickupSpawned>
    {
        [Inject] private readonly IClientItemDatabase _database;
        
        private PickupVisualLogic _logic;

        public void Handle(PickupSpawned message)
        {
            IClientItemConfig config = _database.Get(message.ItemId);
            _logic.Reset(config);
        }

        protected override void Awake()
        {
            base.Awake();

            PickupVisualSetup setup = new PickupVisualSetup();
            setup.Setup(transform, out var iconRenderer, out var glowRenderer);

            _logic = new PickupVisualLogic(iconRenderer, glowRenderer);
            Behaviour.RegisterMessageHandler(this);
        }

        private void Update()
        {
            _logic.Tick(Time.deltaTime);
        }
    }
}
