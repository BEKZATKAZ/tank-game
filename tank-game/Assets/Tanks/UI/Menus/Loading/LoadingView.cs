using LitMotion;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Loading
{
    public sealed class LoadingView : ViewBase<LoadingViewModel>
    {
        private Label _label;

        private readonly string[] _labelValue = new string[4];

        protected override VisualElement GetRootElement() => _label;

        protected override void Initialize(VisualElement root)
        {
            _label = root.Q<Label>("LoadingLabel");
        }

        protected override void Bind(LoadingViewModel viewModel)
        {
            viewModel.Label.Bind(x =>
            {
                // Pre-create strings to avoid constant string allocation
                _labelValue[0] = x;
                _labelValue[1] = x + ".";
                _labelValue[2] = x + "..";
                _labelValue[3] = x + "...";
            });

            LMotion.Create(0f, 1f, 0.25f)
                .WithEase(Ease.OutExpo)
                .Bind(x => _label.style.opacity = x);
        }

        private void LateUpdate()
        {
            if (ViewModel is null) return;
            int index = (int)(Time.time % 1 * 4);
            _label.text = _labelValue[index];
        }
    }
}
