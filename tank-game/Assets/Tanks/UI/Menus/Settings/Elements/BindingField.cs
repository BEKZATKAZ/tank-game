using System;
using Tanks.Client.Input.Glyphs;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Settings.Elements
{
    public sealed class BindingField : VisualElement
    {
        public BindingField(
            string slot,
            Action<string> capture)
        {
            Slot = slot;
            _button = new Button(() => capture(slot));
            _image = new Image { pickingMode = PickingMode.Ignore };
            _label = new Label { pickingMode = PickingMode.Ignore };

            AddToClassList("binding-field");
            _button.AddToClassList("binding-field__capture");
            _image.AddToClassList("binding-field__glyph");
            _label.AddToClassList("binding-field__name");
            _button.Add(_image);
            _button.Add(_label);
            Add(_button);
        }

        public string Slot { get; }
        private readonly Button _button;
        private readonly Image _image;
        private readonly Label _label;

        public void Refresh(InputGlyphDatabase database, string path, bool capturing)
        {
            Sprite sprite = InputGlyphResolver.Resolve(database, path);

            EnableInClassList("binding-field--capturing", capturing);
            _image.sprite = sprite;
            _image.style.display = sprite != null
                ? DisplayStyle.Flex : DisplayStyle.None;
            _label.style.display = sprite == null
                ? DisplayStyle.Flex : DisplayStyle.None;
            _label.SetBinding("text", BindingLabels.Display(path));
            _button.SetBinding("tooltip", BindingLabels.Display(path));
        }
    }
}
