using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Tanks.Core;
using Tanks.Core.Players;
using Tanks.Core.Common;
using Tanks.Core.Lobbies;
using Tanks.Core.Lobbies.Payloads;
using Tanks.Core.Lobbies.Sessions;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

using static Unity.Services.Lobbies.Models.PlayerDataObject;

namespace Tanks.Infrastructure.Lobbies
{
    public sealed class UGSLobbyService : Core.Lobbies.ILobbyService, IDisposable
    {
        public UGSLobbyService(IAuthService auth, ISessionSettingsService settings, ILocalPlayerService player = null)
        {
            _auth = auth;
            _settings = settings;
            _player = player;
            _player?.LocalPlayer.Revision.Bind(OnLocalPlayerChanged);

            CurrentLobbyState.Id.Bind(OnLobbyIdChanged);
            Application.quitting += Dispose;
        }

        public LobbyState CurrentLobbyState { get; private set; } = new();

        public event Action<LobbyPlayerState> PlayerJoined;
        public event Action<LobbyPlayerState> PlayerLeft;
        public event Action<string> LobbyDeleted;

        private Unity.Services.Lobbies.ILobbyService Service => _service ?? LobbyService.Instance;

        private readonly ILocalPlayerService _player;
        private readonly IAuthService _auth;
        private readonly ISessionSettingsService _settings;
        private readonly Func<TimeSpan, CancellationToken, Task> _heartbeatDelay = Task.Delay;

        private Unity.Services.Lobbies.ILobbyService _service;
        private ILobbyEvents _lobbyEvents;
        private Lobby _lobby;

        private CancellationTokenSource _heartbeatCancellation;
        private Task _heartbeatTask = Task.CompletedTask;
        private string _heartbeatLobbyId;
        private bool _disposed;
        private int _generation;

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            Application.quitting -= Dispose;
            CurrentLobbyState.Id.Unbind(OnLobbyIdChanged);
            StopHeartbeat();
            _player?.LocalPlayer.Revision.Unbind(OnLocalPlayerChanged);
        }

        private void OnLobbyIdChanged(LobbyId id)
        {
            _generation++;
            if (id.Value != _heartbeatLobbyId) StopHeartbeat();
        }

        private async Task SubscribeToEventsAsync()
        {
            LobbyId id = CurrentLobbyState.Id.Value;
            if (id.Value is null) return;

            LobbyEventCallbacks callbacks = new LobbyEventCallbacks();
            callbacks.LobbyChanged += OnLobbyChanged;
            callbacks.LobbyDeleted += OnLobbyDeleted;

            _lobbyEvents = await Service.SubscribeToLobbyEventsAsync(id.Value, callbacks);
        }

        private async Task UnsubscribeFromEventsAsync()
        {
            if (_lobbyEvents is null) return;
            await _lobbyEvents.UnsubscribeAsync();
            _lobbyEvents = null;
        }

        private void OnLobbyDeleted()
        {
            if (_auth.CurrentProfile.UserId.Value != CurrentLobbyState.HostId.Value)
            {
                _ = UnsubscribeFromEventsAsync();
            }

            LobbyDeleted?.Invoke(CurrentLobbyState.HostId.Value);
            _lobby = null;

            CurrentLobbyState.Reset();
        }

        private void OnLobbyChanged(ILobbyChanges changes)
        {
            if (changes.LobbyDeleted)
            {
                OnLobbyDeleted();
                return;
            }

            if (_lobby == null) return;

            Dictionary<string, LobbyPlayerState> previous = new(CurrentLobbyState.Players.Value);
            changes.ApplyToLobby(_lobby);
            UGSLobbyMapper.MapLobby(_lobby, CurrentLobbyState);

            foreach (var player in CurrentLobbyState.Players.Value)
            {
                if (previous.ContainsKey(player.Key) == false) PlayerJoined?.Invoke(player.Value);
            }

            foreach (var player in previous)
            {
                if (CurrentLobbyState.Players.Value.ContainsKey(player.Key)) continue;
                PlayerLeft?.Invoke(player.Value);
            }
        }

        private async Task<Lobby> RememberAsync(Task<Lobby> request)
        {
            Lobby lobby = await request;

            if (_lobby == null || _lobby.Id != lobby.Id || lobby.Version >= _lobby.Version)
            {
                _lobby = lobby;
            }

            return _lobby;
        }

