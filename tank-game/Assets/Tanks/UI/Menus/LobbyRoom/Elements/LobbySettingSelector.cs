using System;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.LobbyRoom.Elements
{
    public sealed class LobbySettingSelector : VisualElement
    {
        public LobbySettingSelector(LobbySetting type)
        {
            name = type.ToString();
            AddToClassList("setting");
            AddToClassList("lobby-room__setting");

            _label = new Label { pickingMode = PickingMode.Ignore };
            _label.AddToClassList("setting__label");

            _value = new Label { pickingMode = PickingMode.Ignore };
            _value.AddToClassList("setting-dropdown__value");

            var previous = new Button(() => Select(-1)) { text = "<", name = "Previous" };
            previous.AddToClassList("setting-dropdown__previous-button");
            var next = new Button(() => Select(1)) { text = ">", name = "Next" };
            next.AddToClassList("setting-dropdown__next-button");

            Add(_label);
            Add(previous);
            Add(_value);
            Add(next);
        }

        public event Action<int> SelectionChanged;

        private readonly Label _label;
        private readonly Label _value;
        private int _selectedIndex;
        private int _choiceCount;

        public void SetOption(LobbySettingOption option)
        {
            _label.text = option.Label;
            _label.tooltip = option.Label;
            _selectedIndex = option.SelectedIndex;
            _choiceCount = option.Choices.Count;
            _value.text = _selectedIndex >= 0 && _selectedIndex < _choiceCount
                ? option.Choices[_selectedIndex] : string.Empty;
        }

        private void Select(int direction)
        {
            if (enabledInHierarchy == false || _choiceCount == 0) return;

            int index = (_selectedIndex + direction + _choiceCount) % _choiceCount;
            SelectionChanged?.Invoke(index);
        }
    }
}
