using Tanks.UI.Elements;
using Tanks.UI.Items;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Items
{
    public sealed class ItemBox : VisualElement, IMenuHighlightTarget
    {
        public ItemBox(ItemDescriptionData item, System.Action<bool> onHighlighted)
        {
            _item = item;
            _onHighlighted = onHighlighted;
            focusable = true;
            userData = item.Config.Identifier;
            AddToClassList("item-browser__box");

            _fill = new VisualElement { pickingMode = PickingMode.Ignore };
            _fill.style.opacity = 0;
            _fill.AddToClassList("bg-fill");

            Add(_fill);

            _fill.SetSpriteColors(
                item.ClientConfig.AccentColor, null, revealWhenReady: true);

            VisualElement icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("item-browser__icon");
            icon.style.backgroundImage = new StyleBackground(item.ClientConfig.Icon);
            Add(icon);
        }

        private readonly ItemDescriptionData _item;
        private readonly System.Action<bool> _onHighlighted;
        private readonly VisualElement _fill;

        public void SetHighlighted(bool highlighted)
        {
            _fill.SetSpriteBorderColor(highlighted ? Color.white : _item.ClientConfig.AccentColor);
            _onHighlighted(highlighted);
        }
    }
}
