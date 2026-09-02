using Cysharp.Threading.Tasks;
using System.Linq;
using Tanks.Client.Player;
using Tanks.Client.Visuals;
using Tanks.Core.Helpers;
using Tanks.Shared;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace Tanks.Client.Entities.Health
{
    public sealed class ExplosionVisuals :
        NetworkComponent<IHealthBehaviour>,
        INetworkMessageHandler<Died>
    {
        public ExplosionVisuals(
            IHitstopService hitstop,
            IVisualService particlePlayer,
            ISpectatorService spectatorService)
        {
            _hitstop = hitstop;
            _particlePlayer = particlePlayer;
            _spectatorService = spectatorService;
        }

        private readonly IHitstopService _hitstop;
        private readonly IVisualService _particlePlayer;
        private readonly ISpectatorService _spectatorService;

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

        public async void Handle(Died message)
        {
            _particlePlayer.Play<ParticleSystem>(
                VisualType.Explosion, message.WorldPosition).Play();

            await UniTask.Delay(150);
            await _hitstop.RunAsync(0.3f);
            await UniTask.Delay(1000);

            Spectate(message.From, message.WorldPosition);
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
