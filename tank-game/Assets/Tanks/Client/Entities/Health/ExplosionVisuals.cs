using Tanks.Client.Visuals;
using Tanks.Shared;
using Tanks.Shared.Entities.Health;
using UnityEngine;

namespace Tanks.Client.Entities.Health
{
    public sealed class ExplosionVisuals :
        NetworkComponent<IHealthBehaviour>,
        INetworkMessageHandler<Died>
    {
        public ExplosionVisuals(IVisualService particlePlayer)
        {
            _particlePlayer = particlePlayer;
        }

        private readonly IVisualService _particlePlayer;

        public void Handle(Died message)
        {
            _particlePlayer.Play<ParticleSystem>(
                VisualType.Explosion, message.WorldPosition).Play();
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
