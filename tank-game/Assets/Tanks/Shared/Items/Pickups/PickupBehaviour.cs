namespace Tanks.Shared.Items.Pickups
{
    public interface IPickupBehaviour : INetworkBehaviourExtended
    {
        PickupId PickupId { get; }
        ItemIdentifier ItemId { get; }

        void Initialize(PickupSpawned payload, PickupId id, IPickupSpawnBehaviour spawner);
        void DespawnEveryoneRpc();
    }

    public class PickupBehaviour : NetworkBehaviourExtended, IPickupBehaviour
    {
        public PickupId PickupId { get; private set; }
        public ItemIdentifier ItemId { get; private set; }

        private IPickupSpawnBehaviour _spawner;

        public void DespawnEveryoneRpc()
        {
            _spawner.DespawnEveryoneRpc(PickupId);
        }

        public void Initialize(PickupSpawned payload, PickupId id, IPickupSpawnBehaviour spawner)
        {
            PickupId = id;
            ItemId = payload.ItemId;
            _spawner = spawner;
            EmitMessage(payload);
        }

        protected override void Awake()
        {
            base.Awake();
            ForceInitialize();
        }
    }
}
