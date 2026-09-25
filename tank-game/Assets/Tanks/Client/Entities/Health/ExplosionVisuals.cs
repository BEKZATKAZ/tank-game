using Cysharp.Threading.Tasks;
using System.Linq;
using Tanks.Client.Player;
using Tanks.Client.Rendering;
using Tanks.Client.Visuals;
using Tanks.Core.Helpers;
using Tanks.Shared;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace Tanks.Client.Entities.Health
{
    public sealed class ExplosionVisuals :
        NetworkComponent<IHealthBehaviour>,
        INetworkMessageHandler<Died>,
        IMessageHandler<GameStartedFromServer>
    {
        public ExplosionVisuals(
            IVisualService particlePlayer,
            ISpectatorService spectatorService,
            ICameraShakeService cameraShake,
            IMessageBus bus)
        {
            _particlePlayer = particlePlayer;
            _spectatorService = spectatorService;
            _cameraShake = cameraShake;
            _bus = bus;
        }

        private readonly IVisualService _particlePlayer;
        private readonly ISpectatorService _spectatorService;
        private readonly ICameraShakeService _cameraShake;
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
            _bus.RegisterHandler<GameStartedFromServer>(this);
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
