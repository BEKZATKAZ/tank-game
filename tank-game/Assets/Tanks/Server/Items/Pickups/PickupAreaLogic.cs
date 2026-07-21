using Tanks.Shared.Adapters;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Items.Pickups;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Server.Items.Pickups
{
    public sealed class PickupAreaLogic
    {
        public PickupAreaLogic(
            IItemDatabase database, ITransform pickupTransfrom, float radius, int layerMask)
        {
            _database = database;
            _pickupTransform = pickupTransfrom;
            _radius = radius;
            _layerMask = layerMask;
        }

        private readonly IItemDatabase _database;
        private readonly ITransform _pickupTransform;
        private readonly float _radius;
        private readonly int _layerMask;

        public bool CheckArea(out IItemOwner detected)
        {
            float2 origin = _pickupTransform.WorldPosition;
            Collider2D target = Physics2D.OverlapCircle(origin, _radius, _layerMask);
            detected = null;

            if (target == false) return false;
            if (target.TryGetComponent(out detected) == false) return false;

            return true;
        }

        public void PickUp(IItemOwner owner, in ItemIdentifier itemId, in PickupId pickupId)
        {
            IItemConfig config = _database.Get(itemId);
            owner.PickupItem(config, pickupId);

            foreach (ItemStat stat in config.Stats)
            {
                if (stat.ApplyOnPickUp == false) continue;
                owner.StatOwner.AddStat(stat.Type, stat.ValueType, stat.Value, stat.Duration);
            }
        }
    }
}
