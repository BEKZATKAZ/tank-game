using Tanks.Shared.Generation;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Messaging;
using Unity.Mathematics;
using VContainer;

namespace Tanks.Client.Items.Behaviours.OnPickup
{
    [ItemBehaviour("random_teleport")]
    public sealed class RandomTeleportBehaviour :
        ItemBehaviourBase,
        IMessageHandler<TilemapGenerated>
    {
        private IMessageBus _bus;
        private TilemapGenerated _generated;

        [Inject]
        public void Initialize(IMessageBus bus)
        {
            _bus = bus;
            _bus.RegisterHandler<TilemapGenerated>(this);
        }

        public void Handle(TilemapGenerated message, IMessageSender sender)
        {
            _generated = message;
        }

        public override void OnPickup(IItemOwner owner)
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
