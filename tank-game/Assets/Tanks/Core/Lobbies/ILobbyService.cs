using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tanks.Core.Lobbies.Payloads;

namespace Tanks.Core.Lobbies
{
    public interface ILobbyService
    {
        event Action<LobbyPlayerState> PlayerJoined;
        event Action<LobbyPlayerState> PlayerLeft;
        event Action LobbyDeleted;

        LobbyState CurrentLobbyState { get; }

        Task<ResultType<LobbyState>> HostLobbyAsync(string name, uint maxPlayers);
        Task<ResultType<LobbyState>> UpdateLobbyAsync(LobbyUpdateRequest options);
        Task<ResultType> LeaveLobbyAsync();
        Task<ResultType> SetReadyAsync(bool ready);

        Task<ResultType<IEnumerable<LobbySearchState>>> FindLobbiesAsync(string name = null);
        Task<ResultType<LobbyState>> JoinLobbyAsync(LobbyAddress address);
        Task<ResultType<LobbyState>> JoinLobbyAsync(LobbyId id);
    }
}
