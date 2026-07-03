using Tanks.Shared.Adapters;
using Unity.Mathematics;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Rendering
{
    public interface ICameraFollowService
    {
        void SetDefault(float2 defaultPosition, float defaultOrthographicSize, bool forceOverride);

        void SetFollowTarget(ITransform target, float orthographicSize);
    }

    public sealed class CameraFollowService : MonoBehaviour, ICameraFollowService
    {
        [Inject] private readonly ICameraMainService _cameraMain;

        private ITransform _target;
        private float _targetOrthographicSize;

        private float2 _defaultPosition;
        private float _defaultOrthographicSize = 5;

        public void SetDefault(
            float2 defaultPosition, float defaultOrthographicSize, bool forceOverride)
        {
            _defaultPosition = defaultPosition;
            _defaultOrthographicSize = defaultOrthographicSize;

            if (forceOverride == false) return;
            ICamera camera = _cameraMain.Get();

            camera.Transform.WorldPosition = defaultPosition;
            camera.OrthographicSize = defaultOrthographicSize;
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
            float positionSpeed = 5;
            float sizeSpeed = 1;

            float2 currentPosition = camera.Transform.WorldPosition;
            float2 position = math.lerp(currentPosition, target, deltaTime * positionSpeed);
            float currentSize = camera.OrthographicSize;

            camera.Transform.WorldPosition = position;
            camera.OrthographicSize = math.lerp(currentSize, size, deltaTime * sizeSpeed);
        }

        private void LateUpdate()
        {
            ICamera camera = _cameraMain.Get();

            if (_target is null || _target.IsValid == false)
            {
                LerpTo(camera, _defaultPosition, _defaultOrthographicSize);
                return;
            }

            LerpTo(camera, _target.WorldPosition, _targetOrthographicSize);
        }
    }
}