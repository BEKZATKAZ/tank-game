using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Transitions
{
    public sealed class FadeTransitionView : ViewBase<FadeTransitionViewModel>
    {
        private VisualElement _fill;

        protected override VisualElement GetRootElement() => _fill;

        protected override void Initialize(VisualElement root)
        {
            _fill = root.Q("Fill");
        }

        protected override async void Bind(FadeTransitionViewModel viewModel)
        {
            viewModel.EndMenuLoading();

            _fill.style.opacity = 1;
            _fill.pickingMode = PickingMode.Position;

            await UniTask.Delay(1200);
            _fill.pickingMode = PickingMode.Ignore;

            await LMotion.Create(1f, 0f, duration: 2.5f).Bind(x => _fill.style.opacity = x);
        }
    }
}
