using PlayFab;
using System;
using System.Linq;
using UnityEngine;

namespace Tanks.Server.Bootstrap
{
    public interface IVirtualMachineService
    {
        ushort GetListeningPort();

        void Start();
        void StandBy();
    }

    public sealed class PlayFabVirtualMachineService : IVirtualMachineService, IDisposable
    {
        public PlayFabVirtualMachineService()
        {
#if ENABLE_PLAYFABSERVER_API
            if (Application.isEditor) return;
            PlayFabMultiplayerAgentAPI.OnShutDownCallback += OnShutDown;
#endif
        }

        public ushort GetListeningPort()
        {
#if ENABLE_PLAYFABSERVER_API
            if (Application.isEditor) return 7777;

            return (ushort)PlayFabMultiplayerAgentAPI
                .GetGameServerConnectionInfo()
                .GamePortsConfiguration
                .Single(x => x.Name == "game_port")
                .ServerListeningPort;
#else
            return 7777;
#endif
        }

        public void Dispose()
        {
#if ENABLE_PLAYFABSERVER_API
            if (Application.isEditor) return;
            PlayFabMultiplayerAgentAPI.OnShutDownCallback -= OnShutDown;
#endif
        }

        public void StandBy()
        {
#if ENABLE_PLAYFABSERVER_API
            if (Application.isEditor) return;
            PlayFabMultiplayerAgentAPI.ReadyForPlayers();
#endif
        }

        public void Start()
        {
#if ENABLE_PLAYFABSERVER_API
            if (Application.isEditor) return;
            PlayFabMultiplayerAgentAPI.Start();
#endif
        }

        private void OnShutDown()
        {
            Application.Quit();
        }
    }
}
