using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Core;
using Tanks.Core.Common;
using Tanks.Core.Lobbies;
using Tanks.Core.Lobbies.Payloads;
using Tanks.Utils;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

using static Unity.Services.Lobbies.Models.PlayerDataObject;

namespace Tanks.Infrastructure.Lobbies
{
    public sealed class UGSLobbyService : Core.Lobbies.ILobbyService
    {
        public UGSLobbyService(IAuthService auth)
        {
            _auth = auth;
        }

        public LobbyState CurrentLobbyState { get; private set; } = new();

        public event Action<LobbyPlayerState> PlayerJoined;
        public event Action<LobbyPlayerState> PlayerLeft;
        public event Action LobbyDeleted;

        private Unity.Services.Lobbies.ILobbyService Service => LobbyService.Instance;

        private readonly IAuthService _auth;
        private ILobbyEvents _lobbyEvents;

        private async UniTask SubscribeToEventsAsync()
        {
            LobbyId id = CurrentLobbyState.Id.Value;

            if (id.Value is null) return;
            LobbyEventCallbacks callbacks = new LobbyEventCallbacks();

            callbacks.PlayerJoined += OnPlayerJoined;
            callbacks.PlayerLeft += OnPlayerLeft;
            callbacks.LobbyDeleted += OnLobbyDeleted;

            callbacks.DataAdded += OnDataChanged;
            callbacks.DataChanged += OnDataChanged;
            callbacks.DataRemoved += OnDataChanged;

            _lobbyEvents = await Service.SubscribeToLobbyEventsAsync(id.Value, callbacks);
        }

        private async UniTask UnsubscribeFromEventsAsync()
        {
            if (_lobbyEvents is null) return;
            await _lobbyEvents.UnsubscribeAsync();
            _lobbyEvents = null;
        }

        private void OnLobbyDeleted()
        {
            CurrentLobbyState.Id.Value = default;
            LobbyDeleted?.Invoke();
        }

        private void OnDataChanged(Dictionary<string, ChangedOrRemovedLobbyValue<DataObject>> data)
        {
            UGSLobbyMapper.MergeChangedData(data, CurrentLobbyState);
        }

        private void OnPlayerJoined(List<LobbyPlayerJoined> players)
        {
            foreach (LobbyPlayerJoined player in players)
            {
                LobbyPlayerState mapped = UGSLobbyMapper.MapPlayer(player.Player);
                CurrentLobbyState.Players.Value[mapped.UserId] = mapped;
                PlayerJoined?.Invoke(mapped);
            }

            CurrentLobbyState.Players.ForceRefresh();
        }

        private void OnPlayerLeft(List<int> players)
        {
            using var toRemove = new TemporaryArrayResult<string>(players.Count);

            for (int i = 0; i < players.Count; i++)
            {
                int playerIndex = players[i];
                string id = CurrentLobbyState.Players.Value.Skip(playerIndex).FirstOrDefault().Key;
                toRemove[i] = id;
            }

            for (int i = 0; i < players.Count; i++)
            {
                CurrentLobbyState.Players.Value.Remove(toRemove[i], out LobbyPlayerState removed);
                PlayerLeft?.Invoke(removed);
            }

            CurrentLobbyState.Players.ForceRefresh();
        }

        private Player CreatePlayerData()
        {
            PlayerProfileState profile = _auth.CurrentProfile;

            return new Player(id: profile.UserId.Value, data: new()
            {
                ["username"] = new(VisibilityOptions.Member, profile.DisplayName.Value)
            });
        }

        public UniTask<ResultType<IEnumerable<LobbySearchState>>> FindLobbiesAsync(string name = null)
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

        public async UniTask<ResultType<LobbyState>> HostLobbyAsync(string name, uint maxPlayers)
        {
            var result = await UGSLobbyMapper.MapAsync(
                () => Service.CreateLobbyAsync(name, (int)maxPlayers, new()
                {
                    Player = CreatePlayerData()
                }), CurrentLobbyState, errorCode: "failed-to-create");

            await SubscribeToEventsAsync();
            HeartbeatAsync().Forget();

            return result;
        }

        public async UniTask<ResultType<LobbyState>> JoinLobbyAsync(LobbyAddress address)
        {
            var result = await UGSLobbyMapper.MapAsync(
                () => Service.JoinLobbyByCodeAsync(address.Value, new()
                {
                    Player = CreatePlayerData()
                }), CurrentLobbyState, errorCode: "failed-to-join");

            await SubscribeToEventsAsync();
            return result;
        }

        public async UniTask<ResultType<LobbyState>> JoinLobbyAsync(LobbyId id)
        {
            var result = await UGSLobbyMapper.MapAsync(
                () => Service.JoinLobbyByIdAsync(id.Value, new()
                {
                    Player = CreatePlayerData()
                }), CurrentLobbyState, errorCode: "failed-to-join");

            await SubscribeToEventsAsync();
            return result;
        }

        public UniTask<ResultType<LobbyState>> UpdateLobbyAsync(LobbyUpdateRequest options)
        {
            LobbyId lobbyId = CurrentLobbyState.Id.Value;

            return UGSLobbyMapper.MapAsync(() => Service.UpdateLobbyAsync(lobbyId.Value, new()
            {
                Name = options.Name,
                Data = UGSLobbyMapper.MapDataToUpdate(options)
            }), CurrentLobbyState, errorCode: "failed-to-update");
        }

        public async UniTask<ResultType> LeaveLobbyAsync()
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

                await UnsubscribeFromEventsAsync();
                CurrentLobbyState.Id.Value = default;

                return Result.Success();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return Result.Failure("failed-to-leave");
            }
        }

        private async UniTaskVoid HeartbeatAsync()
        {
            LobbyId currentId = CurrentLobbyState.Id.Value;

            while (currentId.Value == CurrentLobbyState.Id.Value.Value)
            {
                await UniTask.Delay(15000);

                if (currentId.Value != CurrentLobbyState.Id.Value.Value) return;

                await Service.SendHeartbeatPingAsync(currentId.Value);
            }
        }
    }
}

