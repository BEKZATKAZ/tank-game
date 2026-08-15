using Cysharp.Threading.Tasks;

namespace Tanks.Core.Lobbies
{
    public interface ILobbyService
    {
        LobbyState CurrentLobbyState { get; }

        UniTask<ResultType<LobbyState>> HostLobbyAsync(string name, uint maxPlayers);
        UniTask<ResultType<LobbyState>> JoinLobbyAsync(string address);

        void RenameLobby(string newName);
        void LeaveLobby();
    }
}
