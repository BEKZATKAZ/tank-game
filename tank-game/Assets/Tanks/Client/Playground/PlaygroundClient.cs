using System;
using Tanks.Audio;
using Tanks.Client.Rendering;
using Tanks.Core.Common.Scenes;
using Tanks.Shared;
using Tanks.Shared.Adapters;
using Tanks.Shared.Entities;
using Tanks.Shared.Generation;
using Tanks.Shared.Messaging;
using Tanks.Shared.Playground;
using Unity.Netcode;
using VContainer;
using VContainer.Unity;

namespace Tanks.Client.Playground
{
    public sealed class PlaygroundClient :
        NetworkComponent<IPlaygroundStartBehaviour>,
        INetworkMessageHandler<PlaygroundStartedFromServer>,
        IMessageHandler<TilemapGenerated>
    {
        public PlaygroundClient(
            ISceneLoader sceneLoader,
            ICameraFollowService cameraFollow,
            IObjectResolver container,
            IAudioPlayer audioPlayer,
            IMessageBus bus)
        {
            _sceneLoader = sceneLoader;
            _cameraFollow = cameraFollow;
            _container = container;
            _audioPlayer = audioPlayer;
            _bus = bus;
        }

        private readonly ISceneLoader _sceneLoader;
        private readonly ICameraFollowService _cameraFollow;
        private readonly IAudioPlayer _audioPlayer;
        private readonly IObjectResolver _container;
        private readonly IMessageBus _bus;

        public void Handle(TilemapGenerated message, IMessageSender sender)
        {
            _cameraFollow.SetBounds(message.Bounds);
        }

        public void Handle(PlaygroundStartedFromServer message)
        {
            _sceneLoader.EndLoading<LoadPlayground>();

            NetworkManager manager = NetworkManager.Singleton;
            if (manager == false) return;

            NetworkObject playerObject = manager.SpawnManager.PlayerObjects[0];
            _container.InjectGameObject(playerObject.gameObject);

            ITransform player = playerObject.GetComponent<IEntityBehaviour>().Transform;
            _audioPlayer.SetDefault(player);
            _cameraFollow.SetFollowTarget(player, instant: true);
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
            _bus.RegisterHandler<TilemapGenerated>(this);
        }

        protected override void OnDestroy()
        {
            _bus.UnregisterHandler<TilemapGenerated>(this);
        }
    }
}
