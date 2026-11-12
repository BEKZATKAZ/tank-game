using Tanks.Core.Common.Scenes;

namespace Tanks.Client.Connection
{
    public sealed class ClientConnectionRecoveryService
    {
        public ClientConnectionRecoveryService(
            ISceneLoader scenes,
            GameplaySessionService session)
        {
            _scenes = scenes;
            _session = session;
        }

        private readonly ISceneLoader _scenes;
        private readonly GameplaySessionService _session;
        private bool _recovering;

        public void ReturnToMenus(string error = null)
        {
            if (_recovering) return;
            _recovering = true;
            _session.ConnectionError = error;

            if (_scenes.IsLoading) _scenes.EndLoading<LoadGameplay>();

            _scenes.StartLoading(new LoadMenus(), withTips: false);
        }
    }
}
