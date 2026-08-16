using PlayFab;
using PlayFab.Multiplayer;
using System.Collections.Generic;
using System.Linq;
using Tanks.Core.Lobbies;

namespace Tanks.Infrastructure.Lobbies
{
    public static class LobbyMapper
    {
        private static IDictionary<string, LobbyPlayerState> MapPlayers(Lobby lobby)
        {
            return lobby
                .GetMembers()
                .Select(x => KeyValuePair.Create(x.Id, MapPlayer(lobby, x)))
                .ToDictionary(k => k.Key, v => v.Value);
        }

        public static LobbyPlayerState MapPlayer(Lobby lobby, PFEntityKey player) => new()
        {
            UserId = player.Id,
            Data = lobby.GetMemberProperties(player)
        };

        public static LobbyState MapLobby(Lobby lobby, LobbyState destination)
        {
            string localPlayerId = PlayFabSettings.staticPlayer.EntityId;

            lobby.GetSearchProperties()
                .TryGetValue(LobbyConstants.LOBBY_NAME_REGULAR_KEY, out string name);

            destination.Id.Value = new LobbyId(lobby.Id);
            destination.Address.Value = new LobbyAddress(lobby.ConnectionString);

            destination.OwnerId.Value = lobby.TryGetOwner(out PFEntityKey owner) ? owner.Id : "";
            destination.IsLocalPlayerOwner.Value = owner?.Id == localPlayerId;
            destination.Name.Value = name ?? "";

            destination.MaxPlayerCount.Value = (int)lobby.MaxMemberCount;
            destination.Players.Value = MapPlayers(lobby);

            return destination;
        }

        public static LobbySearchState MapLobbySearch(LobbySearchResult lobby)
        {
            lobby.SearchProperties
                .TryGetValue(LobbyConstants.LOBBY_NAME_REGULAR_KEY, out string name);

            return new LobbySearchState
            {
                Id = new LobbyId(lobby.LobbyId),
                Address = new LobbyAddress(lobby.ConnectionString),
                Name = name ?? "",

                MaxPlayerCount = (int)lobby.MaxMemberCount,
                PlayerCount = (int)lobby.CurrentMemberCount
            };
        }
    }
}
