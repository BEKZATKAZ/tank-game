using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace Tanks.Core.Lobbies
{
    public interface ILobbyService
    {
        LobbyState CurrentLobbyState { get; }

        UniTask<ResultType<IEnumerable<LobbySearchState>>> FindLobbiesAsync(string name = null);
        UniTask<ResultType<LobbyState>> HostLobbyAsync(string name, uint maxPlayers);
        UniTask<ResultType<LobbyState>> JoinLobbyAsync(string address);

        void RenameLobby(string newName);
        void LeaveLobby();
    }
}
