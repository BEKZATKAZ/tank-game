using Tanks.Shared.Generation;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Messaging;
using Unity.Mathematics;

namespace Tanks.Client.Items.Behaviours.OnPickup
{
    [ItemBehaviour("random_teleport")]
    public sealed class RandomTeleportBehaviour :
        ItemBehaviourBase, IMessageHandler<TilemapGenerated>
    {
        public RandomTeleportBehaviour(IMessageBus bus)
        {
            _bus = bus;
            _bus.RegisterHandler<TilemapGenerated>(this);
        }

        private IMessageBus _bus;
        private TilemapGenerated _generated;

        public void Handle(TilemapGenerated message, IMessageSender sender)
        {
            _generated = message;
        }

        public override void OnPickup(IItemOwnerBehaviour owner)
        {
            int wallWidth = _generated.Settings.WallWidth;
            int minDistance = 20;

            float2 initialPosition = owner.Transform.WorldPosition;
            float2 position;

            while (true)
            {
                float2 coords = new float2(
                    UnityEngine.Random.Range(0, _generated.Resolution.x),
                    UnityEngine.Random.Range(0, _generated.Resolution.y));

                position = coords * (wallWidth * 2) + wallWidth * 1.5f;

                // Prevent teleporting too close or in place
                if (math.distance(initialPosition, position) >= minDistance) break;
            }

            owner.Transform.WorldPosition = position;
        }
    }
}
