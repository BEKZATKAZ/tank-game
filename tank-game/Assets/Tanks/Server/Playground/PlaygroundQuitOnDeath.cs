using Cysharp.Threading.Tasks;
using Tanks.Shared;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using Tanks.Shared.Playground;
using Unity.Netcode;
using UnityEngine;

namespace Tanks.Server.Playground
{
    public sealed class PlaygroundQuitOnDeath :
        NetworkComponent<IPlaygroundStartBehaviour>,
        IMessageHandler<Died>
    {
        public PlaygroundQuitOnDeath(IMessageBus bus)
        {
            _bus = bus;
        }

        private readonly IMessageBus _bus;

        public async void Handle(Died message, IMessageSender sender)
        {
            if (message.Target.OwnerClientId == 0) return;
            await UniTask.Delay(2500);

            NetworkManager.Singleton.Shutdown(discardMessageQueue: true);
            Application.Quit();
        }

        protected override void OnInitialize()
        {
            _bus.RegisterHandler<Died>(this);
        }

        protected override void OnDestroy()
        {
            _bus.UnregisterHandler<Died>(this);
        }
    }
}
