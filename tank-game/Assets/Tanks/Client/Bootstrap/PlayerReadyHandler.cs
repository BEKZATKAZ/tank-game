using Cysharp.Threading.Tasks;
using Tanks.Client.Items;
using Tanks.Localization;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Bootstrap
{
    public sealed class PlayerReadyHandler :
        NetworkComponent<GameStartBehaviour>,
        INetworkMessageHandler<PlayerReadyRequested>
    {
        [Inject] private readonly IClientItemDatabase _itemDatabase;
        [Inject] private readonly ILocalizationLoader _localizationLoader;

        private bool _isInitialized;

        public async void Handle(PlayerReadyRequested message)
        {
            if (_isInitialized == false)
            {
                await UniTask.WhenAll(
                    _itemDatabase.TryInitializeAsync(),
                    _localizationLoader.WaitForInitializationAsync());

                _isInitialized = true;
            }

            while (NetworkManager.LocalClient.PlayerObject == false)
            {
                await UniTask.Delay(500);

                // Stop if playmode stopped
                if (Application.isPlaying == false) return;
            }

            Behaviour.SendReadyConfirmationServerRpc(message.SecretHandle);
        }

        private void Start()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
