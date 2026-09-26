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

        public void Simulate(
            LaunchCallbacks callbacks, ITransform transform, float distance)
        {
            int mask = (1 << 0) | (1 << 3); // Only walls & entities

            float2 position = transform.WorldPosition;
            float radius = math.cmax(transform.WorldScale) * _radius;

            RaycastHit2D hit = Physics2D.Raycast(position, transform.Right, radius + distance, mask);

            if (hit.collider == false)
            {
                transform.WorldPosition += transform.Right * distance;
                return;
            }

            if (hit.collider.TryGetComponent(out IHealthBehaviour damageable))
            {
                callbacks.OnDamageableHit?.Invoke(damageable);
                transform.WorldPosition += transform.Right * distance;
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
