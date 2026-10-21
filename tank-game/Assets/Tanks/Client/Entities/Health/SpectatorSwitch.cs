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
        INetworkMessageHandler<Died>
    {
        public SpectatorSwitch(
            IClientEntityService clientEntityService,
            ISpectatorService spectatorService)
        {
            _clientEntityService = clientEntityService;
            _spectatorService = spectatorService;
        }

        private readonly IClientEntityService _clientEntityService;
        private readonly ISpectatorService _spectatorService;

        private void Spectate(IEntityBehaviour eliminatedFrom, float2 worldPosition)
        {
            // Already restarted
            if (_clientEntityService.GetPossessedEntity() is { IsValid: true })
            {
                return;
            }

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

        public async void Handle(Died message)
        {
            if (message.Target is not { IsPossessed: true }) return;

            await UniTask.Delay(2000);

            Spectate(message.From, message.WorldPosition);
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
