using System.Collections.Generic;
using Tanks.Core.Lobbies;
using Tanks.UI.Menus.Lobbies.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Lobbies
{
    public sealed class LobbyView : ViewBase<LobbyViewModel>
    {
        private VisualElement _root;
        private ScrollView _playerList;
        private TextField _lobbyName;
        private Label _memberCount;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root;

            _playerList = _root.Q<ScrollView>("PlayerListContent");
            _lobbyName = _root.Q<TextField>("LobbyNameField");
            _memberCount = _root.Q<Label>("PlayerCount");

            Button leaveButton = _root.Q<Button>("LeaveButton");
            Button startButton = _root.Q<Button>("StartButton");

            leaveButton.clicked += LeaveLobby;
            startButton.clicked += StartGame;

            _lobbyName.RegisterValueChangedCallback(OnLobbyRenamed);
        }

        protected override void Bind(LobbyViewModel viewModel)
        {
            viewModel.LobbyName.Bind(_lobbyName.SetValueWithoutNotify);
            viewModel.MaxMemberCount.Bind(UpdateMemberCount);
            viewModel.Players.Bind(OnPlayersUpdated);
        }

        protected override void Unbind(LobbyViewModel viewModel)
        {
            viewModel.LobbyName.Unbind(_lobbyName.SetValueWithoutNotify);
            viewModel.MaxMemberCount.Unbind(UpdateMemberCount);
            viewModel.Players.Unbind(OnPlayersUpdated);
        }

        private void OnPlayersUpdated(IDictionary<string, LobbyPlayerState> players)
        {
            _playerList.Clear();
            
            foreach (LobbyPlayerState player in players.Values)
            {
                LobbyPlayer playerView = new LobbyPlayer
                {
                    Username = player.DisplayName
                };

                _playerList.Add(playerView);
            }
        }

        private void OnLobbyRenamed(ChangeEvent<string> e)
        {
            if (ViewModel.TryRenameLobby(e.newValue)) return;

            // Reset
            _lobbyName.SetValueWithoutNotify(e.previousValue);
        }

        private void LeaveLobby() => ViewModel.LeaveLobbyConfirmation();
        private void StartGame() => ViewModel.StartGame();

        private void UpdateMemberCount(int max)
        {
            int current = ViewModel.Players.Value.Count;
            _memberCount.text = $"{current} / {max}";
        }
    }
}
