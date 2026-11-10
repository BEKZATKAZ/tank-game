using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Store
{
    public sealed class CosmeticCard : VisualElement
    {
        public CosmeticCard(
            string identifier,
            Action<string, string> purchase,
            Action<string> equip,
            Action<string> preview)
        {
            AddToClassList("cosmetic-card");
            userData = identifier;
            focusable = true;
            viewDataKey = identifier + ":preview";
            VisualElement fill = new() { pickingMode = PickingMode.Ignore };
            fill.style.opacity = 0;
            fill.AddToClassList("bg-fill");
            Add(fill);
            this.AddManipulator(new CosmeticCardHighlightManipulator(fill));
            _title = new Label { enableRichText = false };
            _title.AddToClassList("cosmetic-title");
            Add(_title);
            _preview = new CosmeticPreviewElement();
            Add(_preview);
            _discount = new Label { name = "Discount", pickingMode = PickingMode.Ignore };
            Add(_discount);
            _actions = new CosmeticCardActions(identifier, purchase, equip);
            Add(_actions);

            RegisterCallback<ClickEvent>(value =>
            {
                VisualElement element = value.target as VisualElement;
                if (element is Button || element?.GetFirstAncestorOfType<Button>() !=
                    null) return;

                preview(identifier);
            });

            RegisterCallback<NavigationSubmitEvent>(value =>
            {
                if (value.target != this) return;
                value.StopPropagation();
                preview(identifier);
            });

            foreach (VisualElement child in Children()) _content.Add(child);
            SetContentVisible(false);
        }

        private readonly Label _title;
        private readonly Label _discount;
        private readonly CosmeticPreviewElement _preview;
        private readonly CosmeticCardActions _actions;
        private readonly List<VisualElement> _content = new();
        private bool _contentVisible = true;
        private CosmeticCardData _data;
        private int _revision = -1;

        public void Bind(CosmeticCardData item)
        {
            if (ReferenceEquals(_data, item) && _revision == item.Revision) return;
            _data = item;
            _revision = item.Revision;
            _title.text = item.Title;
            _discount.text = $"-{item.PercentOff}%";
            _discount.style.display = item.PercentOff > 0 && item.IsOwned == false
                ? DisplayStyle.Flex : DisplayStyle.None;

            _preview.SetSprites(item.Body ? item.Body.Sprite : null,
                item.Turret ? item.Turret.Sprite : null);

            _actions.Bind(item);
        }

        public VisualElement FindFocus(string key)
        {
            SetContentVisible(true);
            return key == viewDataKey ? this : _actions.FindFocus(key) ?? this;
        }

        public void SetContentVisible(bool visible)
        {
            if (_contentVisible == visible) return;
            _contentVisible = visible;

            foreach (VisualElement child in _content)
            {
                if (visible) Add(child);
                if (visible == false) child.RemoveFromHierarchy();
            }
        }

        public void Release()
        {
            _data = null;
            _revision = -1;
        }
    }
}
