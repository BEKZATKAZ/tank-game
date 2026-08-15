using System.Collections.Generic;
using Tanks.Utils;

namespace Tanks.Core.Lobbies
{
    public struct LobbyPlayerState
    {
        public string UserId;
        public IDictionary<string, string> Data;
    }

    public sealed class LobbyState
    {
        public BindableProperty<LobbyId> Id { get; } = new();
        public BindableProperty<LobbyAddress> Address { get; } = new();

        public BindableProperty<bool> IsLocalPlayerOwner { get; } = new();
        public BindableProperty<string> OwnerId { get; } = new();
        public BindableProperty<string> Name { get; } = new();

        public BindableProperty<int> MaxPlayerCount { get; } = new();
        public BindableProperty<IDictionary<string, LobbyPlayerState>> Players { get; } = new();
    }
}
