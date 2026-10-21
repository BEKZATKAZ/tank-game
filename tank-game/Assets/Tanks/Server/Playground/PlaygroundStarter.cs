using Cysharp.Threading.Tasks;
using System;
using Tanks.Core.Common.Processes;
using Tanks.Shared.Items;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Tanks.Server.Playground
{
    public sealed class PlaygroundStarter : MonoBehaviour
    {
        [Inject] private readonly IItemDatabase _sharedItemDatabase;

        private async void Start()
        {
            try
            {
                await _sharedItemDatabase.WaitForInitializationAsync();
                NetworkManager.Singleton.StartHost();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Application.Quit(1);
            }
        }
    }
}
