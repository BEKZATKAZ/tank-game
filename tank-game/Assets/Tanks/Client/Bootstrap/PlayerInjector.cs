using Tanks.Shared;
using Tanks.Shared.Teams;
using Tanks.Client.Player;
using Tanks.Shared.Bootstrap;
using Unity.Netcode;
using VContainer;
using VContainer.Unity;

namespace Tanks.Client.Bootstrap
{
    public sealed class PlayerInjector :
        NetworkComponent<IGameStartBehaviour>, INetworkMessageHandler<GameStartedFromClient>
    {
        public PlayerInjector(IObjectResolver container, ITeamBehaviourService teams)
        {
            _container = container;
            _teams = teams;
        }

        private readonly IObjectResolver _container;
        private readonly ITeamBehaviourService _teams;

        public void Handle(GameStartedFromClient message)
        {
            var playerObjects = NetworkManager.Singleton.SpawnManager.PlayerObjects;

            foreach (NetworkObject playerObject in playerObjects)
            {
                _container.InjectGameObject(playerObject.gameObject);

                if (_teams.TryGetTeamByPlayerId(playerObject.OwnerClientId, out Team team))
                {
                    playerObject.GetComponent<CosmeticApplier>()?.Apply(team.GetPlayer(playerObject.OwnerClientId).Equipped);
                }
            }
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
