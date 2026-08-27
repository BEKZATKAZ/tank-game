using Cysharp.Threading.Tasks;
using Tanks.Client.Items;
using Tanks.Localization;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;

namespace Tanks.Client.Bootstrap
{
    public sealed class PlayerReadyHandler :
        NetworkComponent<GameStartBehaviour>,
        INetworkMessageHandler<PlayerReadyRequested>
    {
        public PlayerReadyHandler(
            IClientItemDatabase itemDatabase,
            ILocalizationDatabase localization)
        {
            _itemDatabase = itemDatabase;
            _localization = localization;
        }

        private readonly IClientItemDatabase _itemDatabase;
        private readonly ILocalizationDatabase _localization;

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
            }

            Behaviour.SendReadyConfirmationServerRpc(message.SecretHandle);
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
