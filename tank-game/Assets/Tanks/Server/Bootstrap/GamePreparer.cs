using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Tanks.Core.Helpers;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Generation;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace Tanks.Server.Bootstrap
{
    public sealed class GamePreparer :
        NetworkComponent<GameStartBehaviour>,
        INetworkMessageHandler<PrepareGameRequested>
    {
        public void Handle(PrepareGameRequested message)
        {
            foreach (var client in NetworkManager.ConnectedClients.Values)
            {
                if (client.PlayerObject == false) continue;

                client.PlayerObject.Despawn(destroy: true);
            }

            Vector2Int resolution = new Vector2Int(6, 4);
            int wallWidth = 5;

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
            Required<GameObject> playerPrefab = NetworkManager.NetworkConfig.PlayerPrefab;

            foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            {
                float2 coords = positionRandom.NextInt2(0, new int2(resolution.x, resolution.y));
                float2 position = coords * (wallWidth * 2) + wallWidth * 1.5f;
                float3 position3D = new float3(position, 0);

                GameObject player = Instantiate(playerPrefab.Value, position3D, quaternion.identity);
                NetworkObject networkObject = player.GetComponent<NetworkObject>();
                networkObject.SpawnAsPlayerObject(clientId, destroyWithScene: true);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
