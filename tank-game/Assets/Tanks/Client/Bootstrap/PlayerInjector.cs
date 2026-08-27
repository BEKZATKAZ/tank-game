using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Unity.Netcode;
using VContainer;
using VContainer.Unity;

namespace Tanks.Client.Bootstrap
{
    public sealed class PlayerInjector :
        NetworkComponent<IGameStartBehaviour>, INetworkMessageHandler<GameStarted>
    {
        public PlayerInjector(IObjectResolver container)
        {
            _container = container;
        }

        private readonly IObjectResolver _container;

        public void Handle(GameStarted message)
        {
            var playerObjects = NetworkManager.Singleton.SpawnManager.PlayerObjects;

            foreach (NetworkObject playerObject in playerObjects)
            {
                _container.InjectGameObject(playerObject.gameObject);
            }
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
