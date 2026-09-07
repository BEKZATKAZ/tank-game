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
            IVisualService particlePlayer,
            ISpectatorService spectatorService)
        {
            _particlePlayer = particlePlayer;
            _spectatorService = spectatorService;
        }

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
                VisualType.Explosion_1, message.WorldPosition).Play();

            if (message.Target.IsPossessed == false) return;

            await UniTask.Delay(1500);

            Spectate(message.From, message.WorldPosition);
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
