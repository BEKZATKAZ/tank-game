using Cysharp.Threading.Tasks;
using Tanks.Client.Items;
using Tanks.Core.Common.Scenes;
using Tanks.Shared.Items;
using Tanks.Shared.Playground;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Playground
{
    public class PlaygroundClientConnector : MonoBehaviour
    {
        [Inject] private readonly ISceneLoader _sceneLoader;
        [Inject] private readonly IItemDatabase _sharedItemDatabase;
        [Inject] private readonly IClientItemDatabase _clientItemDatabase;

        private UniTask InitializeItemsAsync()
        {
            UniTask shared = _sharedItemDatabase.WaitForInitializationAsync();
            UniTask client = _clientItemDatabase.WaitForInitializationAsync();
            return UniTask.WhenAll(shared, client);
        }

        private void OnConnectionEvent(NetworkManager manager, ConnectionEventData e)
        {
            if (e.EventType != ConnectionEvent.ClientDisconnected) return;
            _sceneLoader.StartLoadingAsync(new LoadMenus(), withTips: false);
        }

        private async void Start()
        {
#if UNITY_EDITOR
            if (_sceneLoader.IsLoading == false)
            {
                _sceneLoader
                    .StartLoadingAsync(new LoadPlayground(), loadActualScene: false)
                    .Forget();
            }
#endif

            await InitializeItemsAsync();

            _sceneLoader.Progress = 0.6f;
            await UniTask.DelayFrame(1);

            NetworkManager manager = NetworkManager.Singleton;
            manager.OnConnectionEvent += OnConnectionEvent;
            manager.StartClient();

            _sceneLoader.Progress = 1;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton == false) return;
            NetworkManager.Singleton.OnConnectionEvent -= OnConnectionEvent;
        }
    }
}
