namespace Tanks.Shared.Items.Pickups
{
    public interface IPickupBehaviour : INetworkBehaviourExtended
    {
        PickupId PickupId { get; }
        ItemIdentifier ItemId { get; }

        void Initialize(PickupSpawned payload, PickupSpawnBehaviour spawner);
        void DespawnEveryoneRpc();
    }

    public class PickupBehaviour : NetworkBehaviourExtended, IPickupBehaviour
    {
        public PickupId PickupId { get; private set; }
        public ItemIdentifier ItemId { get; private set; }

        private PickupSpawnBehaviour _spawner;

        public void DespawnEveryoneRpc()
        {
            _spawner.DespawnEveryoneRpc(PickupId);
        }

        public void Initialize(PickupSpawned payload, PickupSpawnBehaviour spawner)
        {
            PickupId = payload.PickupId;
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
