using Cysharp.Threading.Tasks;
using PlayFab;
using PlayFab.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using Tanks.Core;
using Tanks.Core.Lobbies;
using UnityEngine;

namespace Tanks.Infrastructure.Lobbies
{
    public sealed class PlayFabLobbyService : ILobbyService, IDisposable
    {
        public PlayFabLobbyService(IPlayFabCaller caller)
        {
            _caller = caller;

            PlayFabMultiplayer.OnLobbyMemberAdded += OnPlayerJoined;
            PlayFabMultiplayer.OnLobbyMemberRemoved += OnPlayerLeft;
            PlayFabMultiplayer.OnLobbyUpdated += OnLobbyUpdated;

            Updater().Forget();
        }

        public event Action<LobbyPlayerState> PlayerJoined;
        public event Action<LobbyPlayerState> PlayerLeft;

        public LobbyState CurrentLobbyState { get; private set; } = new LobbyState();

        private readonly IPlayFabCaller _caller;
        private UniTaskCompletionSource<ResultType<IEnumerable<LobbySearchState>>> _searchTcs;
        private Lobby _currentLobby;
        private bool _isDisposed;

        private async UniTaskVoid Updater()
        {
            while (_isDisposed == false && Application.isPlaying)
            {
                await UniTask.Delay(100);
                PlayFabMultiplayer.ProcessLobbyStateChanges();
            }
        }

        private void OnPlayerJoined(Lobby lobby, PFEntityKey member)
        {
            if (_currentLobby is null) return;
            _currentLobby = lobby;

            LobbyMapper.MapLobby(_currentLobby, CurrentLobbyState);
            PlayerJoined?.Invoke(LobbyMapper.MapPlayer(lobby, member));
        }

        private void OnPlayerLeft(Lobby lobby, PFEntityKey member, LobbyMemberRemovedReason reason)
        {
            if (_currentLobby is null) return;
            _currentLobby = lobby;

            LobbyMapper.MapLobby(_currentLobby, CurrentLobbyState);
            PlayerLeft?.Invoke(LobbyMapper.MapPlayer(lobby, member));
        }

        private void OnLobbyUpdated(
            Lobby lobby,
            bool ownerUpdated,
            bool maxMembersUpdated,
            bool accessPolicyUpdated,
            bool membershipLockUpdated,
            IList<string> updatedSearchPropertyKeys,
            IList<string> updatedLobbyPropertyKeys,
            IList<LobbyMemberUpdateSummary> memberUpdates)
        {
            if (_currentLobby is null) return;
            _currentLobby = lobby;
            LobbyMapper.MapLobby(_currentLobby, CurrentLobbyState);
        }

        public void Dispose()
        {
            LeaveLobby();
            _isDisposed = true;

            PlayFabMultiplayer.OnLobbyMemberAdded -= OnPlayerJoined;
            PlayFabMultiplayer.OnLobbyMemberRemoved -= OnPlayerLeft;
            PlayFabMultiplayer.OnLobbyUpdated -= OnLobbyUpdated;
        }

        public UniTask<ResultType<IEnumerable<LobbySearchState>>> FindLobbiesAsync(string name = null)
        {
            // Block new requests if already searching
            if (_searchTcs != null) return _searchTcs.Task;

            _searchTcs = new UniTaskCompletionSource<ResultType<IEnumerable<LobbySearchState>>>();

            void OnFound(IList<LobbySearchResult> lobbies, PFEntityKey searcher, int result)
            {
                PlayFabMultiplayer.OnLobbyFindLobbiesCompleted -= OnFound;

                if (LobbyError.SUCCEEDED(result))
                {
                    IEnumerable<LobbySearchState> collection = lobbies
                        .Select(LobbyMapper.MapLobbySearch);

                    _searchTcs.TrySetResult(Result.Success(collection));
                    _searchTcs = null;

                    return;
                }

                string errorCode = _caller.ErrorToCode((PlayFabLobbyError)result);
                _searchTcs.TrySetResult(Result.Failure<IEnumerable<LobbySearchState>>(errorCode));
                _searchTcs = null;
            }

            // Ignore full lobbies
            string filterString = "lobby/memberCountRemaining gt 0";

            if (string.IsNullOrWhiteSpace(name) == false)
            {
                name = name.ToLowerInvariant();
                filterString += $" and {LobbyConstants.LOBBY_NAME_LOWER_KEY} eq '{name}'";
            }

            PlayFabMultiplayer.OnLobbyFindLobbiesCompleted += OnFound;

            PlayFabMultiplayer.FindLobbies(PlayFabSettings.staticPlayer, new LobbySearchConfiguration
            {
                ClientSearchResultCount = 20,
                SortString = "lobby/memberCountRemaining desc",
                FilterString = filterString
            });

            return _searchTcs.Task;
        }

