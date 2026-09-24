using Cysharp.Threading.Tasks;
using Tanks.Core.Lobbies.Sessions;
using UnityEngine.SceneManagement;

namespace Tanks.Core.Lobbies.UseCases
{
    public interface IConnectToServerUseCase
    {
        UniTask ConnectAsync(string joinCode);
    }

    public sealed class ConnectToServerUseCase : IConnectToServerUseCase
    {
        public ConnectToServerUseCase(ISessionConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        private readonly ISessionConnectionService _connectionService;

        public async UniTask ConnectAsync(string joinCode)
        {
            _connectionService.SetConnectionData(joinCode);
            await SceneManager.LoadSceneAsync("Gameplay").ToUniTask();
        }
    }
}
