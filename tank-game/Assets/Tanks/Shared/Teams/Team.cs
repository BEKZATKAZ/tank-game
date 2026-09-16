using System;
using System.Collections.Generic;
using Tanks.Utils;

namespace Tanks.Shared.Teams
{
    public class Team
    {
        public Team(TeamId id) => Id = id;

        public event Action<PlayerState> PlayerJoined;
        public event Action<PlayerState> PlayerLeft;

        public TeamId Id { get; }
        public IReadOnlyBindableProperty<int> Score => _score;
        public IEnumerable<PlayerState> Players => _players.Values;

        private readonly Dictionary<ulong, PlayerState> _players = new();
        private readonly BindableProperty<int> _score = new();

        public PlayerState GetPlayer(ulong ownerClientId)
        {
            return _players[ownerClientId];
        }

        public PlayerState AddPlayer(ulong ownerClientId, string displayName)
        {
            PlayerState state = new PlayerState(ownerClientId, displayName, Id);
            _players.Add(ownerClientId, state);

            PlayerJoined?.Invoke(state);
            return state;
        }

        public void RemovePlayer(ulong ownerClientId)
        {
            if (_players.Remove(ownerClientId, out PlayerState state) == false) return;
            PlayerLeft?.Invoke(state);
        }

        public void AddScore(int amount)
        {
            _score.Value += amount;
        }
    }
}
