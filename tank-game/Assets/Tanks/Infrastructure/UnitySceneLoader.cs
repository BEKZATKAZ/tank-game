using System;
using Tanks.Core.Common.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tanks.Infrastructure
{
    public enum LoadingStep
    {
        Idle,
        WarmingUp,
        UnloadingCurrent,
        LoadingNew
    }

    public sealed class UnitySceneLoader : MonoBehaviour, ISceneLoader
    {
        public event Action LoadingStarted;
        public event Action LoadingEnded;

        public float Progress { get; set; }
        public bool WithTips { get; private set; }
        public bool IsLoading { get; private set; }

        private ISceneParameters _parameters;
        private LoadingStep _step;
        private AsyncOperation _process;
        private Scene _loadingScreenScene;
        private float _loadingStartTime;
        private float _loadUntil;

        public T GetLoadingParameters<T>() where T : ISceneParameters
        {
            return (T)_parameters;
        }

        public void EndLoading<T>() where T : ISceneParameters
        {
            if (IsLoading == false) throw new Exception("Currently not loading");

            IsLoading = false;
            Progress = 0;

            LoadingEnded?.Invoke();
        }

        public void StartLoading<T>(
            T parameters, bool withTips = true, bool loadActualScene = true, float loadUntil = 0.5f)
            where T : ISceneParameters
        {
            if (_step is not LoadingStep.Idle) throw new Exception("Already loading");

            IsLoading = true;
            Progress = 0;
            WithTips = withTips;

            _parameters = parameters;
            _loadUntil = loadUntil;

            // Load only once and keep in memory
            if (_loadingScreenScene.isLoaded == false)
            {
                SceneManager.LoadScene("Loading", LoadSceneMode.Additive);
                _loadingScreenScene = SceneManager.GetSceneByName("Loading");
            }

            LoadingStarted?.Invoke();
            if (loadActualScene == false) return;
            
            _step = LoadingStep.WarmingUp;
            _loadingStartTime = Time.realtimeSinceStartup;
        }

        private void AddProgress()
        {
            float halfUntil = _loadUntil * 0.5f;
            float progress = _process.progress * halfUntil;

            if (_step is LoadingStep.LoadingNew) progress += halfUntil;
            Progress = progress;
        }

        private void LoadRequestedScene()
        {
            _process = SceneManager.LoadSceneAsync(_parameters.Name, LoadSceneMode.Additive);
            _step = LoadingStep.LoadingNew;
        }

        private void FinishLoading()
        {
            Scene loadedScene = SceneManager.GetSceneByName(_parameters.Name);
            SceneManager.SetActiveScene(loadedScene);

            _process = null;
            _step = LoadingStep.Idle;
        }

        private void LateUpdate()
        {
            if (_step is LoadingStep.Idle) return;

            if (_step is LoadingStep.WarmingUp)
            {
                if (Time.realtimeSinceStartup - _loadingStartTime < 0.5f) return;
                _step = LoadingStep.UnloadingCurrent;

                Scene activeScene = SceneManager.GetActiveScene();
                _process = SceneManager.UnloadSceneAsync(activeScene);
            }

            AddProgress();

            if (_process.isDone == false) return;

            switch (_step)
            {
                case LoadingStep.UnloadingCurrent:
                    LoadRequestedScene();
                    break;

                case LoadingStep.LoadingNew:
                    FinishLoading();
                    break;
            }
        }
    }
}
