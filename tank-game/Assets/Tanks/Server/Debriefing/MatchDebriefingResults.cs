using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tanks.Core.Debriefing;
using Tanks.Server.Bootstrap;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Messaging;
using Tanks.Shared.Teams;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Server.Debriefing
{
    public sealed class MatchDebriefingResults :
        NetworkComponent<IGameStartBehaviour>,
        IMessageHandler<GameStartedFromServer>,
        IMessageHandler<EndGameFromServer>,
        INetworkMessageHandler<DebriefingAcknowledged>
    {
        public MatchDebriefingResults(
            IMessageBus bus,
            ITeamBehaviourService teams,
            IConnectedClientService clients,
            IMatchDebriefingSubmissionService submission,
            IDebriefingClockService clock,
            IServerLifetimeService lifetime,
            MatchDebriefingSettings settings)
        {
            _bus = bus;
            _teams = teams;
            _clients = clients;
            _submission = submission;
            _clock = clock;
            _lifetime = lifetime;
            _settings = settings;
        }

        private readonly IMessageBus _bus;
        private readonly ITeamBehaviourService _teams;
        private readonly IConnectedClientService _clients;
        private readonly IMatchDebriefingSubmissionService _submission;
        private readonly IDebriefingClockService _clock;
        private readonly IServerLifetimeService _lifetime;
        private readonly MatchDebriefingSettings _settings;
        private readonly List<MatchDebriefingParticipant> _roster = new();
        private readonly HashSet<ulong> _awaiting = new();

        private bool _started;
        private string _matchId;

        protected override void OnInitialize()
        {
            _bus.RegisterHandler<GameStartedFromServer>(this);
            _bus.RegisterHandler<EndGameFromServer>(this);
            Behaviour.RegisterMessageHandler(this);
        }

        protected override void OnDestroy()
        {
            _bus.UnregisterHandler<GameStartedFromServer>(this);
            _bus.UnregisterHandler<EndGameFromServer>(this);
        }

        public void Handle(GameStartedFromServer message, IMessageSender sender)
        {
            if (_started) return;
            _started = true;

            foreach (Team team in _teams.GetAllTeams())
            {
                foreach (PlayerState player in team.Players)
                {
                    ClientData client = _clients.GetClientData(player.PlayerId);

                    if (client.IsDirectSceneTest) continue;

                    _roster.Add(new MatchDebriefingParticipant
                    {
                        PlayerId = client.PlayerId,
                        Player = player,
                        Team = team
                    });
                }
            }
        }

        public void Handle(EndGameFromServer message, IMessageSender sender)
        {
            if (_started == false || _matchId != null || _roster.Count == 0) return;

            _matchId = message.MatchId;

            PlayerDebriefingPayload[] players = _roster
                .Select(entry => CreatePayload(entry, message.WinningTeamId))
                .ToArray();

            _lifetime.HoldShutdown(FinishAsync(players));
        }

        public void Handle(DebriefingAcknowledged message)
        {
            if (message.MatchId == _matchId) _awaiting.Remove(message.ClientId);
        }

        private async Task FinishAsync(PlayerDebriefingPayload[] players)
        {
            try
            {
                DebriefingPlayerResult[] results = await _submission.SubmitAsync(
                    _matchId, players, _settings.SubmissionTimeoutSeconds);

                DeliverResults(results);
                await WaitForAcknowledgmentsAsync();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void DeliverResults(DebriefingPlayerResult[] results)
        {
            HashSet<ulong> connected = _clients.GetConnectedClientIds().ToHashSet();

            foreach (DebriefingPlayerResult result in results)
            {
                MatchDebriefingParticipant entry = _roster.First(
                    value => value.PlayerId == result.PlayerId);

                ulong clientId = entry.Player.PlayerId;

                if (connected.Contains(clientId) == false) continue;

                _awaiting.Add(clientId);

                Behaviour.SendDebriefing(
                    clientId, _matchId, JsonConvert.SerializeObject(result));
            }
        }

        private async Task WaitForAcknowledgmentsAsync()
        {
            float timeout = _settings.AcknowledgmentTimeoutSeconds;
            float deadline = _clock.Seconds + timeout;

            while (_awaiting.Count > 0 && _clock.Seconds < deadline)
            {
                _awaiting.IntersectWith(_clients.GetConnectedClientIds());

                if (_awaiting.Count == 0) break;

                await _clock.DelayAsync(math.min(0.05f, deadline - _clock.Seconds));
            }
        }

        private static PlayerDebriefingPayload CreatePayload(
            MatchDebriefingParticipant entry,
            TeamId winningTeam)
        {
            return new PlayerDebriefingPayload
            {
                PlayerId = entry.PlayerId,
                Eliminations = entry.Player.Eliminations,
                Assists = entry.Player.Assistance,
                Deaths = entry.Player.Deaths,
                Score = entry.Team.Score.Value,
                IsVictory = entry.Team.Id == winningTeam
            };
        }
    }
}
