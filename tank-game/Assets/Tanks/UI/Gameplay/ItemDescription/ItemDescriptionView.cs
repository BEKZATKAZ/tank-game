using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay.ItemDescription
{
    public sealed class ItemDescriptionView : IView<ItemDescriptionModel>
    {
        private VisualElement _container;
        private VisualElement _icon;
        private Label _name;
        private Label _description;
        private Label _tier;

        public void Initialize(VisualElement root)
        {
            _container = root.Q("ItemDescriptionContainer");
            _container.style.opacity = 0;

            _icon = root.Q("ItemIcon");
            _name = _container.Q<Label>("ItemName");
            _description = _container.Q<Label>("ItemDescription");
            _tier = _container.Q<Label>("ItemTier");
        }

        public void Hide()
        {
            _container.style.opacity = 0;
        }

        public void Show(ItemDescriptionModel data)
        {
            _icon.style.backgroundImage = new StyleBackground(data.Icon);
            _name.text = data.Name;
            _description.text = data.Description;

            _tier.text = data.Tier;
            _tier.style.color = data.AccentColor;

            _container.style.opacity = 1;
        }
    }
}
