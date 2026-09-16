using System.Linq;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using Tanks.Shared.Teams;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Server.Bootstrap
{
    public sealed class GameRestarter :
        NetworkComponent<IGameStartBehaviour>, IMessageHandler<Died>
    {
        public GameRestarter(
            IMessageBus bus,
            IProjectilePoolService pool,
            ITeamBehaviourService teamService,
            ISessionSettingsService settings,
            IServerLifetimeService lifetime)
        {
            _bus = bus;
            _pool = pool;
            _teamService = teamService;
            _settings = settings;
            _lifetime = lifetime;
        }

        private readonly IMessageBus _bus;
        private readonly IProjectilePoolService _pool;
        private readonly ITeamBehaviourService _teamService;
        private readonly ISessionSettingsService _settings;
        private readonly IServerLifetimeService _lifetime;

        private int _currentTick;
        private int _lastDiedTick = -1;
        private bool _isEndingGame;

        public void Handle(Died message, IMessageSender sender)
        {
            if (message.Target.OwnerClientId == 0 || _isEndingGame) return;
            _lastDiedTick = _currentTick;
        }

        protected override void OnInitialize()
        {
            _bus.RegisterHandler<Died>(this);
        }

        protected override void OnDestroy()
        {
            _bus.UnregisterHandler<Died>(this);
        }

        protected override void OnTick(int tick, float tickDelta)
        {
            _currentTick = tick;
            if (_lastDiedTick == -1 || _isEndingGame) return;

            float delaySeconds = 2.5f;
            int delayTicks = (int)math.ceil(delaySeconds / tickDelta);

            if (tick - _lastDiedTick < delayTicks || _isEndingGame) return;
            _lastDiedTick = -1;

            Debug.Log("Total teams: " + _teamService.GetAllTeams().Count());
            Debug.Log("Total players: " + _teamService.GetAllTeams().SelectMany(x => x.Players).Count());

            int aliveTeamCount = _teamService
                .GetAllTeams()
                .Count(x => x.Players.Any(x => x.IsEliminated == false));

            // Restart when only one or zero teams left
            if (aliveTeamCount > 1) return;

            Debug.Log(aliveTeamCount);

            if (aliveTeamCount == 1)
            {
                Team winningTeam = _teamService
                    .GetAllTeams()
                    .Single(x => x.Players.Any(x => x.IsEliminated == false));

                _teamService.AddTeamScoreEveryoneRpc(winningTeam.Id, 1);

                if (winningTeam.Score.Value >= _settings.GetSettings().MaxScore)
                {
                    _isEndingGame = true;
                    _lifetime.TrySetStep(GameStep.End);

                    Behaviour.EndGameEveryoneRpc(winningTeam.Id);
                    return;
                }
            }

            _pool.ReturnAll();
            Behaviour.PrepareGame();
        }
    }
}
