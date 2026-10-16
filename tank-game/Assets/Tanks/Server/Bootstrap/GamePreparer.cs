using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Server.Generation;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Generation;
using Tanks.Utils;
using Unity.Collections;
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
            ISessionSettingsService settings,
            ICrateSpawner crateSpawner)
        {
            _clientService = clientService;
            _settings = settings;
            _crateSpawner = crateSpawner;
        }

        private readonly IConnectedClientService _clientService;
        private readonly ISessionSettingsService _settings;
        private readonly ICrateSpawner _crateSpawner;

        private const float PlayerSpawnRadius = 2;

        public void Handle(PrepareGameRequested message)
        {
            Behaviour.DespawnAllPlayers();

            var wallWidth = _settings.GetSettings().MapGeneration.WallWidth;
            var resolution = _settings.GetSettings().MapGeneration.Resolution;
            var settings = GenerateSettings(wallWidth);

            using var tiles = GenerateMap(settings, resolution);
            Physics2D.SyncTransforms();

            _crateSpawner.Clear();
            _crateSpawner.Spawn(tiles.Value.AsSpan(), settings.Seed);

            SpawnPlayers(wallWidth, resolution);
            Behaviour.WaitForPlayers();
        }

        private TemporaryArrayResult<GeneratedTile> GenerateMap(
            TilemapGenerationSettings settings, Vector2Int resolution)
        {
            return Behaviour.GenerateTilemap(settings, resolution);
        }

        private TilemapGenerationSettings GenerateSettings(int wallWidth)
        {
            Span<byte> seedBuffer = stackalloc byte[sizeof(uint)];
            RandomNumberGenerator.Fill(seedBuffer);

            return new TilemapGenerationSettings
            {
                Seed = MemoryMarshal.Read<uint>(seedBuffer),
                TileSpacing = 1,
                WallWidth = wallWidth
            };
        }

        private void SpawnPlayers(int wallWidth, Vector2Int resolution)
        {
            Span<byte> positionSeedBuffer = stackalloc byte[sizeof(uint)];
            RandomNumberGenerator.Fill(positionSeedBuffer);

            uint positionSeed = MemoryMarshal.Read<uint>(positionSeedBuffer);
            var positionRandom = Unity.Mathematics.Random.CreateFromIndex(positionSeed);

            using NativeList<float2> candidates = CreateEdgeCandidates(wallWidth, resolution);
            using NativeList<float2> usedPositions = new(4, Allocator.Temp);

            foreach (ulong clientId in _clientService.GetConnectedClientIds())
            {
                int candidateIndex = FindBestSpawnCandidate(candidates, usedPositions, positionRandom);
                if (candidateIndex < 0) continue;

                float2 position = candidates[candidateIndex];
                candidates.RemoveAt(candidateIndex);
                usedPositions.Add(position);
                Behaviour.SpawnPlayer(clientId, position);
            }
        }

        private static NativeList<float2> CreateEdgeCandidates(int wallWidth, Vector2Int resolution)
        {
            int candidateCapacity = (resolution.x + resolution.y) * 2;
            NativeList<float2> candidates = new(candidateCapacity, Allocator.Temp);

            float step = wallWidth * 2f;
            float left = wallWidth * 1.5f;
            float right = (resolution.x * step) - wallWidth * 1.5f;
            float bottom = wallWidth * 1.5f;
            float top = (resolution.y * step) - wallWidth * 1.5f;

            for (int x = 0; x < resolution.x; x++)
            {
                float worldX = left + x * step;
                candidates.Add(new float2(worldX, bottom));
                if (resolution.y > 1) candidates.Add(new float2(worldX, top));
            }

            for (int y = 1; y < resolution.y - 1; y++)
            {
                float worldY = bottom + y * step;
                candidates.Add(new float2(left, worldY));
                if (resolution.x > 1) candidates.Add(new float2(right, worldY));
            }

            // Keep edge positions first, but retain every cell as a fallback when
            // maze walls occupy some or all of the perimeter.
            for (int y = 1; y < resolution.y - 1; y++)
            {
                for (int x = 1; x < resolution.x - 1; x++)
                {
                    candidates.Add(new float2(left + x * step, bottom + y * step));
                }
            }

            return candidates;
        }

        private static int FindBestSpawnCandidate(
            NativeList<float2> candidates,
            NativeList<float2> usedPositions,
            Unity.Mathematics.Random random)
        {
            int bestIndex = -1;
            float bestDistance = float.MinValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                float2 candidate = candidates[i];
                float2 position = new float2(candidate.x, candidate.y);

                if (Physics2D.OverlapCircle(position, PlayerSpawnRadius) != null) continue;
                float nearestDistance = float.MaxValue;

                foreach (float2 usedPosition in usedPositions)
                {
                    float length = math.lengthsq(candidate - usedPosition);
                    nearestDistance = math.min(nearestDistance, length);
                }

                if (usedPositions.Count == 0) nearestDistance = 0f;

                float distance = math.abs(nearestDistance - bestDistance);

                if (nearestDistance > bestDistance || (distance < 0.001f && random.NextBool()))
                {
                    bestDistance = nearestDistance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
