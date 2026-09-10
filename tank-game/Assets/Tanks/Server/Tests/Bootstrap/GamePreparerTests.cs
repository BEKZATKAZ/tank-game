using NSubstitute;
using NUnit.Framework;
using System.Linq;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Server.Bootstrap;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Generation;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Server.Tests.Bootstrap
{
    public sealed class GamePreparerTests
    {
        private IConnectedClientService _clientService;
        private ISessionSettingsService _settings;
        private GamePreparer _component;

        [SetUp]
        public void Setup()
        {
            _clientService = Substitute.For<IConnectedClientService>();

            _settings = Substitute.For<ISessionSettingsService>();
            _settings.GetSettings().MapGeneration.Returns(new MapGenerationSettings
            {
                Resolution = new Vector2Int(4, 3),
                WallWidth = 5
            });

            _component = new GamePreparer(_clientService, _settings);
            _component.Initialize(Substitute.For<IGameStartBehaviour>());
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(10)]
        public void Handle_PreparesGame(int playerCount)
        {
            _clientService
                .GetConnectedClientIds()
                .Returns(Enumerable.Range(1, playerCount).Select(x => (ulong)x).ToArray());

            for (ulong id = 1; id <= (ulong)playerCount; id++)
            {
                _clientService.GetClientData(id).Returns(new ClientData($"Test_{id}"));
            }

            _component.Handle(new PrepareGameRequested());

            _component.Behaviour.Received(1).DespawnAllPlayers();

            _component.Behaviour.Received(1).GenerateTilemapEveryoneRpc(
                Arg.Any<TilemapGenerationSettings>(),
                _settings.GetSettings().MapGeneration.Resolution);

            for (ulong id = 1; id <= (ulong)playerCount; id++)
            {
                string expectedName = $"Test_{id}";

                _component.Behaviour
                    .Received(1)
                    .SpawnPlayer(id, Arg.Any<float2>(), expectedName);
            }

            _component.Behaviour.Received(1).WaitForPlayers();
        }
    }
}
