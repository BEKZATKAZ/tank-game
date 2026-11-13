using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Transitions
{
    public sealed class FadeTransitionView : ViewBase<FadeTransitionViewModel>
    {
        [SerializeField] private UnityEvent _onFadeOut;

        private VisualElement _fill;

        protected override VisualElement GetRootElement() => _fill;

        protected override void Initialize(VisualElement root)
        {
            _fill = root.Q("Fill");
        }

        protected override void Bind(FadeTransitionViewModel viewModel)
        {
            _fill.style.opacity = 1;
            _fill.pickingMode = PickingMode.Position;

            viewModel.Ready += Fade;
            _ = viewModel.InitializeAsync();
        }

        protected override void Unbind(FadeTransitionViewModel viewModel)
        {
            viewModel.Ready -= Fade;
        }

        private void Fade() => FadeAsync().Forget();

        private async UniTask FadeAsync()
        {
            _onFadeOut?.Invoke();

            await UniTask.Delay(500, cancellationToken: destroyCancellationToken);
            _fill.pickingMode = PickingMode.Ignore;

            await LMotion.Create(1f, 0f, duration: 2.5f)
                .Bind(x => _fill.style.opacity = x).ToUniTask(destroyCancellationToken);
        }
    }
}
