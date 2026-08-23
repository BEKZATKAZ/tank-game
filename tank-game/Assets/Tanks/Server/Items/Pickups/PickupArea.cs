using Tanks.Shared;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Pickups;
using VContainer;

namespace Tanks.Server.Items.Pickups
{
    public sealed class PickupArea : NetworkComponent<PickupBehaviour>
    {
        [Inject] private readonly IItemDatabase _database;

        private PickupAreaLogic _logic;

        protected override void OnInitialize()
        {
            _logic = new PickupAreaLogic(_database, Behaviour.Transform, 1.5f, 1 << 3);
        }

        protected override void OnTick(int tick, float tickDelta)
        {
            if (_logic is null) return;
            if (_logic.CheckArea(out IItemOwner owner) == false) return;

            _logic.PickUp(owner, Behaviour.ItemId, Behaviour.PickupId);
            Behaviour.Spawner.DespawnEveryoneRpc(Behaviour.PickupId);
        }
    }
}
