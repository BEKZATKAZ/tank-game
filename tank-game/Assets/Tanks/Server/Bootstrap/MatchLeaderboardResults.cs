using System.Collections.Generic;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Items;
using Tanks.Shared.Messaging;
using Tanks.Shared.Teams;

namespace Tanks.Server.Bootstrap
{
    public sealed class MatchLeaderboardResults :
        NetworkComponent<IGameStartBehaviour>,
        IMessageHandler<GameStartedFromServer>,
        IMessageHandler<ItemPickedUp>,
        IMessageHandler<EndGameFromServer>
    {
        public MatchLeaderboardResults(IMessageBus bus, ITeamBehaviourService teams)
        {
            _bus = bus;
            _teams = teams;
        }

        private readonly IMessageBus _bus;
        private readonly ITeamBehaviourService _teams;
        private readonly Dictionary<ulong, int> _pickups = new();

        private bool _started;
        private bool _completed;

        protected override void OnInitialize()
        {
            _bus.RegisterHandler<GameStartedFromServer>(this);
            _bus.RegisterHandler<ItemPickedUp>(this);
            _bus.RegisterHandler<EndGameFromServer>(this);
        }

        protected override void OnDestroy()
        {
            _bus.UnregisterHandler<GameStartedFromServer>(this);
            _bus.UnregisterHandler<ItemPickedUp>(this);
            _bus.UnregisterHandler<EndGameFromServer>(this);
        }

        public void Handle(GameStartedFromServer message, IMessageSender sender)
        {
            _started = true;
        }

        public void Handle(ItemPickedUp message, IMessageSender sender)
        {
            if (_started == false || _completed || message.Owner is not { IsServer: true }) return;

            ulong playerId = message.Owner.OwnerClientId;
            if (_teams.TryGetTeamByPlayerId(playerId, out _) == false) return;

            _pickups.TryGetValue(playerId, out int count);
            _pickups[playerId] = count + 1;
        }

        public void Handle(EndGameFromServer message, IMessageSender sender)
        {
            if (_started == false || _completed) return;
            _completed = true;

            foreach (Team team in _teams.GetAllTeams())
            {
                foreach (PlayerState player in team.Players)
                {
                    _pickups.TryGetValue(player.PlayerId, out int count);
                    Behaviour.SendLeaderboardResult(player.PlayerId, new LeaderboardMatchResult
                    {
                        Eliminations = player.Eliminations,
                        PickedUpItems = count
                    });
                }
            }
        }
    }
}
