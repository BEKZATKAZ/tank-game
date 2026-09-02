using Tanks.Shared.Adapters;
using Unity.Mathematics;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Rendering
{
    public sealed class CameraFollowService : MonoBehaviour, ICameraFollowService
    {
        [Range(2, 10)]
        [SerializeField]
        private float _positionSpeed = 5;

        private ICameraMainService _cameraMain;
        private ITransform _target;
        private Bounds _mapBounds;
        private float2 _previousTargetPosition;
        private float2 _cameraNextPosition;
        private float2 _lookAhead;

        [Inject]
        public void Inject(ICameraMainService cameraMain)
        {
            _cameraMain = cameraMain;
        }

        public void SetBounds(Bounds mapBounds)
        {
            _mapBounds = mapBounds;
        }

        public void SetFollowTarget(ITransform target)
        {
            _target = target;
            _previousTargetPosition = target.WorldPosition;
            _cameraNextPosition = _previousTargetPosition;
            _lookAhead = float2.zero;
        }

        private float2 ClampToBounds(float2 target, ICamera camera)
        {
            float halfHeight = camera.OrthographicSize;
            float halfWidth = halfHeight * camera.Aspect;

            float minX = _mapBounds.min.x + halfWidth;
            float maxX = _mapBounds.max.x - halfWidth;
            float minY = _mapBounds.min.y + halfHeight;
            float maxY = _mapBounds.max.y - halfHeight;

            return new float2(
                math.clamp(target.x, minX, maxX),
                math.clamp(target.y, minY, maxY));
        }

        private void UpdateTargetPosition()
        {
            float lookAheadSpeed = 3;
            float lookAheadMultiplier = 0.65f;

            float2 playerPosition = _target.WorldPosition;
            float2 movement = (playerPosition - _previousTargetPosition) / Time.deltaTime;
            float2 desiredLookAhead = movement * lookAheadMultiplier;

            float dt = math.min(Time.deltaTime, 0.05f);
            float t = 1 - math.exp(-lookAheadSpeed * dt);

            _lookAhead = math.lerp(_lookAhead, desiredLookAhead, t);
            _cameraNextPosition = playerPosition + _lookAhead;
            _previousTargetPosition = playerPosition;
        }

        private void LateUpdate()
        {
            ICamera camera = _cameraMain.Get();

            if (camera is null) return;
            if (_target != null && _target.IsValid) UpdateTargetPosition();

            float2 currentPosition = camera.Transform.WorldPosition;
            float2 clamped = ClampToBounds(_cameraNextPosition, camera);

            // Limit delta time to prevent overshooting during stutters
            float deltaTime = math.min(Time.deltaTime, 0.1f);
            
            float2 position = math.lerp(currentPosition, clamped, deltaTime * _positionSpeed);
            camera.Transform.WorldPosition = position;
        }
    }
}
