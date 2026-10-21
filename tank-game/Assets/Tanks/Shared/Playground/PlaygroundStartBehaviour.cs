using NaughtyAttributes;
using Tanks.Shared.Generation;
using Tanks.Shared.Messaging;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Tanks.Shared.Playground
{
    public struct PlaygroundStartedFromServer { }

    public sealed class PlaygroundStartBehaviour :
        NetworkBehaviourExtended, IPlaygroundStartBehaviour, IMessageSender
    {
        [Required]
        [SerializeField]
        private NetworkObject _playerPrefab;

        [Required]
        [SerializeField]
        private Transform _spawnPoint;

        [Inject] private readonly IObjectResolver _container;
        [Inject] private readonly IMessageBus _bus;

        protected override void OnInitialize()
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (NetworkManager) NetworkManager.OnClientConnectedCallback -= OnClientConnected;
        }

        public void OnClientConnected(ulong clientId)
        {
            if (IsServer == false) return;
            if (clientId == 0) return; // Discard host

            NetworkObject player = Instantiate(
                _playerPrefab, _spawnPoint.position, Quaternion.identity);

            _container.InjectGameObject(player.gameObject);

            player.SpawnAsPlayerObject(clientId, destroyWithScene: true);

            StartPlaygroundEveryoneRpc();
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        public void StartPlaygroundEveryoneRpc()
        {
            _bus.Publish(new TilemapGenerated
            {
                Bounds = new Bounds(new Vector2(125, 34), new Vector2(250, 68)),
                Settings = new TilemapGenerationSettings
                {
                    Seed = 1,
                    WallWidth = 6
                }
            }, this);

            EmitMessage(new PlaygroundStartedFromServer());
            _bus.Publish(new PlaygroundStartedFromServer(), this);
        }
    }
}
