using Cysharp.Threading.Tasks;
using System.Linq;
using Tanks.Client.Player;
using Tanks.Core.Helpers;
using Tanks.Shared;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using Unity.Mathematics;
using Unity.Netcode;

namespace Tanks.Client.Entities.Health
{
    public sealed class SpectatorSwitch :
        NetworkComponent<IHealthBehaviour>,
        INetworkMessageHandler<Died>,
        IMessageHandler<GameStartedFromServer>
    {
        public SpectatorSwitch(
            ISpectatorService spectatorService,
            IMessageBus bus)
        {
            _spectatorService = spectatorService;
            _bus = bus;
            _bus.RegisterHandler<GameStartedFromServer>(this);
        }

        private readonly ISpectatorService _spectatorService;
        private readonly IMessageBus _bus;

        private IEntityBehaviour _eliminatedFrom;
        private float2 _eliminatedPosition;
        private float _lastEliminatedTime = -1;
        private float _lastTime;

        private void Spectate(IEntityBehaviour eliminatedFrom, float2 worldPosition)
        {
            IEntityBehaviour newTarget = eliminatedFrom;

            if (newTarget is null || newTarget.IsValid == false)
            {
                newTarget = NetworkManager.Singleton.SpawnManager.PlayerObjects
                    .Select(x => x.GetComponent<IEntityBehaviour>())
                    .OrderBy(x => math.distance(x.Transform.WorldPosition, worldPosition))
                    .FirstOrDefault();
            }

            if (newTarget is null || newTarget.IsValid == false) return;

            _spectatorService.Spectate(newTarget.AsRequired());
        }

        public void Handle(Died message)
        {
            if (message.Target is not { IsPossessed: true }) return;

            _lastEliminatedTime = _lastTime;
            _eliminatedFrom = message.From;
            _eliminatedPosition = message.WorldPosition;
        }

        public void Handle(GameStartedFromServer message, IMessageSender sender = null)
        {
            _lastEliminatedTime = -1;
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }

        protected override void OnDestroy()
        {
            _bus.UnregisterHandler<GameStartedFromServer>(this);
        }

        protected override void OnUpdate(float deltaTime)
        {
            _lastTime += deltaTime;

            if (_lastEliminatedTime < 0) return;
            if (_lastTime - _lastEliminatedTime < 2) return;

            _lastEliminatedTime = -1;
            Spectate(_eliminatedFrom, _eliminatedPosition);
        }
    }
}
