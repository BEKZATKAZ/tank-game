using Tanks.Shared;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Items.Pickups;
using Tanks.Shared.Stats;
using Tanks.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Server.Items.Pickups
{
    public sealed class PickupArea : NetworkComponent<IPickupBehaviour>
    {
        public PickupArea(IItemDatabase database)
        {
            _database = database;
        }

        private readonly IItemDatabase _database;

        public bool CheckArea(out IItemOwnerBehaviour detected)
        {
            float2 origin = Transform.WorldPosition;

            using TemporaryArrayResult<Collider2D> buffer = new(8);

            int count = Physics2D.OverlapCircle(origin, radius: 0.1f, new ContactFilter2D
            {
                useTriggers = false,
                layerMask = 1 << 3
            }, buffer.Value.Array);

            detected = null;

            if (count <= 0) return false;

            for (int i = 0; i < count; i++)
            {
                if (buffer[i].TryGetComponent(out detected)) return true;
            }

            return false;
        }

        public void PickUp(
            IItemOwnerBehaviour owner, in ItemIdentifier itemId, in PickupId pickupId)
        {
            IStatOwnerBehaviour statOwner = owner.GetBehaviour<IStatOwnerBehaviour>();
            IItemConfig config = _database.Get(itemId);
            owner.PickupItem(config, pickupId);

            foreach ((StatType type, PlayerStat stat) in config.PlayerStats)
            {
                statOwner.AddStat(type, stat.ValueType, stat.Value, stat.Duration);
            }
        }

        protected override void OnTick(int tick, float tickDelta)
        {
            if (CheckArea(out IItemOwnerBehaviour owner) == false) return;
            PickUp(owner, Behaviour.ItemId, Behaviour.PickupId);
            Behaviour.DespawnEveryoneRpc();
        }
    }
}
