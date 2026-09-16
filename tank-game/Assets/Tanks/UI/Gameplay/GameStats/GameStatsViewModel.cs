using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Localization;
using Tanks.Shared.Teams;
using Tanks.Utils;

namespace Tanks.UI.Gameplay.GameStats
{
    public sealed class GameStatsViewModel : IViewModel, IDisposable
    {
        public GameStatsViewModel(
            ISessionSettingsService settings,
            ITeamBehaviourService teamService,
            ILocalizationDatabase localization)
        {
            _settings = settings;
            _teamService = teamService;
            _localization = localization;

            _showingTeams = _teamService.GetAllTeams().ToArray();
            _teamService.CurrentRound.Bind(OnRoundChanged);

            Players.Value = _showingTeams.SelectMany(x => x.Players).ToList();

            foreach (Team team in _showingTeams)
            {
                team.Score.Bind(OnTeamScoreChanged);
                team.PlayerJoined += OnPlayerJoined;
                team.PlayerLeft += OnPlayerLeft;
            }
        }

        public BindableProperty<string> Header { get; } = new();
        public BindableProperty<List<PlayerState>> Players { get; } = new();

        private readonly ISessionSettingsService _settings;
        private readonly ITeamBehaviourService _teamService;
        private readonly ILocalizationDatabase _localization;
        private readonly Team[] _showingTeams;

        public void Dispose()
        {
            foreach (Team team in _showingTeams)
            {
                team.Score.Unbind(OnTeamScoreChanged);
                team.PlayerJoined -= OnPlayerJoined;
                team.PlayerLeft -= OnPlayerLeft;
            }

            _teamService.CurrentRound.Unbind(OnRoundChanged);
        }

        private void OnTeamScoreChanged(int score)
        {
            Players.Value = Players.Value;

            if (score < _settings.GetSettings().MaxScore) return;

            Team team = _showingTeams.FirstOrDefault(x => x.Players.Any(x => x.IsLocalPlayer));
            bool victory = team?.Score.Value == score;
            string entryName = victory ? "gameplay.victory" : "gameplay.defeat";

            if (_localization.TryGet("Gameplay", entryName, out var header) == false) return;
            Header.Value = header.Get();
        }

        private void OnPlayerJoined(PlayerState player)
        {
            Players.Value.Add(player);
            Players.Value = Players.Value;
        }

        private void OnPlayerLeft(PlayerState player)
        {
            if (Players.Value.Remove(player) == false) return;
            Players.Value = Players.Value;
        }

        private void OnRoundChanged(int round)
        {
            if (_localization.TryGet("Gameplay", "gameplay.game-stats", out var header) == false)
            {
                return;
            }

            header.AddVariable("round", round);
            Header.Value = header.Get();
        }
    }
}
