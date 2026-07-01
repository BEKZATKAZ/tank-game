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
        private readonly int _desiredCount = 1; // ! Testing purposes

        private void Start()
        {
            NetworkManager.ConnectionApprovalCallback += ApprovalCheck;
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
        }

        private void StartGame()
        {
            Span<byte> seedBuffer = stackalloc byte[sizeof(uint)];
            RandomNumberGenerator.Fill(seedBuffer);

            Behaviour.GenerateTilemapEveryoneRpc(new TilemapGenerationSettings
            {
                Seed = MemoryMarshal.Read<uint>(seedBuffer),
                TileSpacing = 1,
                WallWidth = 5
            },
            resolution: new Vector2Int(6, 4));

            GameObject playerPrefab = NetworkManager.NetworkConfig.PlayerPrefab;

            foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            {
                GameObject player = Instantiate(
                    playerPrefab, new float3(8, 8, 0), quaternion.identity);

                player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
            }
        }

        private void ApprovalCheck(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            response.CreatePlayerObject = false;
            response.Approved = true;
        }

        private void OnClientConnected(ulong clientId)
        {
            int currentCount = NetworkManager.ConnectedClients.Count;
            if (currentCount < _desiredCount) return;
            StartGame();
        }
    }
}
