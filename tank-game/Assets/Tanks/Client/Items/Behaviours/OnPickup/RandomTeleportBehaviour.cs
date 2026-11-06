using System;
using Tanks.Shared.Generation;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Messaging;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Client.Items.Behaviours.OnPickup
{
    [ItemBehaviour("random-teleport")]
    public sealed class RandomTeleportBehaviour :
        ItemBehaviourBase, IMessageHandler<TilemapGenerated>, IDisposable
    {
        public RandomTeleportBehaviour(IMessageBus bus)
        {
            _bus = bus;
            _bus.RegisterHandler<TilemapGenerated>(this);
        }

        private readonly IMessageBus _bus;
        private TilemapGenerated _generated;

        public void Dispose()
        {
            _bus.UnregisterHandler<TilemapGenerated>(this);
        }

        public void Handle(TilemapGenerated message, IMessageSender sender)
        {
            _generated = message;
        }

        public override void OnPickup(IItemOwnerBehaviour owner)
        {
            int wallWidth = _generated.Settings.WallWidth;
            int minDistance = wallWidth * 4;
            int attempts = 0;

            float2 initialPosition = owner.Transform.WorldPosition;
            float2 position = owner.Transform.WorldPosition;

            while (attempts++ < 100)
            {
                float2 coords = new float2(
                    UnityEngine.Random.Range(0, _generated.Resolution.x),
                    UnityEngine.Random.Range(0, _generated.Resolution.y));

                position = coords * (wallWidth * 2) + wallWidth * 1.5f;

                // Prevent teleporting too close or in place
                if (math.distance(initialPosition, position) < minDistance) continue;

                float angle = owner.Transform.WorldRotation.Degrees;
                if (Physics2D.OverlapBox(position, owner.Entity.Size * 1.5f, angle)) continue;

                break;
            }

            owner.Transform.WorldPosition = position;
        }
    }
}
