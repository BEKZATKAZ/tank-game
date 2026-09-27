using Cysharp.Threading.Tasks;
using System;
using Tanks.Core.Common.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tanks.Infrastructure
{
    public sealed class UnitySceneLoader : ISceneLoader
    {
        public event Action LoadingStarted;
        public event Action LoadingEnded;

        public float Progress { get; set; }
        public bool WithTips { get; private set; }
        public bool IsLoading => _parameters != null;

        private ISceneParameters _parameters;
        private Scene _loadingScene;

        public T GetLoadingParameters<T>() where T : ISceneParameters
        {
            return (T)_parameters;
        }

        public void EndLoading<T>() where T : ISceneParameters
        {
            if (_parameters is not T) throw new Exception("Currently not loading");

            _parameters = null;
            Progress = 0;

            LoadingEnded?.Invoke();
        }

        public async UniTask StartLoadingAsync<T>(
            T parameters, bool withTips = true, bool loadActualScene = true)
            where T : ISceneParameters
        {
            if (IsLoading) throw new Exception("Already loading");

            Progress = 0;
            WithTips = withTips;
            _parameters = parameters;

            // Load only once and keep in memory
            if (_loadingScene.isLoaded == false)
            {
                SceneManager.LoadScene("Loading", LoadSceneMode.Additive);
                _loadingScene = SceneManager.GetSceneByName("Loading");
            }

            LoadingStarted?.Invoke();

            if (loadActualScene == false) return;
            await UniTask.Delay(500); // Warmup

            Scene activeScene = SceneManager.GetActiveScene();
            AsyncOperation unload = SceneManager.UnloadSceneAsync(activeScene);

            while (unload.isDone == false)
            {
                await UniTask.Yield();
                Progress = unload.progress * 0.25f; // 0.00 -> 0.25
            }

            AsyncOperation load = SceneManager
                .LoadSceneAsync(parameters.Name, LoadSceneMode.Additive);

            while (load.isDone == false)
            {
                await UniTask.Yield();
                Progress = load.progress * 0.25f + 0.25f; // 0.25 -> 0.50
            }

            Scene loadedScene = SceneManager.GetSceneByName(parameters.Name);
            SceneManager.SetActiveScene(loadedScene);
        }
    }
}
