using Cysharp.Threading.Tasks;
using System.Linq;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Health;
using Tanks.Shared.Messaging;
using Tanks.Shared.Weapons.Projectiles;
using UnityEngine;
using VContainer;

namespace Tanks.Server.Bootstrap
{
    public sealed class GameRestarter :
        NetworkComponent<GameStartBehaviour>,
        IMessageHandler<Died>
    {
        [Inject] private readonly IMessageBus _bus;
        [Inject] private readonly IProjectilePoolService _pool;

        private int _diedCount;

        public async void Handle(Died message, IMessageSender sender)
        {
            int currentCount = ++_diedCount;

            await UniTask.Delay(2500);

            // Stop if playmode stopped
            if (Application.isPlaying == false) return;

            // Discard multiple restarts
            if (currentCount != _diedCount) return;

            int alivePlayers = NetworkManager.ConnectedClients
              .Count(x => x.Value.PlayerObject);

            if (alivePlayers > 1) return;

            _pool.ReturnAll();
            Behaviour.PrepareGame();
        }

        private void Start()
        {
            _bus.RegisterHandler<Died>(this);
        }
    }
}
