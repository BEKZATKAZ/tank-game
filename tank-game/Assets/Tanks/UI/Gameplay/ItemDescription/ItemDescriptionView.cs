using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay.ItemDescription
{
    public sealed class ItemDescriptionView : ViewBase<ItemDescriptionViewModel>
    {
        private VisualElement _container;
        private VisualElement _icon;
        private Label _name;
        private Label _description;
        private Label _tier;

        protected override VisualElement GetRootElement() => _container;

        protected override void Initialize(VisualElement root)
        {
            _container = root.Q("ItemDescriptionContainer");
            _icon = root.Q("ItemIcon");
            _name = _container.Q<Label>("ItemName");
            _description = _container.Q<Label>("ItemDescription");
            _tier = _container.Q<Label>("ItemTier");
        }

        protected override void Bind(ItemDescriptionViewModel viewModel)
        {
            viewModel.Name.Bind(x => _name.text = x);
            viewModel.Description.Bind(x => _description.text = x);
            viewModel.TierName.Bind(x => _tier.text = x);

            viewModel.AccentColor.Bind(x =>
            {
                _tier.style.color = x;
                _container.style.borderTopColor = x;
                _container.style.borderRightColor = x;
                _container.style.borderBottomColor = x;
                _container.style.borderLeftColor = x;
            });

            viewModel.Icon.Bind(x => _icon.style.backgroundImage = new StyleBackground(x));
            viewModel.Opacity.Bind(x => _container.style.opacity = x);
        }
    }
}
