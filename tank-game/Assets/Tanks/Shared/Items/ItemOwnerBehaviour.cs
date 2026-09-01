using System.Collections.Generic;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Items.Pickups;
using Tanks.Shared.Messaging;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Tanks.Shared.Items
{
    public struct ItemPickedUp
    {
        public ItemIdentifier ItemId;
        public PickupId PickupId;
    }

    public interface IItemOwnerBehaviour : INetworkBehaviourExtended
    {
        void PickupItem(IItemConfig item, in PickupId pickupId);
    }

    public sealed class ItemOwnerBehaviour :
        NetworkBehaviourExtended, IItemOwnerBehaviour, IMessageSender
    {
        [SerializeField] private SpriteRenderer _accent;

        [Inject] private readonly IItemDatabase _database;
        [Inject] private readonly IObjectResolver _resolver;
        [Inject] private readonly IMessageBus _bus;

        public SpriteRenderer Accent => _accent;

        private readonly List<ItemIdentifier> _items = new();

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        private void PickupItemClientRpc(ItemIdentifier id, PickupId pickupId)
        {
            PickupItem(_database.Get(id), pickupId);
        }

        public void PickupItem(IItemConfig item, in PickupId pickupId)
        {
            _items.Add(item.Identifier);

            foreach (ItemActionConfigBase action in item.OnPickUpActions)
            {
                action.Create(_resolver).Invoke(this);
            }

            foreach (ItemBehaviourBase behaviour in _database.GetBehaviours(item.Identifier))
            {
                behaviour.OnPickup(this);
            }

            ItemPickedUp message = new ItemPickedUp
            {
                ItemId = item.Identifier,
                PickupId = pickupId
            };

            EmitMessage(message);
            _bus.Publish(message, this);

            if (IsServer) PickupItemClientRpc(item.Identifier, pickupId);
        }
    }
}
