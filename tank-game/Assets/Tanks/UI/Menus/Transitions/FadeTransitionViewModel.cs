using System;
using System.Threading.Tasks;
using Tanks.Core.Common.Scenes;
using Tanks.Core.Players;

namespace Tanks.UI.Menus.Transitions
{
    public sealed class FadeTransitionViewModel : IViewModel, IDisposable
    {
        public FadeTransitionViewModel(
            ISceneLoader sceneLoader, ILocalPlayerService player, MenuLoadingState loading)
        {
            _sceneLoader = sceneLoader;
            _player = player;
            _loading = loading;
            _loading.RetryRequested += Retry;
        }

        public event Action Ready;

        private readonly ISceneLoader _sceneLoader;
        private readonly ILocalPlayerService _player;
        private readonly MenuLoadingState _loading;
        private bool _busy;
        private bool _disposed;

        public async Task InitializeAsync()
        {
            if (_busy || _disposed || _loading.IsReady.Value) return;
            _busy = true;
            _loading.HasError.Value = false;

            try
            {
                var result = await _player.LoadDataAsync();
                if (_disposed) return;

                if (result.IsSuccess == false)
                {
                    _loading.HasError.Value = true;
                    return;
                }

                if (_sceneLoader.IsLoading) _sceneLoader.EndLoading<LoadMenus>();
                _loading.IsReady.Value = true;
                Ready?.Invoke();
            }
            catch (Exception)
            {
                if (_disposed == false) _loading.HasError.Value = true;
            }
            finally
            {
                _busy = false;
            }
        }

        private void Retry() => _ = InitializeAsync();

        public void Dispose()
        {
            _disposed = true;
            _loading.RetryRequested -= Retry;
        }
    }
}
