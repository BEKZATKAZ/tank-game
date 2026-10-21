using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
            _dropdown = new DropdownField(Label);
            ChoiceKeys = new List<string>();
            Value = 0;

            Add(_dropdown);
            _dropdown.RegisterValueChangedCallback(x => Value = _dropdown.index);

            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        [UxmlAttribute]
        public List<string> ChoiceKeys
        {
            get => _choiceKeys;
            set
            {
                _choiceKeys = value ?? new List<string>();
                RefreshChoicesAsync(_choiceKeys.ToList()).Forget();
                NotifyPropertyChanged(nameof(ChoiceKeys));
            }
        }

        private readonly DropdownField _dropdown;
        private List<string> _choiceKeys = new List<string>();
        private int _refreshVersion;

        private async UniTask RefreshChoicesAsync(List<string> keys)
        {
            int version = ++_refreshVersion;
            var choices = new List<string>(keys.Count);

            try
            {
                await LocalizationSettings.InitializationOperation.Task;

                foreach (string key in keys)
                {
                    var handle = LocalizationSettings.StringDatabase
                        .GetLocalizedStringAsync("Menus", key);

                    string text;

                    try { text = await handle.Task; }
                    finally { Addressables.Release(handle); }

                    choices.Add(string.IsNullOrWhiteSpace(text) ? key : text);
                }

                // Ignore an outdated request if the locale or keys changed.
                if (version != _refreshVersion) return;

                int previousIndex = _dropdown.index;
                _dropdown.choices = choices;

                if (previousIndex >= 0 && previousIndex < choices.Count)
                {
                    _dropdown.index = previousIndex;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public override void OnDestroyed()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            _refreshVersion++;
        }

        protected override void OnLabelChanged(string newLabel)
        {
            _dropdown.label = newLabel;
            base.OnLabelChanged(newLabel);
        }

        protected override void OnValueChanged(int newValue)
        {
            if (newValue >= 0 && newValue < _dropdown.choices.Count)
            {
                _dropdown.index = newValue;
            }

            base.OnValueChanged(newValue);
        }

        private void OnLocaleChanged(Locale locale)
        {
            RefreshChoicesAsync(_choiceKeys.ToList()).Forget();
        }
    }
}
