using Cysharp.Threading.Tasks;
using Tanks.Core.Lobbies.Payloads;

namespace Tanks.Core.Lobbies.UseCases
{
    public interface IRenameLobbyUseCase
    {
        UniTask<ResultType> RenameAsync(string newName);
    }

    public sealed class RenameLobbyUseCase : IRenameLobbyUseCase
    {
        public RenameLobbyUseCase(ILobbyService service)
        {
            _service = service;
        }

        private readonly ILobbyService _service;

        public async UniTask<ResultType> RenameAsync(string newName)
        {
            return await _service.UpdateLobbyAsync(new LobbyUpdateRequest { Name = newName });
        }
    }
}
