using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Server.Lag
{
    public interface IHitSimulator
    {
        bool Overlaps(in EntitySnapshot entity, in ProjectileSnapshot projectile);
    }

    public sealed class HitSimulator : IHitSimulator
    {
        public bool Overlaps(in EntitySnapshot entity, in ProjectileSnapshot projectile)
        {
            float angle = -entity.Rotation.Radians;
            float sin = math.sin(angle);
            float cos = math.cos(angle);

            float2 offset = projectile.Position - entity.Position;
            float2 halfSize = entity.Size * 0.5f;

            float2 localPosition = new float2(
                offset.x * cos - offset.y * sin,
                offset.x * sin + offset.y * cos
            );

            float2 closestPoint = math.clamp(localPosition, -halfSize, halfSize);
            float2 delta = localPosition - closestPoint;

            Debug.Log(entity.Position + " - " + projectile.Position);

            return math.lengthsq(delta) <= projectile.Radius * projectile.Radius;
        }
    }
}
