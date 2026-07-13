using Cysharp.Threading.Tasks;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Unity.Netcode;
using UnityEngine;

namespace Tanks.Client.Bootstrap
{
    public sealed class PlayerReadyHandler :
        NetworkComponent<GameStartBehaviour>,
        INetworkMessageHandler<PlayerReadyRequested>
    {
        public async void Handle(PlayerReadyRequested message)
        {
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
