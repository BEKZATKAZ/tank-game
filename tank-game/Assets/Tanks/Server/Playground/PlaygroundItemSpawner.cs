using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Items.Pickups;
using Tanks.Shared.Messaging;
using Tanks.Shared.Playground;
using Unity.Mathematics;
using UnityEngine;
using VContainer;

namespace Tanks.Server.Playground
{
    public sealed class PlaygroundItemSpawner :
        MonoBehaviour, IMessageHandler<PlaygroundStartedFromServer>
    {
        [SerializeField] private string _targetItemId;

        [Inject] private readonly IPickupSpawnBehaviour _spawner;
        [Inject] private readonly IMessageBus _bus;

        public void Handle(PlaygroundStartedFromServer message, IMessageSender sender)
        {
            _spawner.SpawnEveryoneRpc(new PickupSpawned
            {
                ItemId = new ItemIdentifier(_targetItemId),
                WorldPosition = ((float3)transform.position).xy
            });
        }

        private void Start()
        {
            _bus.RegisterHandler<PlaygroundStartedFromServer>(this);
        }

        private void OnDestroy()
        {
            _bus?.UnregisterHandler<PlaygroundStartedFromServer>(this);
        }
    }
}
