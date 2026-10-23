using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Tanks.Core.Lobbies;
using Tanks.UI.Menus.Lobbies.Elements;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Lobbies
{
    public sealed class LobbyMainView : ViewBase<LobbyMainViewModel>
    {
        private VisualElement _root;
        private ScrollView _playerList;
        private Button _readyButton;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("Main");
            _playerList = root.Q<ScrollView>();

            Button leaveButton = root.Q<Button>("LeaveButton");
            _readyButton = root.Q<Button>("ReadyButton");

            leaveButton.clicked += LeaveLobby;
            _readyButton.clicked += ToggleReady;
        }

        protected override void Bind(LobbyMainViewModel viewModel)
        {
            viewModel.Players.Bind(OnPlayersUpdated);
            viewModel.CanToggleReady.Bind(OnCanToggleReadyChanged);
            viewModel.ReadyButtonText.Bind(OnReadyButtonTextChanged);
            viewModel.ReadyText.Bind(OnReadinessTextChanged);
            viewModel.NotReadyText.Bind(OnReadinessTextChanged);
        }

        protected override void Unbind(LobbyMainViewModel viewModel)
        {
            viewModel.Players.Unbind(OnPlayersUpdated);
            viewModel.CanToggleReady.Unbind(OnCanToggleReadyChanged);
            viewModel.ReadyButtonText.Unbind(OnReadyButtonTextChanged);
            viewModel.ReadyText.Unbind(OnReadinessTextChanged);
            viewModel.NotReadyText.Unbind(OnReadinessTextChanged);
        }

        private void OnPlayersUpdated(IDictionary<string, LobbyPlayerState> players)
        {
            _playerList.Clear();
            
            foreach (LobbyPlayerState player in players.Values)
            {
                LobbyPlayer playerView = new LobbyPlayer
                {
                    Username = player.DisplayName,
                    Readiness = player.IsReady
                        ? ViewModel.ReadyText.Value
                        : ViewModel.NotReadyText.Value
                };

                _playerList.Add(playerView);
            }
        }

        private void LeaveLobby() => ViewModel.LeaveLobbyConfirmation();
        private void OnCanToggleReadyChanged(bool value) => _readyButton.SetEnabled(value);
        private void OnReadyButtonTextChanged(string value) => _readyButton.text = value;
        private void OnReadinessTextChanged(string value) => OnPlayersUpdated(ViewModel.Players.Value);
        private void ToggleReady() => ViewModel.ToggleReadyAsync().Forget();
    }
}
