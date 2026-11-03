using System.Collections.Generic;
using Tanks.Core;
using Tanks.Core.Lobbies;
using Tanks.UI.TankModel;
using UnityEngine;
using VContainer;

namespace Tanks.UI.Menus.LobbyRoom
{
    public sealed class LobbyTankSpawner : MonoBehaviour
    {
        [Inject] private readonly ITankModelService _tankModelService;
        [Inject] private readonly ILobbyService _lobbyService;
        [Inject] private readonly IAuthService _auth;

        private readonly Dictionary<string, int> _tankIndices = new();
        private readonly Stack<int> _availableIndices = new();
        private readonly List<string> _removed = new();
        private bool _isInLobby;

        private void Start()
        {
            _auth.CurrentProfile.DisplayName.Bind(OnDisplayNameChanged);
            _lobbyService.CurrentLobbyState.Players.Bind(OnPlayersChanged);
        }

        private void OnDestroy()
        {
            _auth.CurrentProfile.DisplayName.Unbind(OnDisplayNameChanged);
            _lobbyService.CurrentLobbyState.Players.Unbind(OnPlayersChanged);
        }

        private void OnDisplayNameChanged(string displayName)
        {
            _tankModelService.SetDisplayName(0, displayName);
        }

        private void OnPlayersChanged(IDictionary<string, LobbyPlayerState> players)
        {
            if (players is not { Count: > 0 })
            {
                if (_isInLobby == false) return;

                _isInLobby = false;
                _tankModelService.Return(0);
                OnDisplayNameChanged(_auth.CurrentProfile.DisplayName.Value);

                foreach (int index in _tankIndices.Values)
                {
                    _tankModelService.Return(index);
                }

                _tankIndices.Clear();
                _availableIndices.Clear();
                return;
            }

            bool instant = _isInLobby == false;

            if (instant)
            {
                _isInLobby = true;
                for (int i = _tankModelService.Models.Count - 1; i >= 1; i--) _availableIndices.Push(i);
            }

            _removed.Clear();
            foreach (string userId in _tankIndices.Keys)
            {
                if (players.ContainsKey(userId) == false) _removed.Add(userId);
            }

            foreach (string userId in _removed)
            {
                int index = _tankIndices[userId];
                _tankIndices.Remove(userId);
                _availableIndices.Push(index);
                _tankModelService.Return(index);
            }

            foreach (LobbyPlayerState player in players.Values)
            {
                if (player.UserId == _auth.CurrentProfile.UserId.Value)
                {
                    _tankModelService.SetDisplayName(0, player.DisplayName);
                    _tankModelService.SetReady(0, player.IsReady);
                    continue;
                }

                if (_tankIndices.TryGetValue(player.UserId, out int index) == false)
                {
                    if (_availableIndices.Count == 0) continue;
                    index = _availableIndices.Pop();

                    _tankIndices.Add(player.UserId, index);
                    _tankModelService.DriveByCurve(index, index, instant);
                }

                _tankModelService.SetDisplayName(index, player.DisplayName);
                _tankModelService.SetReady(index, player.IsReady);
            }
        }
    }
}
