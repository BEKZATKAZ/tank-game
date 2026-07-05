using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Generation;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace Tanks.Server.Bootstrap
{
    public sealed class GameStarter : NetworkComponent<GameStartBehaviour>
    {
        private readonly int _desiredCount = 2; // ! Testing purposes
        private bool _isGameStarted;

        private void Start()
        {
            NetworkManager.ConnectionApprovalCallback += ApprovalCheck;
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
        }

        private void StartGame()
        {
            if (_isGameStarted) return;
            _isGameStarted = true;

            Span<byte> seedBuffer = stackalloc byte[sizeof(uint)];
            RandomNumberGenerator.Fill(seedBuffer);

            int tileSpacing = 1;
            int wallWidth = 5;

            TilemapGenerationSettings settings = new TilemapGenerationSettings
            {
                Seed = MemoryMarshal.Read<uint>(seedBuffer),
                TileSpacing = tileSpacing,
                WallWidth = wallWidth
            };

            Vector2Int resolution = new Vector2Int(6, 4);
            Behaviour.GenerateTilemapEveryoneRpc(settings, resolution);

            Span<byte> positionSeedBuffer = stackalloc byte[sizeof(uint)];
            RandomNumberGenerator.Fill(positionSeedBuffer);

            uint positionSeed = MemoryMarshal.Read<uint>(positionSeedBuffer);
            var positionRandom = Unity.Mathematics.Random.CreateFromIndex(positionSeed);
            GameObject playerPrefab = NetworkManager.NetworkConfig.PlayerPrefab;

            foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            {
                float2 coords = positionRandom.NextInt2(0, new int2(resolution.x, resolution.y));

                float2 position = coords * (wallWidth * 2) + wallWidth * 1.5f;
                float3 position3D = new float3(position, 0);

                GameObject player = Instantiate(playerPrefab, position3D, quaternion.identity);
                NetworkObject networkObject = player.GetComponent<NetworkObject>();
                networkObject.SpawnAsPlayerObject(clientId, destroyWithScene: true);
            }
        }

        private void ApprovalCheck(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            response.CreatePlayerObject = false;
            response.Approved = _isGameStarted == false;
        }

        private void OnClientConnected(ulong clientId)
        {
            int currentCount = NetworkManager.ConnectedClients.Count;
            if (currentCount < _desiredCount) return;
            StartGame();
        }
    }
}
