using Tanks.Core.Helpers;
using Tanks.Shared.Adapters;
using Unity.Mathematics;

namespace Tanks.Shared.Driving
{
    public struct ArcadeDriverSettings
    {
        public PositiveFloat AccelerationTime;
        public PositiveFloat MaxSpeed;
        public PositiveFloat ReverseSpeed;
        public Angle SteeringSpeed;
    }

    public class ArcadeDriver
    {
        public ArcadeDriver(ArcadeDriverSettings settings) => _settings = settings;

        private readonly ArcadeDriverSettings _settings;
        private float _acceleration;

        public void SetMaxAcceleration(PositiveFloatOrZero limit)
        {
            limit = math.saturate(limit);

            if (_acceleration > 0) _acceleration = math.min(_acceleration, limit);
            // Treat min as max since min is within 0-1
            else _acceleration = math.max(_acceleration, -limit);
        }

        public float2 Drive(float2 forward, float throttle, float deltaTime)
        {
            throttle = math.clamp(throttle, -1, 1);

            // Distance / Time
            float accTime = deltaTime / _settings.AccelerationTime;

            if (throttle > _acceleration)
            {
                _acceleration = math.min(_acceleration + accTime, throttle);
            }
            else if (throttle < _acceleration)
            {
                _acceleration = math.max(_acceleration - accTime, throttle);
            }

            float speed = _acceleration < 0 ? _settings.ReverseSpeed : _settings.MaxSpeed;
            float force = _acceleration * speed;

            return force * forward;
        }

        public Angle Steer(float steering)
        {
            steering = math.clamp(steering, -1, 1);
            return Angle.FromDegrees(-steering) * _settings.SteeringSpeed;
        }
    }
}