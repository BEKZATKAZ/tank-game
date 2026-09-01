using Tanks.Shared;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Items.Pickups;
using Tanks.Shared.Stats;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Server.Items.Pickups
{
    public sealed class PickupArea : NetworkComponent<IPickupBehaviour>
    {
        public PickupArea(IItemDatabase database)
        {
            _database = database;
            _radius = 1.5f;
            _layerMask = 1 << 3;
        }

        private readonly IItemDatabase _database;
        private readonly float _radius;
        private readonly int _layerMask;

        public bool CheckArea(out IItemOwnerBehaviour detected)
        {
            float2 origin = Transform.WorldPosition;
            Collider2D target = Physics2D.OverlapCircle(origin, _radius, _layerMask);
            detected = null;

            if (target == false) return false;
            if (target.TryGetComponent(out detected) == false) return false;

            return true;
        }

        public void PickUp(
            IItemOwnerBehaviour owner, in ItemIdentifier itemId, in PickupId pickupId)
        {
            IStatOwnerBehaviour statOwner = owner.GetBehaviour<IStatOwnerBehaviour>();
            IItemConfig config = _database.Get(itemId);
            owner.PickupItem(config, pickupId);

            foreach (ItemStat stat in config.Stats)
            {
                if (stat.ApplyOnPickUp == false) continue;
                statOwner.AddStat(stat.Type, stat.ValueType, stat.Value, stat.Duration);
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
