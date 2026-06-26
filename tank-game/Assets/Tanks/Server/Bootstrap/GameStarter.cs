using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Generation;
using Unity.Netcode;
using UnityEngine;

namespace Tanks.Server.Bootstrap
{
    public sealed class GameStarter : NetworkComponent<GameStartBehaviour>
    {
        private readonly int _desiredCount = 1; // ! Testing purposes

        private void Start()
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        private void OnClientConnected(ulong clientId)
        {
            int currentCount = NetworkManager.Singleton.ConnectedClients.Count;
            if (currentCount < _desiredCount) return;

            Span<byte> seedBuffer = stackalloc byte[sizeof(uint)];
            RandomNumberGenerator.Fill(seedBuffer);

            Behaviour.GenerateTilemapEveryoneRpc(new TilemapGenerationSettings
            {
                Seed = MemoryMarshal.Read<uint>(seedBuffer),
                TileSpacing = 1,
                WallWidth = 5
            },
            resolution: new Vector2Int(6, 4));
        }
    }
}
