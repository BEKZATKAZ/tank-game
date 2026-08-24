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

        [Range(1, 4)]
        [SerializeField]
        private float _sizeSpeed = 1;

        private ICameraMainService _cameraMain;
        private ITransform _target;
        private float _targetOrthographicSize;
        private Bounds _mapBounds;

        private float2 _defaultPosition;
        private float _defaultOrthographicSize = 5;

        [Inject]
        public void Inject(ICameraMainService cameraMain)
        {
            _cameraMain = cameraMain;
        }

        public void SetDefault(Bounds mapBounds, bool forceOverride)
        {
            ICamera camera = _cameraMain.Get();
            float width = mapBounds.size.x;
            float height = mapBounds.size.y;

            float aspect = camera.Aspect;
            float verticalSize = height * 0.5f;
            float horizontalSize = width / aspect * 0.5f;

            _mapBounds = mapBounds;
            _defaultPosition = ((float3)mapBounds.center).xy;
            _defaultOrthographicSize = math.max(verticalSize, horizontalSize);

            if (forceOverride == false) return;
            camera.Transform.WorldPosition = _defaultPosition;
            camera.OrthographicSize = _defaultOrthographicSize;
        }

        public void SetFollowTarget(ITransform target, float orthographicSize)
        {
            _target = target;
            _targetOrthographicSize = orthographicSize;
        }

        private void LerpTo(ICamera camera, float2 target, float size)
        {
            // Limit delta time to prevent overshooting during stutters
            float deltaTime = math.min(Time.deltaTime, 0.05f);

            float2 currentPosition = camera.Transform.WorldPosition;
            float2 position = math.lerp(currentPosition, target, deltaTime * _positionSpeed);
            float currentSize = camera.OrthographicSize;

            camera.Transform.WorldPosition = position;
            camera.OrthographicSize = math.lerp(currentSize, size, deltaTime * _sizeSpeed);
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

        private void LateUpdate()
        {
            ICamera camera = _cameraMain.Get();

            if (camera is null) return;

            if (_target is null || _target.IsValid == false)
            {
                LerpTo(camera, _defaultPosition, _defaultOrthographicSize);
                return;
            }

            float2 targetPosition = ClampToBounds(_target.WorldPosition, camera);
            LerpTo(camera, targetPosition, _targetOrthographicSize);
        }
    }
}
