using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Main
{
    public sealed class CopyrightView : ViewBase<CopyrightViewModel>
    {
        private Label _label;

        protected override VisualElement GetRootElement() => _label;

        protected override void Initialize(VisualElement root)
        {
            _label = root.Q<Label>("Copyright");
        }

        protected override void Bind(CopyrightViewModel viewModel)
        {
            viewModel.Label.Bind(x => _label.text = x);
        }
    }
}
