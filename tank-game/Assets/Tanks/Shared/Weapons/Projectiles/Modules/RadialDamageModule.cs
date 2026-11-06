using System;
using System.Collections.Generic;
using Tanks.Core.Helpers;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Unity.Mathematics;
using UnityEngine;
using VContainer;

namespace Tanks.Shared.Weapons.Projectiles.Modules
{
    public class RadialDamageModule : DamageModuleBase
    {
        public byte Radius = 3;
        public ushort NearDamage = 40;
        public ushort FarDamage = 20;

        public override PositiveFloat MaxDamage => math.max((float)NearDamage, FarDamage);

        private readonly List<Collider2D> _colliders = new();
        private readonly HashSet<IEntityBehaviour> _targets = new();
        private IDamageService _service;

        [Inject]
        public virtual void Construct(IDamageService service) => _service = service;

        protected override void OnProjectileDone() => Explode(Transform.WorldPosition);
        protected virtual void BeforeDamage(IEntityBehaviour target, float distanceRatio) { }

        protected void Explode(float2 center)
        {
            if (Radius == 0) return;
            _targets.Clear();

            Physics2D.OverlapCircle(center, Radius, new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = 1 << 3,
                useTriggers = false
            }, _colliders);

            foreach (Collider2D other in _colliders)
            {
                if (other.TryGetComponent(out IHealthBehaviour health) == false) continue;
                if (_targets.Add(health.Entity) == false || health.ServerState.Value.Current <= 0) continue;

                float2 position = health.Transform.WorldPosition;
                float2 halfSize = health.Entity.Size * 0.5f;

                quaternion rotation = quaternion.AxisAngle(
                    math.forward(), health.Transform.WorldRotation.Radians);

                Span<float2> points = stackalloc float2[5]
                {
                    position,
                    position + Rotate(new float2(-halfSize.x, 0), rotation),
                    position + Rotate(new float2(halfSize.x, 0), rotation),
                    position + Rotate(new float2(0, -halfSize.y), rotation),
                    position + Rotate(new float2(0, halfSize.y), rotation)
                };

                foreach (float2 point in points)
                {
                    float2 direction = point - center;
                    float length = math.length(direction);

                    if (length > Radius) continue;
                    if (length > math.EPSILON &&
                        Physics2D.Raycast(center, direction / length, length, 1).collider)
                    {
                        continue;
                    }

                    // Preserve the existing damage falloff's floored distances
                    float distance = math.floor(length);
                    float damage = math.lerp(
                        NearDamage, FarDamage, math.saturate(distance / Radius));

                    BeforeDamage(health.Entity, math.saturate(length / Radius));

                    _service.Damage(
                        (uint)math.ceil(damage), health.Entity, Projectile.Owner.Entity,
                        HealthEventId.Create(GetType(), Id));

                    break;
                }
            }
        }

        private static float2 Rotate(float2 point, quaternion rotation)
        {
            return math.rotate(rotation, new float3(point, 0)).xy;
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
