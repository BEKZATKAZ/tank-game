using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Common;
using Tanks.Core.Lobbies;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Core.Players;
using Tanks.Infrastructure.Lobbies;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

namespace Tanks.Infrastructure.Tests.Lobbies
{
    public sealed class LobbyAppearanceTests
    {
        [Test]
        public async Task PlayerSnapshot_PublishesMemberVisibleAppearance()
        {
            var auth = Substitute.For<IAuthService>();
            auth.CurrentProfile.Returns(new PlayerProfileState());
            auth.CurrentProfile.UserId.Value = "local";
            var player = Substitute.For<ILocalPlayerService>();
            var data = new LocalPlayer();
            player.LocalPlayer.Returns(data);
            using var service = new UGSLobbyService(auth, Substitute.For<ISessionSettingsService>(), player);
            var sdk = Substitute.For<Unity.Services.Lobbies.ILobbyService>();
            typeof(UGSLobbyService).GetField("_service", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(service, sdk);
            var lobby = new Lobby(id: "lobby", version: 1, hostId: "local",
                players: new List<Player> { new(id: "local") }, data: new Dictionary<string, DataObject>());
            UGSLobbyMapper.MapLobby(lobby, service.CurrentLobbyState);
            sdk.UpdatePlayerAsync("lobby", "local", Arg.Any<UpdatePlayerOptions>()).Returns(lobby);

            data.Apply(new PlayerSnapshot
            {
                Level = new PlayerLevel { Value = 7 },
                Equipped = new EquippedCosmetics { Body = "v4-body", Turret = "kr1-turret" }
            });

            await sdk.Received(1).UpdatePlayerAsync("lobby", "local", Arg.Is<UpdatePlayerOptions>(options =>
                options.Data.Count == 3 && options.Data["level"].Value == "7" &&
                options.Data["body"].Value == "v4-body" && options.Data["turret"].Value == "kr1-turret" &&
                options.Data["body"].Visibility == PlayerDataObject.VisibilityOptions.Member));
        }

        [Test]
        public void RemoteAppearance_MapsAndFallsBackForLegacyPlayers()
        {
            var sdkPlayer = new Player(id: "remote", data: new Dictionary<string, PlayerDataObject>
            {
                ["username"] = new(PlayerDataObject.VisibilityOptions.Member, "Remote"),
                ["level"] = new(PlayerDataObject.VisibilityOptions.Member, "7"),
                ["body"] = new(PlayerDataObject.VisibilityOptions.Member, "v4-body"),
                ["turret"] = new(PlayerDataObject.VisibilityOptions.Member, "kr1-turret")
            });
            PlayerDisplayData data = UGSLobbyMapper.MapPlayer(sdkPlayer).DisplayData;

            Assert.That(data.Level, Is.EqualTo(7));
            Assert.That(data.DisplayName, Is.EqualTo("Remote"));
            Assert.That(data.Equipped.Body, Is.EqualTo("v4-body"));
            Assert.That(data.Equipped.Turret, Is.EqualTo("kr1-turret"));

            data = new LobbyPlayerState { UserId = "legacy" }.DisplayData;
            Assert.That(data.Level, Is.Zero);
            Assert.That(data.Equipped.Body, Is.EqualTo("v3-body"));
        }
    }
}
