using Tanks.Shared;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Pickups;
using Tanks.Shared.Messaging;
using Unity.Mathematics;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Items.Pickups
{
    public struct PickupAreaEntered
    {
        public ItemIdentifier ItemId;
        public PickupId PickupId;
    }

    public struct PickupAreaLeft
    {
        public PickupId PickupId;
    }

    public sealed class PickupArea : NetworkComponent<PickupBehaviour>, IMessageSender
    {
        [Inject] private readonly IMessageBus _bus;

        private readonly Collider2D[] _colliderBuffer = new Collider2D[8];
        private bool _hasEntered;

        private void TryEnter()
        {
            if (_hasEntered) return;

            _bus.Publish(new PickupAreaEntered
            {
                ItemId = Behaviour.ItemId,
                PickupId = Behaviour.PickupId
            }, this);

            _hasEntered = true;
        }

        private void TryLeave()
        {
            if (_hasEntered == false) return;
            _hasEntered = false;
            _bus.Publish(new PickupAreaLeft { PickupId = Behaviour.PickupId }, this);
        }

        private void OnDisable()
        {
            TryLeave();
        }

        public override void OnTick(int tick, float tickDelta)
        {
            var localPlayer = NetworkManager.LocalClient.PlayerObject;

            if (localPlayer == false)
            {
                TryLeave();
                return;
            }

            ContactFilter2D filter = new ContactFilter2D { layerMask = 1 << 3 };
            float2 origin = Behaviour.Transform.WorldPosition;
            float radius = 6;

            int count = Physics2D.OverlapCircle(origin, radius, filter, _colliderBuffer);

            for (int i = 0; i < count; i++)
            {
                Collider2D other = _colliderBuffer[i];
                if (localPlayer.gameObject != other.gameObject) continue;

                TryEnter();
                return;
            }

            TryLeave();
        }
    }
}