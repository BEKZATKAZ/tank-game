using System;
using Tanks.Core.Common.Processes;
using UnityEngine;

namespace Tanks.Server.Bootstrap
{
    public sealed class HostProcessMonitor : MonoBehaviour
    {
        private HostProcessService _host;
        private float _nextCheck;

#if UNITY_SERVER && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            try
            {
                HostProcessService host = HostProcessService.FromArguments(
                    Environment.GetCommandLineArgs());

                if (host == null) return;

                GameObject owner = new GameObject(nameof(HostProcessMonitor));

                DontDestroyOnLoad(owner);
                owner.AddComponent<HostProcessMonitor>()._host = host;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Application.Quit(1);
            }
        }
#endif

        private void Update()
        {
            if (_host == null || Time.realtimeSinceStartup < _nextCheck) return;

            _nextCheck = Time.realtimeSinceStartup + 1;

            if (_host.IsRunning()) return;

            Debug.Log("Host client exited. Shutting down server.");
            Application.Quit();
        }
    }
}
