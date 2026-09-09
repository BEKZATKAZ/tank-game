using Cysharp.Threading.Tasks;
using System.Linq;
using Tanks.Client.Player;
using Tanks.Client.Rendering;
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
            ISpectatorService spectatorService,
            ICameraShakeService cameraShake)
        {
            _particlePlayer = particlePlayer;
            _spectatorService = spectatorService;
            _cameraShake = cameraShake;
        }

        private readonly IVisualService _particlePlayer;
        private readonly ISpectatorService _spectatorService;
        private readonly ICameraShakeService _cameraShake;

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

            _cameraShake.AddShake(new CameraImpulse(
                position: Transform.WorldPosition,
                amplitude: 5,
                frequency: 1.5f,
                radius: 25,
                duration: 2.75f));

            _cameraShake.AddShake(new CameraImpulse(
                position: Transform.WorldPosition,
                amplitude: 0.4f,
                frequency: 15,
                radius: 15,
                duration: 1.5f));

            if (message.Target is not { IsPossessed: true }) return;

            await UniTask.Delay(1500);

            Spectate(message.From, message.WorldPosition);
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
