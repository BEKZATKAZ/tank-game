using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using Tanks.Core.Lobbies.Payloads;

namespace Tanks.Core.Lobbies
{
    public interface ILobbyService
    {
        event Action<LobbyPlayerState> PlayerJoined;
        event Action<LobbyPlayerState> PlayerLeft;
        event Action LobbyDeleted;

        LobbyState CurrentLobbyState { get; }

        UniTask<ResultType<LobbyState>> HostLobbyAsync(string name, uint maxPlayers);
        UniTask<ResultType<LobbyState>> UpdateLobbyAsync(LobbyUpdateRequest options);
        UniTask<ResultType> LeaveLobbyAsync();
        UniTask<ResultType> SetReadyAsync(bool ready);

        UniTask<ResultType<IEnumerable<LobbySearchState>>> FindLobbiesAsync(string name = null);
        UniTask<ResultType<LobbyState>> JoinLobbyAsync(LobbyAddress address);
        UniTask<ResultType<LobbyState>> JoinLobbyAsync(LobbyId id);
    }
}
