using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using Tanks.Core;
using Tanks.Core.Players;
using Tanks.Core.Common;
using Tanks.Core.Lobbies;
using Tanks.UI.Menus.LobbyRoom;
using Tanks.UI.Menus.TankModel;
using Tanks.UI.TankModel;
using UnityEngine;

namespace Tanks.UI.Tests.Menus.LobbyRoom
{
    public sealed class LobbyTankSpawnerTests
    {
        private LobbyTankSpawner _spawner;
        private ITankModelService _models;
        private PlayerProfileState _profile;
        private LobbyState _lobby;
        private LocalPlayer _playerData;

        [SetUp]
        public void Setup()
        {
            _models = Substitute.For<ITankModelService>();
            _models.Models.Returns(new TankModelEntry[3]);
            _profile = new PlayerProfileState();
            _profile.UserId.Value = "local";
            _profile.DisplayName.Value = "Local";
            _lobby = new LobbyState();

            IAuthService auth = Substitute.For<IAuthService>();
            auth.CurrentProfile.Returns(_profile);
            ILobbyService service = Substitute.For<ILobbyService>();
            service.CurrentLobbyState.Returns(_lobby);

            _spawner = new GameObject("Lobby Tank Spawner").AddComponent<LobbyTankSpawner>();
            SetField("_tankModelService", _models);
            SetField("_auth", auth);
            SetField("_lobbyService", service);
            var player = Substitute.For<ILocalPlayerService>();
            _playerData = new LocalPlayer();
            player.LocalPlayer.Returns(_playerData);
            SetField("_player", player);
            typeof(LobbyTankSpawner).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_spawner, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (_spawner) Object.DestroyImmediate(_spawner.gameObject);
        }

        [Test]
        public void ProfileName_IsShownOutsideLobbyAndRefreshes()
        {
            _models.Received().SetPlayerData(0, Arg.Is<PlayerDisplayData>(data => data.DisplayName == "Local"));

            _profile.DisplayName.Value = "Renamed";
            _models.Received().SetPlayerData(0, Arg.Is<PlayerDisplayData>(data => data.DisplayName == "Renamed"));
        }

        [Test]
        public void LobbyNames_IncludeLocalAndRefreshWithoutRespawning()
        {
            SetPlayers(Player("local", "Lobby Local"), Player("remote", "Remote"));

            _models.Received().SetPlayerData(0, Arg.Is<PlayerDisplayData>(data => data.DisplayName == "Lobby Local"));
            _models.Received().SetPlayerData(1, Arg.Is<PlayerDisplayData>(data => data.DisplayName == "Remote"));
            _models.Received(1).DriveByCurve(1, 1, true);

            SetPlayers(Player("local", "Lobby Local"), Player("remote", "Renamed"));

            _models.Received().SetPlayerData(1, Arg.Is<PlayerDisplayData>(data => data.DisplayName == "Renamed"));
            _models.ReceivedWithAnyArgs(1).DriveByCurve(default, default, default);
        }

        [Test]
        public void ReadinessChanges_UpdateLocalAndRemoteModelsWithoutRespawning()
        {
            var local = Player("local", "Local");
            var remote = Player("remote", "Remote");
            local.Data["ready"] = "True";
            SetPlayers(local, remote);

            _models.Received().SetReady(0, true);
            _models.Received().SetReady(1, false);

            remote.Data["ready"] = "True";
            SetPlayers(local, remote);

            _models.Received().SetReady(1, true);
            _models.ReceivedWithAnyArgs(1).DriveByCurve(default, default, default);

            local.Data["ready"] = "False";
            SetPlayers(local, remote);
            _models.Received().SetReady(0, false);
        }

        [Test]
        public void ReadyPlayerLeaves_ReplacementSlotUsesNewPlayersReadiness()
        {
            var ready = Player("first", "First");
            ready.Data["ready"] = "True";
            SetPlayers(Player("local", "Local"), ready);
            _models.Received().SetReady(1, true);

            SetPlayers(Player("local", "Local"));
            _models.ClearReceivedCalls();
            SetPlayers(Player("local", "Local"), Player("second", "Second"));

            _models.Received().SetReady(1, false);
            _models.DidNotReceive().SetReady(1, true);
        }

        [Test]
        public void DepartedPlayer_ReturnsTankAndReplacementReusesSlot()
        {
            SetPlayers(Player("local", "Local"), Player("first", "First"), Player("second", "Second"));
            SetPlayers(Player("local", "Local"), Player("second", "Second"));

            _models.Received().Return(1);

            SetPlayers(Player("local", "Local"), Player("second", "Second"), Player("third", "Third"));

            _models.Received().DriveByCurve(1, 1, false);
            _models.Received().SetPlayerData(1, Arg.Is<PlayerDisplayData>(data => data.DisplayName == "Third"));
        }

        [Test]
        public void LobbyReentry_ClearsOldMappingsAndRestoresProfileName()
        {
            SetPlayers(Player("local", "Lobby Local"), Player("first", "First"), Player("second", "Second"));
            SetPlayers();

            _models.Received().Return(0);
            _models.Received().Return(1);
            _models.Received().Return(2);
            _models.Received().SetPlayerData(0, Arg.Is<PlayerDisplayData>(data => data.DisplayName == "Local"));

            _models.ClearReceivedCalls();
            SetPlayers(Player("local", "Local"), Player("second", "Second"));

            _models.Received().DriveByCurve(1, 1, true);
            _models.Received().SetPlayerData(1, Arg.Is<PlayerDisplayData>(data => data.DisplayName == "Second"));
            SetPlayers();

            _models.Received().Return(1);
            _models.DidNotReceive().Return(2);
        }

        [Test]
        public void OnDestroy_UnsubscribesFromProfileAndLobby()
        {
            typeof(LobbyTankSpawner).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_spawner, null);
            _models.ClearReceivedCalls();

            _profile.DisplayName.Value = "After";
            SetPlayers(Player("local", "After"));

            _models.DidNotReceiveWithAnyArgs().SetPlayerData(default, default);
        }

        [Test]
        public void PlayerData_RefreshesLocalLevelAndCosmetics()
        {
            _playerData.Apply(new PlayerSnapshot
            {
                Level = new PlayerLevel { Value = 7 },
                Equipped = new EquippedCosmetics { Body = "v4-body", Turret = "kr1-turret" }
            });

            _models.Received().SetPlayerData(0, Arg.Is<PlayerDisplayData>(data =>
                data.DisplayName == "Local" && data.Level == 7 &&
                data.Equipped.Body == "v4-body" && data.Equipped.Turret == "kr1-turret"));
        }

        private void SetField(string name, object value)
        {
            typeof(LobbyTankSpawner).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_spawner, value);
        }

        private void SetPlayers(params LobbyPlayerState[] players)
        {
            Dictionary<string, LobbyPlayerState> states = new();
            foreach (LobbyPlayerState player in players) states.Add(player.UserId, player);
            _lobby.Players.Value = states;
        }

        private static LobbyPlayerState Player(string userId, string displayName)
        {
            return new LobbyPlayerState
            {
                UserId = userId,
                Data = new Dictionary<string, string> { ["username"] = displayName }
            };
        }
    }
}
