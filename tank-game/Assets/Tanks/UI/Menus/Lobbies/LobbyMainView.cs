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
        private Button _startButton;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("Main");
            _playerList = root.Q<ScrollView>();

            Button leaveButton = root.Q<Button>("LeaveButton");
            _startButton = root.Q<Button>("StartButton");

            leaveButton.clicked += LeaveLobby;
            _startButton.clicked += StartGame;
        }

        protected override void Bind(LobbyMainViewModel viewModel)
        {
            viewModel.Players.Bind(OnPlayersUpdated);
            viewModel.CanStartGame.Bind(x => _startButton.enabledSelf = x);
            viewModel.IsHost.Bind(x => _startButton.style.display = x ? DisplayStyle.Flex : 0);
        }

        protected override void Unbind(LobbyMainViewModel viewModel)
        {
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

        private void LeaveLobby() => ViewModel.LeaveLobbyConfirmation();
        private void StartGame() => ViewModel.StartGameAsync().Forget();
    }
}
