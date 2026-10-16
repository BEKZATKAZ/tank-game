using Tanks.Shared.Generation;
using Tanks.Shared.Teams;
using Tanks.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Shared.Bootstrap
{
    public struct WaitingForPlayersRequested { }
    public struct PrepareGameRequested { }

    public struct EndGameFromServer
    {
        public TeamId WinningTeamId;
    }

    public struct PlayerReadyConfirmed
    {
        public int SecretHandle;
    }

    public struct PlayerReadyRequested
    {
        public int SecretHandle;
    }

    public interface IGameStartBehaviour : INetworkBehaviourExtended
    {
        TemporaryArrayResult<GeneratedTile> GenerateTilemap(
            TilemapGenerationSettings settings, Vector2Int resolution);

        void SendReadyConfirmationServerRpc(int secretHandle);
        void RequestReadyConfirmation(ulong clientId, int secretHandle);

        void EndGameEveryoneRpc(TeamId winningTeamId);
        void StartGameEveryoneRpc(GameStartedFromServer message);

        void SpawnPlayer(ulong clientId, float2 position);
        void DespawnAllPlayers();

        void PrepareGame();
        void WaitForPlayers();
    }
}
