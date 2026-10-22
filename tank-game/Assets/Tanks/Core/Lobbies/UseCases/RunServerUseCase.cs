using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Tanks.Core.Common.Processes;
using Tanks.Core.Lobbies.Payloads;
using Tanks.Core.Lobbies.Responses;
using UnityEngine;

namespace Tanks.Core.Lobbies.UseCases
{
    public interface IRunServerUseCase
    {
        UniTask<ResultType> RunAsync(
            RequestedServerScene scene = RequestedServerScene.Gameplay,
            bool authorize = true);
    }

    public sealed class RunServerUseCase : IRunServerUseCase
    {
        public RunServerUseCase(
            IAuthService auth,
            IProcessService service,
            IClientCommunicationService client)
        {
            _auth = auth;
            _service = service;
            _client = client;
        }

#if UNITY_STANDALONE_WIN
        public const string SERVER_PATH = "Server/TANKS.exe";
#elif UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
        public const string SERVER_PATH = "Server/TANKS";
#else
        public const string SERVER_PATH = "";
#endif

        public const string SERVER_ARGUMENTS = "-nographics -batchmode -logfile log.txt";

        private readonly IAuthService _auth;
        private readonly IProcessService _service;
        private readonly IClientCommunicationService _client;
        private int _lastServerProcessId = -1;

        public async UniTask<ResultType> RunAsync(
            RequestedServerScene scene = RequestedServerScene.Gameplay,
            bool authorize = true)
        {
            LoadSceneRequest sceneRequest = new LoadSceneRequest
            {
                Scene = scene
            };

#if UNITY_EDITOR
            if (Application.isEditor && Application.isPlaying)
            {
                // Skips only when running in Unity or in tests
                return await _client.SendRequestAsync(sceneRequest);
            }
#endif
            try
            {
                if (_lastServerProcessId != -1) _service.KillById(_lastServerProcessId);
                _lastServerProcessId = _service.Launch(SERVER_PATH, SERVER_ARGUMENTS);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return Result.Failure("failed-to-start-server");
            }

            using CancellationTokenSource sceneTimeout = new CancellationTokenSource(15000);
            ResultType sceneLoad = await _client.SendRequestAsync(sceneRequest, sceneTimeout.Token);

            if (sceneLoad.IsSuccess == false)
            {
                _service.KillById(_lastServerProcessId);
                return sceneLoad;
            }

            if (authorize == false)
            {
                return Result.Success();
            }

            using CancellationTokenSource timeout = new CancellationTokenSource(15000);

            ResultType<ServerAuthReady> auth = await _client
                .SendRequestAsync<AuthenticateServerRequest, ServerAuthReady>(
                    default, timeout.Token);

            if (auth.IsSuccess == false)
            {
                _service.KillById(_lastServerProcessId);
                return auth;
            }

            return await _auth.ConfirmCodeAsync(auth.Value.AuthCode);
        }
    }
}
