using System.Collections.Generic;
using System.Linq;
using Tanks.Shared.Teams;
using UnityEngine.UIElements;
using VContainer;

namespace Tanks.UI.Gameplay.GameStats
{
    public sealed class GameStatsView : ViewBase<GameStatsViewModel>
    {
        [Inject] private readonly ITeamBehaviourService _teamService;

        private VisualElement _root;
        private Label _header;
        private ScrollView _playerList;
        private PlayerState[] _lastPlayerList;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("GameStats");
            _header = _root.Q<Label>("GameStatsHeader");
            _playerList = _root.Q<ScrollView>();
        }

        protected override void Bind(GameStatsViewModel viewModel)
        {
            viewModel.Header.Bind(x => _header.text = x);
            viewModel.Players.Bind(RefreshPlayers);
        }

        protected override void Unbind(GameStatsViewModel viewModel)
        {
            ClearLastPlayerList();
        }

        private void ClearLastPlayerList()
        {
            if (_lastPlayerList is null) return;

            foreach (PlayerState player in _lastPlayerList)
            {
                player.Updated -= OnPlayerStateModified;
            }

            _lastPlayerList = null;
        }

        private void OnPlayerStateModified(PlayerState _)
        {
            RefreshPlayers(ViewModel.Players.Value);
        }

        private void RefreshPlayers(List<PlayerState> players)
        {
            ClearLastPlayerList();

            _lastPlayerList = players
                .OrderByDescending(x => _teamService.GetTeam(x.BelongsTo).Score.Value)
                .ToArray();

            _playerList.Clear();

            foreach (PlayerState player in _lastPlayerList)
            {
                PlayerElement element = new PlayerElement();
                element.Set(player, _teamService.GetTeam(player.BelongsTo));

                _playerList.Add(element);
                player.Updated += OnPlayerStateModified;
            }
        }
    }
}
