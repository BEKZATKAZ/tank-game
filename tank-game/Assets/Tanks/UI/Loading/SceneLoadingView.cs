using Cysharp.Threading.Tasks;
using LitMotion;
using System;
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

        protected override VisualElement GetRootElement() => _root;

        private async UniTaskVoid TipLoop()
        {
            while (ViewModel != null)
            {
                _tip.text = await ViewModel.GetNextTipAsync();

                await LMotion
                    .Create(0f, 1f, _tipFadeDuration)
                    .Bind(x => _tipContainer.style.opacity = x);

                await UniTask
                    .Delay(TimeSpan.FromSeconds(_tipDuration));

                await LMotion
                    .Create(1f, 0f, _tipFadeDuration)
                    .Bind(x => _tipContainer.style.opacity = x);
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
            ViewModel.Progress.Bind(x => _progress.value = x);

            ViewModel.IsLoading.Bind(x =>
            {
                _root.style.display = x ? DisplayStyle.Flex : DisplayStyle.None;
                _loadingCamera.gameObject.SetActive(x);
            });

            if (ViewModel.WithTips) TipLoop().Forget();
        }

        private void LateUpdate()
        {
            _title.style.opacity = math.abs(math.sin(Time.time * math.PI)) * 0.6f + 0.2f;
            ViewModel?.Tick(Time.deltaTime);
        }
    }
}
