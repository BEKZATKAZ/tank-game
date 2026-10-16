using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Server.Bootstrap;
using Tanks.Server.Generation;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Generation;
using Tanks.Shared.Items;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Server.Tests.Bootstrap
{
    // No NSubstitute since spans don't work well with mocking
    class TestCrateSpawner : ICrateSpawner
    {
        public int SpawnCallCount { get; private set; }
        public int ReturnCallCount { get; private set; }
        public int ClearCallCount { get; private set; }

        public void Spawn(ReadOnlySpan<GeneratedTile> tiles, uint seed)
        {
            SpawnCallCount++;
        }

        public void Return(ICrateBehaviour instance)
        {
            ReturnCallCount++;
        }

        public void Clear()
        {
            ClearCallCount++;
        }
    }

    public sealed class GamePreparerTests
    {
        private IConnectedClientService _clientService;
        private ISessionSettingsService _settings;
        private TestCrateSpawner _crateSpawner;
        private GamePreparer _component;

        [SetUp]
        public void Setup()
        {
            _clientService = Substitute.For<IConnectedClientService>();
            _crateSpawner = new TestCrateSpawner();

            _settings = Substitute.For<ISessionSettingsService>();
            _settings.GetSettings().MapGeneration.Returns(new MapGenerationSettings
            {
                Resolution = new Vector2Int(4, 3),
                WallWidth = 5
            });

            _component = new GamePreparer(_clientService, _settings, _crateSpawner);
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

            _component.Behaviour.Received(1).GenerateTilemap(
                Arg.Any<TilemapGenerationSettings>(),
                _settings.GetSettings().MapGeneration.Resolution);

            Assert.That(_crateSpawner.ClearCallCount, Is.EqualTo(1));
            Assert.That(_crateSpawner.SpawnCallCount, Is.EqualTo(1));

            for (ulong id = 1; id <= (ulong)playerCount; id++)
            {
                _component.Behaviour.Received(1).SpawnPlayer(id, Arg.Any<float2>());
            }

            _component.Behaviour.Received(1).WaitForPlayers();
        }

        [Test]
        public void Handle_SpacesPlayersApart()
        {
            _clientService.GetConnectedClientIds().Returns(new[] { 1ul, 2ul, 3ul, 4ul });
            _component.Handle(new PrepareGameRequested());

            List<float2> positions = SpawnedPositions();

            for (int i = 0; i < positions.Count; i++)
            {
                for (int j = i + 1; j < positions.Count; j++)
                {
                    Assert.That(math.distance(positions[i], positions[j]), Is.GreaterThan(0f));
                }
            }
        }

        private List<float2> SpawnedPositions()
        {
            var positions = new List<float2>();
            foreach (var call in _component.Behaviour.ReceivedCalls())
            {
                if (call.GetMethodInfo().Name == nameof(IGameStartBehaviour.SpawnPlayer))
                    positions.Add((float2)call.GetArguments()[1]);
            }

            return positions;
        }
    }
}
