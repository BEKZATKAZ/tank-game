using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Common;
using Tanks.Core.Lobbies;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Infrastructure.Lobbies;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

namespace Tanks.Infrastructure.Tests.Lobbies
{
    public sealed class LobbyChatServiceTests
    {
        private UGSLobbyService _service;
        private Unity.Services.Lobbies.ILobbyService _sdk;
        private Lobby _lobby;

        [SetUp]
        public void Setup()
        {
            var auth = Substitute.For<IAuthService>();
            auth.CurrentProfile.Returns(new PlayerProfileState());
            auth.CurrentProfile.UserId.Value = "player";
            _sdk = Substitute.For<Unity.Services.Lobbies.ILobbyService>();
            _service = new UGSLobbyService(auth, Substitute.For<ISessionSettingsService>());
            typeof(UGSLobbyService).GetField("_service", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_service, _sdk);

            _lobby = new Lobby(id: "lobby", version: 1, hostId: "player", players: new List<Player>
            {
                new(id: "player", data: new Dictionary<string, PlayerDataObject>
                {
                    ["username"] = new(PlayerDataObject.VisibilityOptions.Member, "Player"),
                    ["ready"] = new(PlayerDataObject.VisibilityOptions.Member, "True"),
                    ["LastMessage"] = new(PlayerDataObject.VisibilityOptions.Member, "")
                })
            }, data: new Dictionary<string, DataObject>());
            typeof(UGSLobbyService).GetField("_lobby", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_service, _lobby);
            UGSLobbyMapper.MapLobby(_lobby, _service.CurrentLobbyState);
        }

        [TearDown]
        public void TearDown()
        {
            _service.Dispose();
        }

        [Test]
        public void NewPlayer_HasEmptyMemberVisibleLastMessage()
        {
            var player = (Player)typeof(UGSLobbyService)
                .GetMethod("CreatePlayerData", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_service, null);

            Assert.That(player.Data["LastMessage"].Value, Is.Empty);
            Assert.That(player.Data["LastMessage"].Visibility, Is.EqualTo(PlayerDataObject.VisibilityOptions.Member));
        }

        [Test]
        public async Task Send_UpdatesOnlyLastMessageAndRefreshesLocalState()
        {
            var payloads = new List<LobbyChatMessage.Payload>();
            _sdk.UpdatePlayerAsync("lobby", "player", Arg.Any<UpdatePlayerOptions>()).Returns(call =>
            {
                var options = call.Arg<UpdatePlayerOptions>();
                Assert.That(options.Data.Count, Is.EqualTo(1));
                Assert.That(options.Data["LastMessage"].Visibility, Is.EqualTo(PlayerDataObject.VisibilityOptions.Member));
                payloads.Add(JsonConvert.DeserializeObject<LobbyChatMessage.Payload>(options.Data["LastMessage"].Value));
                _lobby.Players[0].Data["LastMessage"] = options.Data["LastMessage"];
                return Task.FromResult(_lobby);
            });

            Assert.That((await _service.SendMessageAsync("hello")).IsSuccess, Is.True);
            Assert.That((await _service.SendMessageAsync("hello")).IsSuccess, Is.True);

            Assert.That(payloads[0].Text, Is.EqualTo("hello"));
            Assert.That(payloads[1].Id, Is.Not.EqualTo(payloads[0].Id));
            Assert.That(_service.CurrentLobbyState.Players.Value["player"].IsReady, Is.True);
            Assert.That(_service.CurrentLobbyState.Players.Value["player"].Data["LastMessage"],
                Is.EqualTo(_lobby.Players[0].Data["LastMessage"].Value));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public async Task BlankMessage_DoesNotCallSdk(string message)
        {
            Assert.That((await _service.SendMessageAsync(message)).IsSuccess, Is.False);
            await _sdk.DidNotReceive().UpdatePlayerAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UpdatePlayerOptions>());
        }

        [Test]
        public async Task OversizedMessage_DoesNotCallSdk()
        {
            Assert.That((await _service.SendMessageAsync(new string('a', 501))).IsSuccess, Is.False);
            await _sdk.DidNotReceive().UpdatePlayerAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UpdatePlayerOptions>());
        }

        [Test]
        public async Task MaximumLengthMessage_IsSent()
        {
            _sdk.UpdatePlayerAsync("lobby", "player", Arg.Any<UpdatePlayerOptions>()).Returns(Task.FromResult(_lobby));

            Assert.That((await _service.SendMessageAsync(new string('a', 500))).IsSuccess, Is.True);
            await _sdk.Received(1).UpdatePlayerAsync("lobby", "player", Arg.Any<UpdatePlayerOptions>());
        }

        [Test]
        public async Task NoLobby_DoesNotCallSdk()
        {
            _service.CurrentLobbyState.Id.Value = default;

            Assert.That((await _service.SendMessageAsync("hello")).IsSuccess, Is.False);
            await _sdk.DidNotReceive().UpdatePlayerAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UpdatePlayerOptions>());
        }

        [Test]
        public async Task MissingMember_DoesNotCallSdk()
        {
            _service.CurrentLobbyState.Players.Value = new Dictionary<string, LobbyPlayerState>();

            Assert.That((await _service.SendMessageAsync("hello")).IsSuccess, Is.False);
            await _sdk.DidNotReceive().UpdatePlayerAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UpdatePlayerOptions>());
        }

        [Test]
        public async Task LateResponse_DoesNotRestorePreviousLobby()
        {
            var completion = new TaskCompletionSource<Lobby>();
            _sdk.UpdatePlayerAsync("lobby", "player", Arg.Any<UpdatePlayerOptions>()).Returns(completion.Task);
            Task<ResultType> sending = _service.SendMessageAsync("hello");

            _service.CurrentLobbyState.Id.Value = new LobbyId("other");
            completion.SetResult(_lobby);
            ResultType result = await sending;

            Assert.That(result.ErrorCode, Is.EqualTo("cancelled"));
            Assert.That(_service.CurrentLobbyState.Id.Value.Value, Is.EqualTo("other"));
        }
    }
}
