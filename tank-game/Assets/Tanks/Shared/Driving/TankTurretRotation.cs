using Tanks.Shared.Adapters;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Shared.Driving
{
    public class TankTurretRotation
    {
        public TankTurretRotation(ITransform transform, Angle turnSpeed)
        {
            _transform = transform;
            _turnSpeed = turnSpeed;
        }

        private readonly ITransform _transform;
        private readonly Angle _turnSpeed;

        public void TurnAt(float2 direction, float deltaTime)
        {
            direction = math.normalizesafe(direction);

            float target = math.atan2(direction.y, direction.x);
            float current = _transform.WorldRotation.Radians;
            float delta = _turnSpeed.Radians * deltaTime;

            float diff = math.radians(Mathf.DeltaAngle(
                math.degrees(current),
                math.degrees(target)
            ));

            float step = math.clamp(diff, -delta, delta);
            float result = current + step;

            _transform.WorldRotation = Angle.FromRadians(result);
        }
    }
}
