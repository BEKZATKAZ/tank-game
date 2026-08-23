using Cysharp.Threading.Tasks;
using Tanks.Client.Items;
using Tanks.Localization;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Tanks.Client.Bootstrap
{
    public sealed class PlayerReadyHandler :
        NetworkComponent<GameStartBehaviour>,
        INetworkMessageHandler<PlayerReadyRequested>
    {
        [Inject] private readonly IClientItemDatabase _itemDatabase;
        [Inject] private readonly ILocalizationDatabase _localization;
        [Inject] private readonly IObjectResolver _container;

        private bool _isInitialized;

        public async void Handle(PlayerReadyRequested message)
        {
            if (_isInitialized == false)
            {
                await UniTask.WhenAll(
                    Behaviour.TryLoadSceneAsync(),
                    _itemDatabase.TryInitializeAsync(),
                    _localization.WaitForInitializationAsync());

                _isInitialized = true;
            }

            while (Behaviour.NetworkManager.LocalClient.PlayerObject == false)
            {
                await UniTask.Delay(500);

                // Stop if playmode stopped
                if (Application.isPlaying == false) return;
            }

            GameObject player = Behaviour.NetworkManager.LocalClient.PlayerObject.gameObject;
            _container.InjectGameObject(player);

            Behaviour.SendReadyConfirmationServerRpc(message.SecretHandle);
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
