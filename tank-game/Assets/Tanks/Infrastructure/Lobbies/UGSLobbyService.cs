using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tanks.Core;
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
        public UGSLobbyService(IAuthService auth, ISessionSettingsService settings)
        {
            _auth = auth;
            _settings = settings;

            CurrentLobbyState.Id.Bind(OnLobbyIdChanged);
            Application.quitting += Dispose;
        }

        public LobbyState CurrentLobbyState { get; private set; } = new();

        public event Action<LobbyPlayerState> PlayerJoined;
        public event Action<LobbyPlayerState> PlayerLeft;
        public event Action LobbyDeleted;

        private Unity.Services.Lobbies.ILobbyService Service => _service ?? LobbyService.Instance;

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

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            Application.quitting -= Dispose;
            CurrentLobbyState.Id.Unbind(OnLobbyIdChanged);
            StopHeartbeat();
        }

        private void OnLobbyIdChanged(LobbyId id)
        {
            if (id.Value != _heartbeatLobbyId) StopHeartbeat();
        }

        private async Task SubscribeToEventsAsync()
        {
            LobbyId id = CurrentLobbyState.Id.Value;

            if (id.Value is null) return;
            LobbyEventCallbacks callbacks = new LobbyEventCallbacks();

            callbacks.LobbyChanged += OnLobbyChanged;

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
            CurrentLobbyState.Id.Value = default;
            _lobby = null;
            CurrentLobbyState.Players.Value = new Dictionary<string, LobbyPlayerState>();
            LobbyDeleted?.Invoke();
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

        private Player CreatePlayerData()
        {
            PlayerProfileState profile = _auth.CurrentProfile;

            return new Player(id: profile.UserId.Value, data: new()
            {
                ["username"] = new(VisibilityOptions.Member, profile.DisplayName.Value),
                ["ready"] = new(VisibilityOptions.Member, bool.FalseString)
            });
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

                CurrentLobbyState.Id.Value = default;
                CurrentLobbyState.Settings.Value = default;
                CurrentLobbyState.ServerConfirmedSettings.Value = default;
                CurrentLobbyState.Status.Value = default;

                _lobby = null;
                CurrentLobbyState.Players.Value = new Dictionary<string, LobbyPlayerState>();

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

