using Tanks.Shared;
using Tanks.Shared.Adapters;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Client.Weapons
{
    public sealed class ProjectileRicochetParticle :
        NetworkComponent<ProjectileBehaviour>,
        INetworkMessageHandler<ProjectileRicocheted>
    {
        private ParticleSystem _particle;
        private ITransform _particleTransform;

        public void Handle(ProjectileRicocheted message)
        {
            float2 normal = message.SurfaceNormal;
            float angle = math.atan2(normal.y, normal.x) - math.PIHALF;

            _particleTransform.WorldPosition = message.HitPoint;
            _particleTransform.WorldRotation = Angle.FromRadians(angle);
            _particle.Play();
        }

        protected override void OnInitialize()
        {
            _particle = Behaviour.GetComponentInChildren<ParticleSystem>();

            Transform transform = _particle.transform;
            transform.SetParent(null, worldPositionStays: true);

            _particleTransform = new UnityTransform(transform);
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
