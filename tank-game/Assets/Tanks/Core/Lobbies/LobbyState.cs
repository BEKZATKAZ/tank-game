using System.Collections.Generic;
using Tanks.Utils;

namespace Tanks.Core.Lobbies
{
    public struct LobbyPlayerState
    {
        public readonly string DisplayName
        {
            get => Data.TryGetValue("username", out string username) ? username : UserId;
        }

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

        public BindableProperty<string> SessionIpAddress { get; } = new("127.0.0.1");
        public BindableProperty<ushort> SessionPort { get; } = new(7777);

        public BindableProperty<int> MaxPlayerCount { get; } = new();
        public BindableProperty<IDictionary<string, LobbyPlayerState>> Players { get; } = new();
    }
}
