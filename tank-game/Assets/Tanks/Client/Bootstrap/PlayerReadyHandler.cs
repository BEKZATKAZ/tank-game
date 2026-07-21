using Cysharp.Threading.Tasks;
using Tanks.Client.Items;
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

        public async void Handle(PlayerReadyRequested message)
        {
            await _itemDatabase.TryInitializeAsync();

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
