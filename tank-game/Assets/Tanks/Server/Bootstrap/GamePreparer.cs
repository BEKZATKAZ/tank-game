using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Generation;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Server.Bootstrap
{
    public sealed class GamePreparer :
        NetworkComponent<IGameStartBehaviour>,
        INetworkMessageHandler<PrepareGameRequested>
    {
        public GamePreparer(
            IConnectedClientService clientService, 
            ISessionSettingsService settings)
        {
            _clientService = clientService;
            _settings = settings;
        }

        private readonly IConnectedClientService _clientService;
        private readonly ISessionSettingsService _settings;

        public void Handle(PrepareGameRequested message)
        {
            Behaviour.DespawnAllPlayers();

            Vector2Int resolution = _settings.GetSettings().MapGeneration.Resolution;
            int wallWidth = _settings.GetSettings().MapGeneration.WallWidth;

            GenerateMap(wallWidth, resolution);
            SpawnPlayers(wallWidth, resolution);

            Behaviour.WaitForPlayers();
        }

        private void GenerateMap(int wallWidth, Vector2Int resolution)
        {
            Span<byte> seedBuffer = stackalloc byte[sizeof(uint)];
            RandomNumberGenerator.Fill(seedBuffer);

            TilemapGenerationSettings settings = new TilemapGenerationSettings
            {
                Seed = MemoryMarshal.Read<uint>(seedBuffer),
                TileSpacing = 1,
                WallWidth = wallWidth
            };

            Behaviour.GenerateTilemapEveryoneRpc(settings, resolution);
        }

        private void SpawnPlayers(int wallWidth, Vector2Int resolution)
        {
            Span<byte> positionSeedBuffer = stackalloc byte[sizeof(uint)];
            RandomNumberGenerator.Fill(positionSeedBuffer);

            uint positionSeed = MemoryMarshal.Read<uint>(positionSeedBuffer);
            var positionRandom = Unity.Mathematics.Random.CreateFromIndex(positionSeed);

            foreach (ulong clientId in _clientService.GetConnectedClientIds())
            {
                float2 coords = positionRandom.NextInt2(0, new int2(resolution.x, resolution.y));
                float2 position = coords * (wallWidth * 2) + wallWidth * 1.5f;

                ClientData data = _clientService.GetClientData(clientId);
                Behaviour.SpawnPlayer(clientId, position, data.DisplayName);
            }
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
