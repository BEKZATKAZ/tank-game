using FMODUnity;
using Tanks.Shared;
using Tanks.Shared.Items;
using VContainer;

namespace Tanks.Client.Items.Pickups
{
    public sealed class PickupSound :
        NetworkComponent<ItemOwnerBehaviour>,
        INetworkMessageHandler<ItemPickedUp>
    {
        [Inject] private readonly IClientItemDatabase _database;

        public void Handle(ItemPickedUp message)
        {
            IClientItemConfig config = _database.Get(message.ItemId);

            if (config.OnPickUpSound.IsNull) return;

            RuntimeManager.PlayOneShotAttached(config.OnPickUpSound, gameObject);
        }

        protected override void Awake()
        {
            base.Awake();
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
