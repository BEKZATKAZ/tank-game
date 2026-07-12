using FMODUnity;
using Tanks.Shared;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Mathematics;

namespace Tanks.Client.Weapons
{
    public sealed class ProjectileRicochetSound :
        NetworkComponent<ProjectileBehaviour>,
        INetworkMessageHandler<ProjectileRicocheted>
    {
        private static readonly EventReference _reference = RuntimeManager
            .PathToEventReference("event:/SFX/Ricochet");

        public void Handle(ProjectileRicocheted message)
        {
            float2 hitPoint = message.HitPoint;
            RuntimeManager.PlayOneShot(_reference, new float3(hitPoint.x, hitPoint.y, 0));
        }

        protected override void Awake()
        {
            base.Awake();
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
