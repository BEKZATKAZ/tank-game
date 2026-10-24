using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Lobbies;
using Tanks.Core.Lobbies.Payloads;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

using static Unity.Services.Lobbies.Models.DataObject;

namespace Tanks.Infrastructure.Lobbies
{
    public static class UGSLobbyMapper
    {
        public static async Task<ResultType<LobbyState>> MapAsync(
            Func<Task<Lobby>> task, LobbyState destination, string errorCode = "unknown")
        {
            try
            {
                return MapLobby(await task(), destination);
            }
            catch (LobbyServiceException ex)
            {
                Debug.LogException(ex);
                return Result.Failure<LobbyState>(ReasonToErrorCode(ex.Reason));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return Result.Failure<LobbyState>(errorCode);
            }
        }

        public static async Task<ResultType<IEnumerable<LobbySearchState>>> MapAsync(
            Func<Task<QueryResponse>> task)
        {
            try
            {
                QueryResponse response = await task();
                return Result.Success(response.Results.Select(MapLobbySearch));
            }
            catch (LobbyServiceException ex)
            {
                Debug.LogException(ex);
                return Result.Failure<IEnumerable<LobbySearchState>>(ReasonToErrorCode(ex.Reason));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return Result.Failure<IEnumerable<LobbySearchState>>("unknown");
            }
        }

        public static LobbyState MapLobby(Lobby from, LobbyState destination)
        {
            destination.Id.Value = new LobbyId(from.Id);
            destination.Address.Value = new LobbyAddress(from.LobbyCode);

            destination.MaxPlayerCount.Value = from.MaxPlayers;
            destination.Name.Value = from.Name;
            destination.HostId.Value = from.HostId;

            destination.LobbyData.Clear();

            if (from.Data is { Count: > 0 })
            {
                foreach (var data in from.Data)
                {
                    destination.LobbyData[data.Key] = data.Value.Value;
                }
            }

            destination.Refresh();
            destination.Players.Value = from.Players.ToDictionary(k => k.Id, v => MapPlayer(v));
            return destination;
        }

        public static LobbySearchState MapLobbySearch(Lobby from) => new LobbySearchState
        {
            Id = new LobbyId(from.Id),
            Address = new LobbyAddress(from.LobbyCode),
            Name = from.Name,
            MaxPlayerCount = from.MaxPlayers,
            PlayerCount = from.Players.Count
        };

        public static LobbyPlayerState MapPlayer(Player from)
        {
            return new LobbyPlayerState
            {
                UserId = from.Id,
                Data = from.Data?.ToDictionary(k => k.Key, v => v.Value.Value) ?? new()
            };
        }

        public static LobbyState MergeChangedData(
            Dictionary<string, ChangedOrRemovedLobbyValue<DataObject>> data,
            LobbyState destination)
        {
            foreach ((string key, ChangedOrRemovedLobbyValue<DataObject> state) in data)
            {
                if (state.Removed)
                {
                    destination.LobbyData.Remove(key);
                    continue;
                }

                destination.LobbyData[key] = state.Value.Value;
            }

            destination.Refresh();
            return destination;
        }

        public static Dictionary<string, DataObject> MapDataToUpdate(LobbyUpdateRequest options)
        {
            Dictionary<string, DataObject> result = new();

            if (options.JoinCode != null)
            {
                result["join-code"] = new(VisibilityOptions.Member, options.JoinCode);
            }

            if (options.Status.HasValue)
            {
                int status = (int)options.Status.Value;
                result["status"] = new(VisibilityOptions.Member, status.ToString());
            }

            if (options.Settings.HasValue)
            {
                string json = JsonConvert.SerializeObject(options.Settings.Value);
                result["settings"] = new(VisibilityOptions.Member, json);
            }

            if (options.ServerConfirmedSettings.HasValue)
            {
                string json = JsonConvert.SerializeObject(options.ServerConfirmedSettings.Value);
                result["server-confirmed-settings"] = new(VisibilityOptions.Member, json);
            }

            return result;
        }

        private static string ReasonToErrorCode(LobbyExceptionReason reason)
        {
            string input = reason.ToString();

            input = Regex.Replace(input, "([A-Z]+)([A-Z][a-z])", "$1-$2");
            input = Regex.Replace(input, "([a-z0-9])([A-Z])", "$1-$2");

            return input.ToLowerInvariant();
        }
    }
}
