using System.Collections.Generic;
using Tanks.Shared.Adapters;
using Tanks.Shared.Entities.Health;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Shared.Weapons.Projectiles
{
    public class ProjectileMover 
    {
        public ProjectileMover(float radius)
        {
            _radius = radius;
        }

        private readonly float _radius;
        private readonly List<Collider2D> _colliders = new();

        public IHealthBehaviour ResolveLaunch(ITransform transform)
        {
            float2 origin = transform.WorldPosition;
            float2 position = origin;
            float2 direction = transform.Right;
            float radius = math.cmax(math.abs(transform.WorldScale)) * _radius;

            ContactFilter2D filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = 1 << 0,
                useTriggers = false
            };

            int steps = 0;

            while (Physics2D.OverlapCircle(position, radius, filter, _colliders) > 0)
            {
                position = origin - direction * (++steps * 0.01f);
            }

            transform.WorldPosition = position;
            filter.layerMask = 1 << 3;
            Physics2D.OverlapCircle(position, radius, filter, _colliders);

            foreach (Collider2D collider in _colliders)
            {
                if (collider.TryGetComponent(out IHealthBehaviour health)) return health;
            }

            return null;
        }

        public void Simulate(
            LaunchCallbacks callbacks, ITransform transform, float distance)
        {
            int mask = (1 << 0) | (1 << 3); // Only walls & entities

            float2 position = transform.WorldPosition;
            float radius = math.cmax(math.abs(transform.WorldScale)) * _radius;

            RaycastHit2D hit = Physics2D.Raycast(position, transform.Right, radius + distance, mask);

            if (hit.collider == false)
            {
                transform.WorldPosition += transform.Right * distance;
                return;
            }

            if (hit.collider.TryGetComponent(out IHealthBehaviour damageable))
            {
                transform.WorldPosition = hit.point;
                callbacks.OnDamageableHit?.Invoke(damageable);
                return;
            }

            float2 normal = hit.normal;
            float2 result = transform.WorldPosition;

            // Move to the collision point
            float travel = math.max(0, hit.distance - radius);
            result += transform.Right * travel;

            float2 reflected = math.reflect(transform.Right, normal);
            transform.WorldRotation = Angle.FromRadians(math.atan2(reflected.y, reflected.x));
            result += normal * 0.001f; // Push slightly away from the wall

            float remaining = math.max(0, distance - travel);
            result += transform.Right * remaining;

            transform.WorldPosition = result;
            callbacks.OnRicochet?.Invoke(hit.point, normal);
        }
    }
}