        public async Task<ResultType> SetReadyAsync(bool ready)
        {
            LobbyId lobbyId = CurrentLobbyState.Id.Value;
            string userId = _auth.CurrentProfile.UserId.Value;
            
            if (lobbyId.Value == null || CurrentLobbyState.Status.Value != LobbyStatus.WaitingForPlayers)
            {
                return Result.Failure("failed-to-update");
            }

            return await UGSLobbyMapper.MapAsync(() => RememberAsync(Service.UpdatePlayerAsync(
                lobbyId.Value, userId, new UpdatePlayerOptions
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        ["ready"] = new(VisibilityOptions.Member, ready.ToString())
                    }
                })), CurrentLobbyState, errorCode: "failed-to-update");
        }

        public async Task<ResultType> SendMessageAsync(string message)
        {
            LobbyId lobbyId = CurrentLobbyState.Id.Value;
            string userId = _auth.CurrentProfile.UserId.Value;
            int generation = _generation;

            if (_disposed || string.IsNullOrEmpty(lobbyId.Value) || string.IsNullOrEmpty(userId) ||
                CurrentLobbyState.Players.Value.ContainsKey(userId) == false ||
                string.IsNullOrWhiteSpace(message) || message.Length > LobbyChatMessage.MaxMessageLength)
            {
                return Result.Failure("failed-to-update");
            }

            string json = JsonConvert.SerializeObject(new LobbyChatMessage.Payload
            {
                Id = Guid.NewGuid().ToString("N"),
                Text = message
            });

            try
            {
                Lobby lobby = await Service.UpdatePlayerAsync(lobbyId.Value, userId, new UpdatePlayerOptions
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        ["LastMessage"] = new(VisibilityOptions.Member, json)
                    }
                });

                if (_disposed || generation != _generation || CurrentLobbyState.Id.Value.Value != lobbyId.Value)
                {
                    return Result.Failure("cancelled");
                }

                if (_lobby == null || lobby.Version >= _lobby.Version)
                {
                    _lobby = lobby;
                    UGSLobbyMapper.MapLobby(lobby, CurrentLobbyState);
                }

                return Result.Success();
            }
            catch (LobbyServiceException ex)
            {
                Debug.LogException(ex);
                return Result.Failure(UGSLobbyMapper.ReasonToErrorCode(ex.Reason));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return Result.Failure("failed-to-update");
            }
        }

        private async void OnLocalPlayerChanged(int revision)
        {
            string lobbyId = CurrentLobbyState.Id.Value.Value;
            if (_disposed || string.IsNullOrEmpty(lobbyId)) return;
            int generation = _generation;

            try
            {
                var updated = await Service.UpdatePlayerAsync(lobbyId, _auth.CurrentProfile.UserId.Value,
                    new UpdatePlayerOptions { Data = CreateAppearanceData() });

                if (_disposed || generation != _generation) return;

                if (_lobby == null || updated.Version >= _lobby.Version)
                {
                    _lobby = updated;
                    UGSLobbyMapper.MapLobby(updated, CurrentLobbyState);
                }
            }
            catch (Exception ex) { Debug.LogException(ex); }
        }

        private Dictionary<string, PlayerDataObject> CreateAppearanceData()
        {
            EquippedCosmetics equipped = _player?.LocalPlayer.Equipped.Value ?? EquippedCosmetics.Default;

            return new()
            {
                ["level"] = new(VisibilityOptions.Member, (_player?.LocalPlayer.Level.Value.Value ?? 0).ToString()),
                ["body"] = new(VisibilityOptions.Member, equipped.Body),
                ["turret"] = new(VisibilityOptions.Member, equipped.Turret)
            };
        }

        private Player CreatePlayerData()
        {
            PlayerProfileState profile = _auth.CurrentProfile;

            var data = CreateAppearanceData();
            data["username"] = new(VisibilityOptions.Member, profile.DisplayName.Value);
            data["ready"] = new(VisibilityOptions.Member, bool.FalseString);
            data["LastMessage"] = new(VisibilityOptions.Member, "");
            return new Player(id: profile.UserId.Value, data: data);

        }

        public Task<ResultType<IEnumerable<LobbySearchState>>> FindLobbiesAsync(string name = null)
        {
            return UGSLobbyMapper.MapAsync(() => Service.QueryLobbiesAsync(new QueryLobbiesOptions()
            {
                Filters = new List<QueryFilter>()
                {
                    new QueryFilter(
                        QueryFilter.FieldOptions.Name,
                        name?.Trim().ToLower() ?? "",
                        QueryFilter.OpOptions.CONTAINS)
                },
                Count = 10
            }));
        }

        public async Task<ResultType<LobbyState>> HostLobbyAsync(string name, uint maxPlayers)
        {
            var result = await UGSLobbyMapper.MapAsync(
                () => RememberAsync(Service.CreateLobbyAsync(name, (int)maxPlayers, new()
                {
                    Player = CreatePlayerData(),
                    Data = UGSLobbyMapper.MapDataToUpdate(new LobbyUpdateRequest
                    {
                        Settings = SessionSettingsMapper.FromDefaultToLobby(_settings.DefaultSettings)
                    })
                })), CurrentLobbyState, errorCode: "failed-to-create");

            if (result.IsSuccess == false) return result;

            StartHeartbeat();
            await SubscribeToEventsAsync();

            return result;
        }

        public async Task<ResultType<LobbyState>> JoinLobbyAsync(LobbyAddress address)
        {
            var result = await UGSLobbyMapper.MapAsync(
                () => RememberAsync(Service.JoinLobbyByCodeAsync(address.Value, new()
                {
                    Player = CreatePlayerData()
                })), CurrentLobbyState, errorCode: "failed-to-join");

            await SubscribeToEventsAsync();
            return result;
        }

        public async Task<ResultType<LobbyState>> JoinLobbyAsync(LobbyId id)
        {
            var result = await UGSLobbyMapper.MapAsync(
                () => RememberAsync(Service.JoinLobbyByIdAsync(id.Value, new()
                {
                    Player = CreatePlayerData()
                })), CurrentLobbyState, errorCode: "failed-to-join");

            await SubscribeToEventsAsync();
            return result;
        }

        public Task<ResultType<LobbyState>> UpdateLobbyAsync(LobbyUpdateRequest options)
        {
            LobbyId lobbyId = CurrentLobbyState.Id.Value;

            if (options.Settings.HasValue &&
                (string.IsNullOrEmpty(lobbyId.Value) ||
                CurrentLobbyState.HostId.Value != _auth.CurrentProfile.UserId.Value ||
                CurrentLobbyState.Status.Value != LobbyStatus.WaitingForPlayers))
            {
                return Task.FromResult(Result.Failure<LobbyState>("failed-to-update"));
            }

            return UGSLobbyMapper.MapAsync(() => RememberAsync(Service.UpdateLobbyAsync(lobbyId.Value, new()
            {
                Name = options.Name,
                Data = UGSLobbyMapper.MapDataToUpdate(options)
            })), CurrentLobbyState, errorCode: "failed-to-update");
        }

        public async Task<ResultType> LeaveLobbyAsync()
        {
            try
            {
                LobbyId lobbyId = CurrentLobbyState.Id.Value;
                string userId = _auth.CurrentProfile.UserId.Value;

                if (CurrentLobbyState.HostId.Value == userId)
                {
                    await Service.DeleteLobbyAsync(lobbyId.Value);
                }
                else
                {
                    await Service.RemovePlayerAsync(lobbyId.Value, userId);
                }

                StopHeartbeat();
                await UnsubscribeFromEventsAsync();

                _lobby = null;
                CurrentLobbyState.Reset();

                return Result.Success();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return Result.Failure("failed-to-leave");
            }
        }

        private void StartHeartbeat()
        {
            string id = CurrentLobbyState.Id.Value.Value;
            if (_heartbeatLobbyId == id && _heartbeatTask.IsCompleted == false) return;

            StopHeartbeat();
            if (IsCurrentHost(id) == false) return;

            _heartbeatLobbyId = id;
            _heartbeatCancellation = new CancellationTokenSource();
            _heartbeatTask = HeartbeatAsync(id, _heartbeatCancellation.Token);
        }

        private void StopHeartbeat()
        {
            _heartbeatCancellation?.Cancel();
            _heartbeatCancellation?.Dispose();
            _heartbeatCancellation = null;
            _heartbeatLobbyId = null;
        }

        private bool IsCurrentHost(string id)
        {
            return _disposed == false && string.IsNullOrEmpty(id) == false &&
                CurrentLobbyState.Id.Value.Value == id &&
                CurrentLobbyState.HostId.Value == _auth.CurrentProfile.UserId.Value;
        }

        private async Task HeartbeatAsync(string id, CancellationToken cancellation)
        {
            while (cancellation.IsCancellationRequested == false && IsCurrentHost(id))
            {
                try
                {
                    await Service.SendHeartbeatPingAsync(id);

                    if (cancellation.IsCancellationRequested || IsCurrentHost(id) == false) return;

                    Debug.Log("Lobby heartbeat");
                }
                catch (Exception ex)
                {
                    if (cancellation.IsCancellationRequested || IsCurrentHost(id) == false) return;

                    Debug.LogWarning($"Lobby heartbeat failed: {ex.Message}");
                }

                if (cancellation.IsCancellationRequested || IsCurrentHost(id) == false) return;

                try
                {
                    await _heartbeatDelay(TimeSpan.FromSeconds(15), cancellation);
                }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
                {
                    return;
                }
            }
        }
    }
}