        public async UniTask<ResultType<LobbyState>> HostLobbyAsync(string name, uint maxPlayers)
        {
            var tcs = new UniTaskCompletionSource<ResultType<Lobby>>();

            void OnHosted(Lobby lobby, int result)
            {
                PlayFabMultiplayer.OnLobbyCreateAndJoinCompleted -= OnHosted;
                LobbyCodeToResult(tcs, lobby, result);
            }

            LobbyCreateConfiguration create = new LobbyCreateConfiguration
            {
                AccessPolicy = LobbyAccessPolicy.Public,
                OwnerMigrationPolicy = LobbyOwnerMigrationPolicy.Automatic,
                MaxMemberCount = maxPlayers,
                SearchProperties = new Dictionary<string, string>()
                {
                    [LobbyConstants.LOBBY_NAME_REGULAR_KEY] = name,
                    [LobbyConstants.LOBBY_NAME_LOWER_KEY] = name.ToLowerInvariant()
                }
            };

            LobbyJoinConfiguration join = new LobbyJoinConfiguration
            {
                MemberProperties = new Dictionary<string, string>()
            };

            PlayFabMultiplayer.OnLobbyCreateAndJoinCompleted += OnHosted;
            PlayFabMultiplayer.CreateAndJoinLobby(PlayFabSettings.staticPlayer, create, join);

            ResultType<Lobby> result = await tcs.Task;
            return Result.Map(result, l => LobbyMapper.MapLobby(l, CurrentLobbyState));
        }

        public async UniTask<ResultType<LobbyState>> JoinLobbyAsync(LobbyAddress address)
        {
            var tcs = new UniTaskCompletionSource<ResultType<Lobby>>();

            void OnJoined(Lobby lobby, PFEntityKey member, int result)
            {
                PlayFabMultiplayer.OnLobbyJoinCompleted -= OnJoined;
                LobbyCodeToResult(tcs, lobby, result);
            }

            PlayFabMultiplayer.OnLobbyJoinCompleted += OnJoined;
            PlayFabMultiplayer.JoinLobby(PlayFabSettings.staticPlayer, address.Value, null);

            ResultType<Lobby> result = await tcs.Task;
            return Result.Map(result, l => LobbyMapper.MapLobby(l, CurrentLobbyState));
        }

        public void RenameLobby(string newName)
        {
            _currentLobby.PostUpdate(PlayFabSettings.staticPlayer, new LobbyDataUpdate
            {
                SearchProperties = new Dictionary<string, string>()
                {
                    [LobbyConstants.LOBBY_NAME_REGULAR_KEY] = newName,
                    [LobbyConstants.LOBBY_NAME_LOWER_KEY] = newName.ToLowerInvariant()
                }
            });
        }

        public void LeaveLobby()
        {
            _currentLobby?.Leave(PlayFabSettings.staticPlayer);
            _currentLobby = null;
        }

        private void LobbyCodeToResult(
            UniTaskCompletionSource<ResultType<Lobby>> tcs, Lobby lobby, int result)
        {
            if (LobbyError.SUCCEEDED(result))
            {
                _currentLobby = lobby;
                tcs.TrySetResult(lobby);
                return;
            }

            string errorCode = _caller.ErrorToCode((PlayFabLobbyError)result);
            tcs.TrySetResult(Result.Failure<Lobby>(errorCode));
        }
    }
}
