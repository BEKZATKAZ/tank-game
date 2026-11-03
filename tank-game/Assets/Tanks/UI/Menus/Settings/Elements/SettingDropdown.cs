using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Settings.Elements
{
    [UxmlElement]
    public partial class SettingDropdown : SettingElementBase
    {
        public SettingDropdown()
        {
            _label = new Label() { pickingMode = PickingMode.Ignore };
            _label.AddToClassList("setting__label");

            _valueLabel = new Label() { pickingMode = PickingMode.Ignore };
            _valueLabel.AddToClassList("setting-dropdown__value");

            Button previous = new Button(PreviousValue) { text = "<" };
            previous.AddToClassList("setting-dropdown__previous-button");

            Button next = new Button(NextValue) { text = ">" };
            next.AddToClassList("setting-dropdown__next-button");

            Add(_label);
            Add(previous);
            Add(_valueLabel);
            Add(next);

            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        [UxmlAttribute]
        public List<string> ChoiceKeys
        {
            get => _choiceKeys;
            set
            {
                _choiceKeys = value ?? new List<string>();
                NotifyPropertyChanged(nameof(ChoiceKeys));
                OnValueChanged(Value);
            }
        }

        [UxmlAttribute]
        [CreateProperty]
        public bool LocalizeChoices
        {
            get => _localizeChoices;
            set
            {
                _localizeChoices = value;
                NotifyPropertyChanged(nameof(LocalizeChoices));
                OnValueChanged(Value);
            }
        }

        private readonly Label _label;
        private readonly Label _valueLabel;
        private List<string> _choiceKeys = new List<string>();
        private bool _localizeChoices;

        public override void OnDestroyed()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        protected override async void OnValueChanged(int newValue)
        {
            if (newValue < 0 || newValue >= _choiceKeys.Count) return;

            base.OnValueChanged(newValue);

            string choice = _choiceKeys[newValue];
            string text = null;

            if (LocalizeChoices)
            {
                try
                {
                    text = await LocalizationSettings.StringDatabase
                        .GetLocalizedStringAsync("Menus", $"menu.settings.{choice}").Task;
                }
                catch (Exception) { }
            }

            _valueLabel.text = text ?? choice;
        }

        protected override void OnLabelChanged(string newLabel)
        {
            _label.text = newLabel;
            base.OnLabelChanged(newLabel);
        }

        private void PreviousValue()
        {
            if (Value <= 0) Value = _choiceKeys.Count - 1;
            else Value--;
        }

        private void NextValue()
        {
            if (Value >= _choiceKeys.Count - 1) Value = 0;
            else Value++;
        }

        private void OnLocaleChanged(Locale locale)
        {
            if (panel is null) return;
            OnValueChanged(Value);
        }
    }
}
