using System;
using Tanks.Core.Helpers;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Utils;
using Unity.Mathematics;
using UnityEngine;
using VContainer;

namespace Tanks.Shared.Weapons.Projectiles.Modules
{
    public sealed class RadialDamageModule : DamageModuleBase
    {
        public byte Radius = 3;
        public ushort NearDamage = 40;
        public ushort FarDamage = 20;

        public override PositiveFloat MaxDamage => math.max((float)NearDamage, FarDamage);

        private IDamageService _service;

        [Inject]
        public void Construct(IDamageService service)
        {
            _service = service;
        }

        protected override void OnProjectileDone()
        {
            using var array = new TemporaryArrayResult<Collider2D>(16);
            float2 center = Transform.WorldPosition;

            HealthEventId eventId = HealthEventId.Create(GetType(), Id);
            IEntityBehaviour from = Projectile.Owner.Entity;

            int count = Physics2D.OverlapCircle(center, Radius, new ContactFilter2D
            {
                layerMask = 1 << 3 // Only entities
            }, array.Value.Array);

            for (int i = 0; i < count; i++)
            {
                ref Collider2D other = ref array[i];

                if (other.TryGetComponent(out IHealthBehaviour health) == false) continue;

                float2 otherPosition = health.Transform.WorldPosition;
                float2 halfSize = health.Entity.Size * 0.5f;

                quaternion rotation = quaternion
                    .AxisAngle(math.forward(), health.Transform.WorldRotation.Radians);

                Span<float2> points = stackalloc float2[5]
                {
                    otherPosition,
                    otherPosition + Rotate(new float2(-halfSize.x, 0), rotation),
                    otherPosition + Rotate(new float2(halfSize.x, 0), rotation),
                    otherPosition + Rotate(new float2(0, -halfSize.y), rotation),
                    otherPosition + Rotate(new float2(0, halfSize.y), rotation)
                };

                foreach (ref float2 point in points)
                {
                    float2 direction = point - center;
                    float distance = math.length(direction);

                    if (distance <= math.EPSILON)
                    {
                        _service.Damage(
                            NearDamage, health.Entity, from, eventId, includeStats: true);

                        break;
                    }

                    direction /= distance;
                    RaycastHit2D hit = Physics2D.Raycast(center, direction, distance, 1);

                    if (hit.collider) continue;

                    float t = 1 - math.saturate(distance / Radius);
                    float damage = math.lerp(FarDamage, NearDamage, t);

                    _service.Damage(damage, health.Entity, from, eventId, includeStats: true);
                    break;
                }
            }
        }

        private static float2 Rotate(float2 point, quaternion rotation)
        {
            float3 rotated = math.rotate(rotation, new float3(point, 0f));
            return rotated.xy;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
#endif
    }
}
