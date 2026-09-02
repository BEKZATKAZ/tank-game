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
        public ExplosionVisuals(IVisualService particlePlayer, ISpectatorService spectatorService)
        {
            _particlePlayer = particlePlayer;
            _spectatorService = spectatorService;
        }

        private readonly IVisualService _particlePlayer;
        private readonly ISpectatorService _spectatorService;

        public async void Handle(Died message)
        {
            _particlePlayer.Play<ParticleSystem>(
                VisualType.Explosion, message.WorldPosition).Play();
            
            await UniTask.Delay(1500);

            IEntityBehaviour newTarget = message.From;

            if (newTarget is null || newTarget.IsValid == false)
            {
                newTarget = NetworkManager.Singleton.SpawnManager.PlayerObjects
                    .Select(x => x.GetComponent<IEntityBehaviour>())
                    .OrderBy(x => math.distance(x.Transform.WorldPosition, message.WorldPosition))
                    .FirstOrDefault();
            }

            if (newTarget is null || newTarget.IsValid == false) return;

            _spectatorService.Spectate(newTarget.AsRequired());
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
