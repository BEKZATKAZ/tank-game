using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Lobbies
{
    public sealed class LobbySettingsView : ViewBase<LobbySettingsViewModel>
    {
        private VisualElement _root;
        private readonly Dictionary<LobbySetting, DropdownField> _fields = new();
        private bool _refreshing;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("LobbySettings");
            ScrollView content = _root.Q<ScrollView>();

            foreach (LobbySetting type in System.Enum.GetValues(typeof(LobbySetting)))
            {
                var row = new VisualElement();
                row.AddToClassList("setting");
                var dropdown = new DropdownField { name = type.ToString() };
                dropdown.RegisterValueChangedCallback(_ =>
                {
                    if (_refreshing == false && ViewModel != null)
                    {
                        ViewModel.SetValueAsync(type, dropdown.index).Forget();
                    }
                });
                row.Add(dropdown);
                content.Add(row);
                _fields.Add(type, dropdown);
            }
        }

        protected override void Bind(LobbySettingsViewModel viewModel)
        {
            viewModel.Settings.Bind(OnSettingsChanged);
            viewModel.CanEdit.Bind(OnCanEditChanged);
        }

        protected override void Unbind(LobbySettingsViewModel viewModel)
        {
            viewModel.Settings.Unbind(OnSettingsChanged);
            viewModel.CanEdit.Unbind(OnCanEditChanged);
        }

        private void OnSettingsChanged(IReadOnlyList<LobbySettingOption> settings)
        {
            _refreshing = true;

            try
            {
                foreach (LobbySettingOption setting in settings)
                {
                    DropdownField field = _fields[setting.Type];
                    field.label = setting.Label;
                    field.choices = setting.Choices;
                    field.SetValueWithoutNotify(setting.Choices[setting.SelectedIndex]);
                }
            }
            finally { _refreshing = false; }
        }

        private void OnCanEditChanged(bool canEdit)
        {
            foreach (DropdownField field in _fields.Values) field.SetEnabled(canEdit);
        }
    }
}
