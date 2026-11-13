using Unity.Mathematics;

namespace Tanks.Client.Input.Driving
{
    public sealed class DirectionalDriveResolverService
    {
        private const float ALIGNMENT_TOLERANCE = 0.05f;
        private const float ALIGNMENT_TIME = 0.05f;

        public ManualDriveInput Resolve(
            float2 direction,
            float heading,
            bool turnInPlace,
            bool reverse,
            float deltaTime,
            float turnRate)
        {
            if (reverse)
                return new ManualDriveInput { Throttle = -1 };

            float magnitude = math.length(direction);
            if (magnitude <= 0) return default;

            float target = math.degrees(math.atan2(direction.y, direction.x));

            float error = ((target - heading + 180) % 360 + 360) % 360 - 180;
            float steering = 0;

            if (deltaTime > 0 && turnRate > 0 &&
                math.abs(error) > ALIGNMENT_TOLERANCE)
            {
                // Ease the final correction instead of chasing each rendered frame.
                float step = turnRate * math.max(deltaTime, ALIGNMENT_TIME);

                steering = math.clamp(-error / step, -1, 1);
            }

            float throttle = turnInPlace ? 0 : math.min(magnitude, 1);

            return new ManualDriveInput { Throttle = throttle, Steering = steering };
        }
    }
}
