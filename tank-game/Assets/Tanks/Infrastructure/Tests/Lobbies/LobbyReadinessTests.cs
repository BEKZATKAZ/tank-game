using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using Tanks.Core;
using Tanks.Core.Lobbies;
using Tanks.Infrastructure.Lobbies;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

namespace Tanks.Infrastructure.Tests.Lobbies
{
    public sealed class LobbyReadinessTests
    {
        private UGSLobbyService _service;
        private Lobby _lobby;

        [SetUp]
        public void Setup()
        {
            _service = new UGSLobbyService(Substitute.For<IAuthService>());
            _lobby = new Lobby(id: "lobby", hostId: "host", players: new List<Player>
            {
                Player("host", "False"), Player("guest", "False")
            }, data: new Dictionary<string, DataObject>());
            typeof(UGSLobbyService).GetField("_lobby", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_service, _lobby);
            UGSLobbyMapper.MapLobby(_lobby, _service.CurrentLobbyState);
        }

        private static Player Player(string id, string ready)
        {
            var data = new Dictionary<string, PlayerDataObject>();
            if (ready != null) data["ready"] = new(PlayerDataObject.VisibilityOptions.Member, ready);
            return new Player(id: id, data: data);
        }

        private void Apply(ILobbyChanges changes)
        {
            typeof(UGSLobbyService).GetMethod("OnLobbyChanged", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_service, new object[] { changes });
        }

        [TestCase(null, false)]
        [TestCase("invalid", false)]
        [TestCase("False", false)]
        [TestCase("True", true)]
        public void PlayerReadiness_MapsSafely(string data, bool expected)
        {
            Assert.That(UGSLobbyMapper.MapPlayer(Player("player", data)).IsReady, Is.EqualTo(expected));
            Assert.That(default(LobbyPlayerState).IsReady, Is.False);
        }

        [Test]
        public void PlayerDataChange_RefreshesReadinessOnBothClients()
        {
            var other = new UGSLobbyService(Substitute.For<IAuthService>());
            UGSLobbyMapper.MapLobby(_lobby, other.CurrentLobbyState);
            var changes = Substitute.For<ILobbyChanges>();
            changes.When(x => x.ApplyToLobby(_lobby)).Do(_ =>
                _lobby.Players[1].Data["ready"] = new(PlayerDataObject.VisibilityOptions.Member, "True"));
            int refreshes = 0;
            _service.CurrentLobbyState.Players.Bind(_ => refreshes++);
            Apply(changes);
            UGSLobbyMapper.MapLobby(_lobby, other.CurrentLobbyState);
            Assert.That(_service.CurrentLobbyState.Players.Value["guest"].IsReady, Is.True);
            Assert.That(other.CurrentLobbyState.Players.Value["guest"].IsReady, Is.True);
            Assert.That(refreshes, Is.EqualTo(2));
        }

        [Test]
        public void AlreadyAppliedSdkSnapshot_StillRefreshesPlayers()
        {
            _lobby.Players[1].Data["ready"] = new(PlayerDataObject.VisibilityOptions.Member, "True");
            var changes = Substitute.For<ILobbyChanges>();
            changes.Version.Returns(new ChangedLobbyValue<int>(_lobby.Version));
            Apply(changes);
            Assert.That(_service.CurrentLobbyState.Players.Value["guest"].IsReady, Is.True);
        }

        [Test]
        public void RemovedReadyData_ResetsReadiness()
        {
            _lobby.Players[1].Data["ready"] = new(PlayerDataObject.VisibilityOptions.Member, "True");
            UGSLobbyMapper.MapLobby(_lobby, _service.CurrentLobbyState);
            var changes = Substitute.For<ILobbyChanges>();
            changes.When(x => x.ApplyToLobby(_lobby)).Do(_ => _lobby.Players[1].Data.Remove("ready"));
            Apply(changes);
            Assert.That(_service.CurrentLobbyState.Players.Value["guest"].IsReady, Is.False);
        }

        [Test]
        public void MembershipChanges_PreservePlayerIdentityAndNotify()
        {
            string joined = null;
            string left = null;
            _service.PlayerJoined += player => joined = player.UserId;
            _service.PlayerLeft += player => left = player.UserId;
            var changes = Substitute.For<ILobbyChanges>();
            changes.When(x => x.ApplyToLobby(_lobby)).Do(_ =>
            {
                _lobby.Players.RemoveAt(0);
                _lobby.Players.Add(Player("new", null));
            });
            Apply(changes);
            Assert.That(left, Is.EqualTo("host"));
            Assert.That(joined, Is.EqualTo("new"));
            Assert.That(_service.CurrentLobbyState.Players.Value["new"].IsReady, Is.False);
        }

        [Test]
        public void PreparingSnapshot_PublishesStatusBeforeReadyPlayers()
        {
            _lobby.Players[0].Data["ready"] = new(PlayerDataObject.VisibilityOptions.Member, "True");
            _lobby.Data["status"] = new(DataObject.VisibilityOptions.Member, "1");
            LobbyStatus observed = default;
            _service.CurrentLobbyState.Players.Bind(_ => observed = _service.CurrentLobbyState.Status.Value);
            UGSLobbyMapper.MapLobby(_lobby, _service.CurrentLobbyState);
            Assert.That(observed, Is.EqualTo(LobbyStatus.PreparingServer));
        }
    }
}
