using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using Tanks.Core.Common.Scenes;
using Tanks.Localization;
using Tanks.Utils;
using UnityEngine;

namespace Tanks.UI.Loading
{
    public sealed class SceneLoadingViewModel : IViewModel, IDisposable
    {
        public SceneLoadingViewModel(
            ILocalizationDatabase localization,
            ISceneLoader loader)
        {
            _loader = loader;

            _allTips = localization
                .GetAllFromTable("Loading")
                .Where(x => x.EntryName.Contains("tip."))
                .ToArray();

            Shuffle(_allTips);

            _loader.LoadingStarted += OnLoadingStarted;
            _loader.LoadingEnded += OnLoadingEnded;
        }

        public BindableProperty<float> Progress { get; } = new();
        public BindableProperty<bool> IsLoading { get; } = new(true);
        public bool WithTips => _loader.WithTips;

        private readonly ISceneLoader _loader;
        private readonly ILocalizedString[] _allTips;
        private int _nextTipIndex;

        private void OnLoadingStarted() => IsLoading.Value = true;
        private void OnLoadingEnded() => IsLoading.Value = false;

        public void Dispose()
        {
            _loader.LoadingStarted -= OnLoadingStarted;
            _loader.LoadingEnded -= OnLoadingEnded;
        }

        public async UniTask<string> GetNextTipAsync()
        {
            ILocalizedString tip = _allTips[_nextTipIndex++];

            if (_nextTipIndex >= _allTips.Length) _nextTipIndex = 0;

            return await tip.GetAsync();
        }

        public void Tick(float deltaTime)
        {
            float target = _loader.Progress;

            if (target < Progress.Value)
            {
                Progress.Value = target;
                return;
            }

            Progress.Value = Mathf.MoveTowards(Progress.Value, target, deltaTime * 2);
        }

        private static void Shuffle<T>(T[] array)
        {
            int n = array.Length;

            while (n > 1)
            {
                int k = UnityEngine.Random.Range(0, n--);
                (array[k], array[n]) = (array[n], array[k]);
            }
        }
    }
}
