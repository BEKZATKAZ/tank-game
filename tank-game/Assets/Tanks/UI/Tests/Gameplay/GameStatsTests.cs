using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Localization;
using Tanks.Shared.Teams;
using Tanks.UI.Gameplay.GameStats;
using Tanks.Utils;

namespace Tanks.UI.Tests.Gameplay
{
    public class GameStatsTests : ViewModelTestBase<GameStatsViewModel>
    {
        private ISessionSettingsService _settings;
        private ITeamBehaviourService _teamService;
        private ILocalizationDatabase _localization;
        private BindableProperty<int> _roundProperty;
        private Team _team;

        protected override GameStatsViewModel CreateViewModel()
        {
            _roundProperty = new BindableProperty<int>();
            _team = new Team(new TeamId(101));

            _teamService = Substitute.For<ITeamBehaviourService>();
            _teamService.CurrentRound.Returns(_roundProperty);

            _teamService.GetTeam(_team.Id).Returns(_team);
            _teamService.GetAllTeams().Returns(new Team[] { _team });

            _settings = Substitute.For<ISessionSettingsService>();
            _settings.GetSettings().MaxScore.Returns(30);

            _localization = Substitute.For<ILocalizationDatabase>();
            return new GameStatsViewModel(_settings, _teamService, _localization);
        }

        [Test]
        public void VictoryOrDefeat_ChangesHeader()
        {
            MockLocalizationEntry("Gameplay", "gameplay.victory", "VICTORY");
            MockLocalizationEntry("Gameplay", "gameplay.defeat", "DEFEAT");

            _team.AddPlayer(1, "self").SetLocalPlayer(true);
            _team.AddPlayer(2, "other");
            _team.AddScore(30);

            Assert.AreEqual("VICTORY", ViewModel.Header.Value);

            _team.GetPlayer(1).SetLocalPlayer(false);
            _team.AddScore(1);

            Assert.AreEqual("DEFEAT", ViewModel.Header.Value);
        }

        [Test]
        public void OnPlayerJoined_RefreshesPlayerList()
        {
            List<PlayerState> players = ViewModel.Players.Value;
            Assert.AreEqual(0, players.Count, "Player list changed too early");

            _team.AddPlayer(5, "player");
            Assert.AreEqual(1, players.Count, "Player list should change");

            Assert.AreEqual(5, players[0].PlayerId, "Player ID dismatch");
            Assert.AreEqual("player", players[0].DisplayName, "Display name dismatch");
        }

        [Test]
        public void OnPlayerLeft_RefreshesPlayerList()
        {
            _team.AddPlayer(5, "player1");
            _team.AddPlayer(6, "player2");
            _team.AddPlayer(7, "player3");

            _team.RemovePlayer(5);
            _team.RemovePlayer(10); // Invalid player, does nothing

            List<PlayerState> players = ViewModel.Players.Value;
            Assert.AreEqual(2, players.Count, "Player list should change");
            Assert.AreEqual(6, players[0].PlayerId, "Player ID dismatch");
            Assert.AreEqual(7, players[1].PlayerId, "Player ID dismatch");
        }

        [Test]
        public void NewRound_ValidLocalization_ChangesHeader()
        {
            MockLocalizationEntry("Gameplay", "gameplay.game-stats", "Test");
            _roundProperty.Value = 10;
            Assert.AreEqual("Test", ViewModel.Header.Value);
        }

        [Test]
        public void NewRound_InvalidLocalization_DoesNothing()
        {
            string previousValue = ViewModel.Header.Value;
            _roundProperty.Value = 10;
            Assert.AreEqual(previousValue, ViewModel.Header.Value);
        }

        private void MockLocalizationEntry(string tableName, string entryName, string text)
        {
            _localization
                .TryGet(tableName, entryName, out Arg.Any<ILocalizedString>())
                .Returns(args =>
                {
                    ILocalizedString result = Substitute.For<ILocalizedString>();
                    result.Get().Returns(text);
                    args[2] = result;
                    return true;
                });
        }
    }
}
