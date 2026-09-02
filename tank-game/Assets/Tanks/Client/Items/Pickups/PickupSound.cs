using Tanks.Audio;
using Tanks.Shared;
using Tanks.Shared.Entities;
using Tanks.Shared.Items;

namespace Tanks.Client.Items.Pickups
{
    public sealed class PickupSound :
        NetworkComponent<ItemOwnerBehaviour>,
        INetworkMessageHandler<ItemPickedUp>
    {
        public PickupSound(IClientItemDatabase database, IAudioPlayer audioPlayer)
        {
            _database = database;
            _audioPlayer = audioPlayer;
        }

        private readonly IClientItemDatabase _database;
        private readonly IAudioPlayer _audioPlayer;

        public void Handle(ItemPickedUp message)
        {
            IEntityBehaviour entity = Behaviour.GetBehaviour<IEntityBehaviour>();
            IClientItemConfig config = _database.Get(message.ItemId);

            _audioPlayer.PlayOneShotAttached(
                path: config.OnPickUpSound,
                target: Transform,
                useDefault: entity.IsPossessed);
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
