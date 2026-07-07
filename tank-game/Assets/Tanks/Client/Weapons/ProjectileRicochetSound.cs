using FMODUnity;
using Tanks.Shared.Messaging;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Mathematics;

namespace Tanks.Client.Weapons
{
    public sealed class ProjectileRicochetSound : IMessageHandler<ProjectileRicocheted>
    {
        private static readonly EventReference _reference = RuntimeManager
            .PathToEventReference("event:/SFX/Ricochet");

        public void Handle(ProjectileRicocheted message, IMessageSender sender)
        {
            float2 hitPoint = message.HitPoint;
            RuntimeManager.PlayOneShot(_reference, new float3(hitPoint.x, hitPoint.y, 0));
        }
    }
}