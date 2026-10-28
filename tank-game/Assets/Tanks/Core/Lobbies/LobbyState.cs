using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Core.Lobbies.Sessions.DTOs;
using Tanks.Utils;

namespace Tanks.Core.Lobbies
{
    public struct LobbyPlayerState
    {
        public readonly string DisplayName
        {
            get => Data.TryGetValue("username", out string username) ? username : UserId;
        }

        public readonly bool IsReady => Data != null &&
            Data.TryGetValue("ready", out string ready) && bool.TryParse(ready, out bool value) && value;

        public string UserId;
        public IDictionary<string, string> Data;
    }

    public enum LobbyStatus
    {
        WaitingForPlayers,
        PreparingServer,
        Connecting
    };

    public sealed class LobbyState
    {
        public LobbyState()
        {
            Players = new(new Dictionary<string, LobbyPlayerState>());
            LobbyData = new Dictionary<string, string>();
        }

        public BindableProperty<LobbyId> Id { get; } = new();
        public BindableProperty<LobbyAddress> Address { get; } = new();

        public BindableProperty<string> HostId { get; } = new();
        public BindableProperty<string> Name { get; } = new();
        public BindableProperty<int> MaxPlayerCount { get; } = new();

        public BindableProperty<IDictionary<string, LobbyPlayerState>> Players { get; }
        public IDictionary<string, string> LobbyData { get; }

        public BindableProperty<string> JoinCode { get; } = new();
        public BindableProperty<LobbyStatus> Status { get; } = new();
        public BindableProperty<SessionSettingsFromLobby> Settings { get; } = new();
        public BindableProperty<bool> IsUpdatingSettings { get; } = new();
        public SemaphoreSlim SettingsLock { get; } = new(1, 1);
        public BindableProperty<SessionSettings> ServerConfirmedSettings { get; } = new();

        public void Refresh()
        {
            LobbyData.TryGetValue("join-code", out string code);
            LobbyData.TryGetValue("status", out string statusString);
            LobbyData.TryGetValue("settings", out string settingsJson);
            LobbyData.TryGetValue("server-confirmed-settings", out string serverSettingsJson);

            int.TryParse(statusString, out int status);

            JoinCode.Value = code;
            Status.Value = (LobbyStatus)status;

            TryDeserializeFromJson(Settings, settingsJson);
            TryDeserializeFromJson(ServerConfirmedSettings, serverSettingsJson);
        }

        private void TryDeserializeFromJson<T>(BindableProperty<T> destination, string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;

            try
            {
                destination.Value = JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {

            }
        }
    }
}
