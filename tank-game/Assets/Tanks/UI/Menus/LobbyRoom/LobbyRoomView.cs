using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Tanks.Core.Lobbies;
using Tanks.UI.Menus.LobbyRoom.Elements;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.LobbyRoom
{
    [UnityEngine.RequireComponent(typeof(LobbyChatView))]
    public sealed class LobbyRoomView : ViewBase<LobbyRoomViewModel>
    {
        private readonly Dictionary<LobbySetting, LobbySettingSelector> _settings = new();
        private VisualElement _root;
        private Button _readyButton;
        private Label _lobbyName;
        private Label _lobbyCode;
        private Label _status;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("LobbyRoomSection");
            _root.AddManipulator(new MenuHighlightManipulator());

            _lobbyName = _root.Q<Label>("LobbyName");
            _lobbyCode = _root.Q<Label>("LobbyCode");
            _status = _root.Q<Label>("LobbyStatus");

            Button leaveButton = _root.Q<Button>("LeaveButton");
            _readyButton = _root.Q<Button>("ReadyButton");

            leaveButton.clicked += () => ViewModel.LeaveLobbyConfirmation();
            _readyButton.clicked += () => ViewModel.ToggleReadyAsync().Forget();

            ScrollView settings = _root.Q<ScrollView>("LobbySettings");
            settings.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            foreach (LobbySetting type in System.Enum.GetValues(typeof(LobbySetting)))
            {
                LobbySettingSelector selector = new LobbySettingSelector(type);

                selector.SelectionChanged += index =>
                {
                    ViewModel?.Settings.SetValueAsync(type, index).Forget();
                };

                settings.Add(selector);
                _settings.Add(type, selector);
            }
        }

        protected override void Bind(LobbyRoomViewModel viewModel)
        {
            GetComponent<LobbyChatView>().Show();

            viewModel.Name.Bind(OnNameChanged);
            viewModel.Address.Bind(OnAddressChanged);

            viewModel.Settings.Settings.Bind(OnSettingsChanged);
            viewModel.Settings.CanEdit.Bind(OnCanEditChanged);

            viewModel.CanReady.Bind(_readyButton.SetEnabled);
            viewModel.ReadyText.Bind(OnReadyTextChanged);
            viewModel.Status.Bind(OnStatusChanged);
        }

        protected override void Unbind(LobbyRoomViewModel viewModel)
        {
            GetComponent<LobbyChatView>().Hide();

            viewModel.Name.Unbind(OnNameChanged);
            viewModel.Address.Unbind(OnAddressChanged);
            viewModel.Settings.Settings.Unbind(OnSettingsChanged);
            viewModel.Settings.CanEdit.Unbind(OnCanEditChanged);
            viewModel.CanReady.Unbind(_readyButton.SetEnabled);
            viewModel.ReadyText.Unbind(OnReadyTextChanged);
            viewModel.Status.Unbind(OnStatusChanged);
        }

        private void OnReadyTextChanged(string text) => _readyButton.text = text;
        private void OnStatusChanged(string text) => _status.text = text;

        private void OnNameChanged(string name) => _lobbyName.text = name;
        private void OnAddressChanged(LobbyAddress address) => _lobbyCode.text = address.Value;

        private void OnSettingsChanged(IReadOnlyList<LobbySettingOption> settings)
        {
            foreach (LobbySettingOption option in settings) _settings[option.Type].SetOption(option);
        }

        private void OnCanEditChanged(bool canEdit)
        {
            foreach (LobbySettingSelector selector in _settings.Values) selector.SetEnabled(canEdit);
        }
    }
}
