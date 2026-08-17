using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Tanks.Core.Lobbies
{
    public interface ILobbyService
    {
        event Action<LobbyPlayerState> PlayerJoined;
        event Action<LobbyPlayerState> PlayerLeft;

        LobbyState CurrentLobbyState { get; }

        UniTask<ResultType<IEnumerable<LobbySearchState>>> FindLobbiesAsync(string name = null);
        UniTask<ResultType<LobbyState>> HostLobbyAsync(string name, uint maxPlayers);
        UniTask<ResultType<LobbyState>> JoinLobbyAsync(LobbyAddress address);

        void RenameLobby(string newName);
        void LeaveLobby();
    }
}
