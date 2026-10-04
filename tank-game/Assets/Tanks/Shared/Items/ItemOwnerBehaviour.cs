using System.Collections.Generic;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Items.Pickups;
using Tanks.Shared.Messaging;
using Tanks.Shared.Weapons;
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
        IEnumerable<ItemIdentifier> Items { get; }
        IEntityBehaviour Entity { get; }

        void PickupItem(IItemConfig item, in PickupId pickupId);
    }

    [RequireComponent(typeof(EntityBehaviour))]
    public sealed class ItemOwnerBehaviour :
        NetworkBehaviourExtended,
        IItemOwnerBehaviour,
        IMessageHandler<HealthChangedFromServer>,
        IMessageHandler<WeaponUsed>,
        IMessageSender
    {
        [SerializeField] private SpriteRenderer _accent;

        [Inject] private readonly IItemDatabase _database;
        [Inject] private readonly IObjectResolver _resolver;
        [Inject] private readonly IMessageBus _bus;

        public IEntityBehaviour Entity { get; private set; }
        public IEnumerable<ItemIdentifier> Items => _items;
        public SpriteRenderer Accent => _accent;

        private readonly List<ItemIdentifier> _items = new();

        public void Handle(HealthChangedFromServer message, IMessageSender sender)
        {
            if (message.Target != Entity) return;

            foreach (ItemIdentifier item in _items)
            {
                foreach (ItemBehaviourBase behaviour in _database.GetBehaviours(item))
                {
                    behaviour.OnHealthChanged(
                        this, message.From, message.OldState, message.AuthoritativeState);
                }
            }
        }

        public void Handle(WeaponUsed message, IMessageSender sender)
        {
            if (message.Owner.Entity != Entity) return;

            foreach (ItemIdentifier item in _items)
            {
                foreach (ItemBehaviourBase behaviour in _database.GetBehaviours(item))
                {
                    behaviour.OnFire(this, message);
                }
            }
        }

        public void PickupItem(IItemConfig item, in PickupId pickupId)
        {
            _items.Add(item.Identifier);

            ItemPickedUp message = new ItemPickedUp
            {
                ItemId = item.Identifier,
                PickupId = pickupId
            };

            EmitMessage(message);
            _bus.Publish(message, this);

            if (IsServer) PickupItemClientRpc(item.Identifier, pickupId);

            foreach (ItemActionConfigBase action in item.OnPickUpActions)
            {
                action.Create(_resolver).Invoke(this);
            }

            foreach (ItemBehaviourBase behaviour in _database.GetBehaviours(item.Identifier))
            {
                behaviour.OnPickup(this);
            }
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        private void PickupItemClientRpc(ItemIdentifier id, PickupId pickupId)
        {
            PickupItem(_database.Get(id), pickupId);
        }

        protected override void OnInitialize()
        {
            Entity = GetComponent<EntityBehaviour>();
            _bus.RegisterHandler<HealthChangedFromServer>(this);
            _bus.RegisterHandler<WeaponUsed>(this);
        }

        public override void OnDestroy()
        {
            _bus?.UnregisterHandler<HealthChangedFromServer>(this);
            _bus?.UnregisterHandler<WeaponUsed>(this);
        }
    }
}
