namespace Tanks.Shared.Items.Pickups
{
    public class PickupBehaviour : NetworkBehaviourExtended
    {
        public PickupSpawnBehaviour Spawner { get; private set; }
        public PickupId PickupId { get; private set; }
        public ItemIdentifier ItemId { get; private set; }

        public void Initialize(PickupSpawned payload, PickupSpawnBehaviour spawner)
        {
            PickupId = payload.PickupId;
            ItemId = payload.ItemId;
            Spawner = spawner;
            EmitMessage(payload);
        }

        protected override void Awake()
        {
            base.Awake();
            ForceInitialize();
        }
    }
}
