using Cysharp.Threading.Tasks;
using LitMotion;
using System;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Loading
{
    public sealed class SceneLoadingView : ViewBase<SceneLoadingViewModel>
    {
        [SerializeField] private Camera _loadingCamera;
        [SerializeField] private float _tipFadeDuration = 1;
        [SerializeField] private float _tipDuration = 5;

        private VisualElement _root;
        private VisualElement _tipContainer;
        private ProgressBar _progress;
        private Label _title;
        private Label _tip;
        private CancellationTokenSource _tipCancellation;

        protected override VisualElement GetRootElement() => _root;

        private async UniTask TipLoop(
            SceneLoadingViewModel viewModel, CancellationToken cancellationToken)
        {
            while (true)
            {
                _tip.text = await viewModel.GetNextTipAsync()
                    .AttachExternalCancellation(cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                await LMotion
                    .Create(0f, 1f, _tipFadeDuration)
                    .Bind(x => _tipContainer.style.opacity = x)
                    .ToUniTask(cancellationToken);

                await UniTask
                    .Delay(TimeSpan.FromSeconds(_tipDuration),
                        cancellationToken: cancellationToken);

                await LMotion
                    .Create(1f, 0f, _tipFadeDuration)
                    .Bind(x => _tipContainer.style.opacity = x)
                    .ToUniTask(cancellationToken);
            }
        }

        protected override void Initialize(VisualElement root)
        {
            _root = root;
            _tipContainer = _root.Q("Bottom");

            _progress = _root.Q<ProgressBar>();
            _title = _root.Q<Label>("Title");

            _tip = _tipContainer.Q<Label>("Tip");
            _tip.text = "";
        }

        protected override void Bind(SceneLoadingViewModel viewModel)
        {
            viewModel.Progress.Bind(OnProgressChanged);
            viewModel.IsLoading.Bind(OnLoadingChanged);
        }

        protected override void Unbind(SceneLoadingViewModel viewModel)
        {
            viewModel.Progress.Unbind(OnProgressChanged);
            viewModel.IsLoading.Unbind(OnLoadingChanged);
            StopTips();
        }

        protected override void OnDestroy()
        {
            if (ViewModel is null) return;
            Unbind(ViewModel);
            ViewModel.Dispose();
        }

        private void OnProgressChanged(float progress) => _progress.value = progress;

        private void OnLoadingChanged(bool isLoading)
        {
            _root.style.display = isLoading ? DisplayStyle.Flex : DisplayStyle.None;
            _loadingCamera.gameObject.SetActive(isLoading);

            StopTips();
            if (isLoading == false || ViewModel.WithTips == false) return;

            _tipContainer.style.display = DisplayStyle.Flex;
            _tipCancellation = new CancellationTokenSource();
            TipLoop(ViewModel, _tipCancellation.Token).Forget();
        }

        private void StopTips()
        {
            _tipCancellation?.Cancel();
            _tipCancellation?.Dispose();
            _tipCancellation = null;

            _tip.text = "";
            _tipContainer.style.opacity = 0;
            _tipContainer.style.display = DisplayStyle.None;
        }

        private void LateUpdate()
        {
            _title.style.opacity = math.abs(math.sin(Time.time * math.PI)) * 0.6f + 0.2f;
            ViewModel?.Tick(Time.deltaTime);
        }
    }
}
