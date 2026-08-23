using Tanks.Audio;
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
        [Inject] private readonly IAudioPlayer _audioPlayer;

        public void Handle(ItemPickedUp message)
        {
            IClientItemConfig config = _database.Get(message.ItemId);

            _audioPlayer.PlayOneShotAttached(
                path: config.OnPickUpSound,
                target: Behaviour.Transform,
                useDefault: Behaviour.IsLocalPlayer);
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
