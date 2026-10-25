using Cysharp.Threading.Tasks;
using System;
using Tanks.Core;
using Tanks.Core.Common.Scenes;
using Tanks.Core.Lobbies;
using Unity.Netcode;
using UnityEngine;

namespace Tanks.Client.Bootstrap
{
    public sealed class GameplayExitService : IGameplayExitService
    {
        public GameplayExitService(ILobbyService lobbies, ISceneLoader sceneLoader)
        {
            _lobbies = lobbies;
            _sceneLoader = sceneLoader;
        }

        private readonly ILobbyService _lobbies;
        private readonly ISceneLoader _sceneLoader;
        private bool _exiting;
        private bool _exited;

        public async UniTask<ResultType> ExitAsync()
        {
            if (_exiting || _exited || _sceneLoader.IsLoading) return Result.Success();
            _exiting = true;

            try
            {
                if (string.IsNullOrEmpty(_lobbies.CurrentLobbyState.Id.Value.Value) == false)
                {
                    ResultType result = await _lobbies.LeaveLobbyAsync();
                    if (result.IsSuccess == false) return result;
                }

                if (NetworkManager.Singleton) NetworkManager.Singleton.Shutdown();

                if (_sceneLoader.IsLoading == false)
                {
                    _sceneLoader.StartLoading(new LoadMenus(), withTips: false);
                }

                _exited = true;
                return Result.Success();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return Result.Failure("failed-to-leave");
            }
            finally
            {
                _exiting = false;
            }
        }
    }
}
