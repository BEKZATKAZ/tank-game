using Tanks.Core.Common.Scenes;
using Tanks.Core.Lobbies.Sessions;

namespace Tanks.Core.Lobbies.UseCases
{
    public interface IConnectToServerUseCase
    {
        void Load(string joinCode, SessionSettings settings);
    }

    public sealed class ConnectToServerUseCase : IConnectToServerUseCase
    {
        public ConnectToServerUseCase(ISceneLoader loader)
        {
            _loader = loader;
        }

        private readonly ISceneLoader _loader;

        public void Load(string joinCode, SessionSettings settings)
        {
            _loader.StartLoading(new LoadGameplay
            {
                JoinCode = joinCode,
                Settings = settings
            });
        }
    }
}
