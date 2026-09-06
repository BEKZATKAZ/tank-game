using Tanks.Shared;
using Tanks.Shared.Adapters;
using Tanks.Shared.Driving;
using Unity.Mathematics;

namespace Tanks.Client.Player
{
    public sealed class TireTrackEmitter : NetworkComponent<ITankDriveBehaviour>
    {
        private float2 _lastPosition;
        private Angle _lastRotation;

        private float _leftVolume;
        private float _rightVolume;

        protected override void OnEarlyUpdate(float deltaTime)
        {
            float2 currentPosition = Behaviour.Transform.WorldPosition;
            Angle currentRotation = Behaviour.Transform.WorldRotation;

            float distance = math.distance(currentPosition, _lastPosition);
            float angularVelocity = (currentRotation - _lastRotation).Degrees / deltaTime;

            _lastPosition = currentPosition;
            _lastRotation = currentRotation;

            // Prevent long stretching tracks, for example when teleporting
            if (distance > 0.5f)
            {
                Behaviour.EmitTireTracks(false, false);
                return;
            }

            bool isMoving = distance > 0.001f;
            bool isTurning = math.abs(angularVelocity) > 5;

            // Turning in place
            if (isMoving == false && isTurning)
            {
                Behaviour.EmitTireTracks(true, true);
                return;
            }

            bool left = isMoving && angularVelocity <= 0;
            bool right = isMoving && angularVelocity >= 0;

            float leftDelta = left ? deltaTime : -deltaTime;
            float rightDelta = right ? deltaTime : -deltaTime;
            float volumeSpeed = 4;

            _leftVolume = math.saturate(_leftVolume + leftDelta * volumeSpeed);
            _rightVolume = math.saturate(_rightVolume + rightDelta * volumeSpeed);

            Behaviour.EmitTireTracks(_leftVolume >= deltaTime, _rightVolume >= deltaTime);
        }
    }
}
