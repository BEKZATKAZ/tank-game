using Cysharp.Threading.Tasks;
using Tanks.Client.Effects;
using Tanks.UI.Elements;
using Tanks.UI.Items;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Items
{
    public sealed class ItemBrowserView : ViewBase<ItemBrowserViewModel>
    {
        [SerializeField] private EffectListConfig _effectsConfig;

        private VisualElement _root;
        private VisualElement _grid;
        private ScrollView _scroll;
        private Button _back;
        private VisualElement _details;
        private VisualElement _fill;
        private VisualElement _icon;
        private Label _name;
        private Label _description;
        private Label _tier;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("ItemBrowserSection");
            _root.AddManipulator(new MenuHighlightManipulator());
            _grid = _root.Q("ItemGrid");
            _scroll = _grid.GetFirstAncestorOfType<ScrollView>();
            _scroll.focusable = false;
            _details = _root.Q("ItemDetails");
            _fill = _details.Q("Fill");
            _icon = _details.Q("ItemIcon");
            _name = _details.Q<Label>("ItemName");
            _description = _details.Q<Label>("ItemDescription");
            _tier = _details.Q<Label>("ItemTier");
            _back = _root.Q<Button>("BackButton");
            _back.clicked += () => ViewModel.Back();
            _root.AddManipulator(new ItemBrowserNavigationManipulator(_grid, _back));
        }

        protected override void Bind(ItemBrowserViewModel viewModel)
        {
            viewModel.Items.Bind(OnItemsChanged);
            viewModel.HoveredItem.Bind(OnHoveredItemChanged);
            viewModel.InitializeAsync(_effectsConfig).Forget();
        }

        protected override void Unbind(ItemBrowserViewModel viewModel)
        {
            viewModel.Items.Unbind(OnItemsChanged);
            viewModel.HoveredItem.Unbind(OnHoveredItemChanged);
            _grid.Clear();
            _details.style.visibility = Visibility.Hidden;
        }

        private void OnItemsChanged(ItemDescriptionData[] items)
        {
            var focused = _grid.focusController?.focusedElement as VisualElement;
            object focusedId = focused != null && _grid.Contains(focused) ? focused.userData : null;
            _grid.Clear();

            foreach (ItemDescriptionData item in items)
            {
                ItemBox box = new ItemBox(item, highlighted =>
                {
                    if (highlighted) ViewModel?.Hover(item.Config.Identifier);
                    else ViewModel?.Leave(item.Config.Identifier);
                });
                box.RegisterCallback<FocusInEvent>(_ => _scroll.ScrollTo(box));
                _grid.Add(box);

                if (item.Config.Identifier.Equals(focusedId)) box.schedule.Execute(() => box.Focus());
            }
        }

        private void OnHoveredItemChanged(ItemDescriptionData item)
        {
            _details.style.visibility = item is null ? Visibility.Hidden : Visibility.Visible;
            if (item is null) return;

            _name.text = item.Name;
            _description.text = item.Description;
            _tier.text = item.TierName;
            _tier.style.color = item.ClientConfig.AccentColor;
            _fill.SetSpriteBorderColor(item.ClientConfig.AccentColor);
            _icon.style.backgroundImage = new StyleBackground(item.ClientConfig.Icon);
        }
    }
}
